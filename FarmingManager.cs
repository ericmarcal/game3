using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

public class FarmingManager : MonoBehaviour, ISavable
{
    public static FarmingManager instance;

    [Header("Referências de Tilemap")]
    [SerializeField] private Tilemap soilTilemap;
    [SerializeField] private Tilemap waterEffectTilemap;

    [Header("Referências de Tiles")]
    [SerializeField] private TileBase tilledSoilTile;
    [SerializeField] private TileBase wateredTile;

    [Header("Organização das Plantas")]
    [SerializeField] private GameObject genericCropPrefab;
    [SerializeField] private Transform plantsContainer;

    [Header("Configurações de Renderização")]
    [SerializeField] private string plantSortingLayer = "Default";
    [SerializeField] private string plantLayerName = "Default";

    private Dictionary<Vector3Int, PlantedCrop> plantedCrops = new Dictionary<Vector3Int, PlantedCrop>();
    private Dictionary<Vector3Int, bool> tilledGroundState = new Dictionary<Vector3Int, bool>();

    public string ID => "FarmingManager";

    [System.Serializable]
    private struct PlantedCropSaveData
    {
        public string cropYieldItemName;
        public Vector3Int gridPosition;
        public int currentGrowthStage;
        public bool isWatered;
    }

    [System.Serializable]
    private struct FarmSaveData
    {
        public List<Vector3Int> tilledGroundPositions;
        public List<PlantedCropSaveData> plantedCropsData;
    }

    public object CaptureState()
    {
        var farmData = new FarmSaveData
        {
            tilledGroundPositions = tilledGroundState.Keys.ToList(),
            plantedCropsData = plantedCrops.Values.Select(crop => new PlantedCropSaveData
            {
                cropYieldItemName = crop.cropData.yieldItem.itemName,
                gridPosition = crop.gridPosition,
                currentGrowthStage = crop.currentGrowthStage,
                isWatered = crop.isWatered
            }).ToList()
        };
        return farmData;
    }

    public void RestoreState(object state)
    {
        var farmData = ((JObject)state).ToObject<FarmSaveData>();

        foreach (var crop in plantedCrops.Values)
        {
            if (crop.cropInstance != null) Destroy(crop.cropInstance);
        }
        plantedCrops.Clear();
        tilledGroundState.Clear();
        if (waterEffectTilemap != null) waterEffectTilemap.ClearAllTiles();

        if (farmData.tilledGroundPositions != null)
        {
            foreach (var pos in farmData.tilledGroundPositions)
            {
                soilTilemap.SetTile(pos, tilledSoilTile);
                tilledGroundState[pos] = false;
            }
        }

        if (farmData.plantedCropsData != null)
        {
            foreach (var cropData in farmData.plantedCropsData)
            {
                ItemData itemFromDb = ItemDatabase.Instance.GetItemByName(cropData.cropYieldItemName);
                if (itemFromDb != null)
                {
                    CropData dataToReplant = itemFromDb.cropData;
                    if (dataToReplant != null)
                    {
                        PlantedCrop restoredCrop = Replant(dataToReplant, cropData.gridPosition);
                        restoredCrop.currentGrowthStage = cropData.currentGrowthStage;
                        restoredCrop.isWatered = cropData.isWatered;
                        restoredCrop.UpdateSprite();

                        if (restoredCrop.isWatered)
                        {
                            if (waterEffectTilemap != null) waterEffectTilemap.SetTile(cropData.gridPosition, wateredTile);
                            tilledGroundState[cropData.gridPosition] = true;
                        }
                    }
                }
            }
        }
    }

    private void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);
    }

    private PlantedCrop Replant(CropData cropToPlant, Vector3Int gridPosition)
    {
        Vector3 worldPosition = soilTilemap.GetCellCenterWorld(gridPosition);
        GameObject cropInstance = Instantiate(genericCropPrefab, worldPosition, Quaternion.identity, plantsContainer);
        cropInstance.name = $"{cropToPlant.name}_Restored";
        int targetLayer = LayerMask.NameToLayer(plantLayerName);
        if (targetLayer != -1) cropInstance.layer = targetLayer;
        SpriteRenderer sr = cropInstance.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.sortingLayerName = plantSortingLayer;
            sr.sortingOrder = 5;
        }
        PlantedCrop newPlantedCrop = new PlantedCrop(cropToPlant, gridPosition, cropInstance);
        plantedCrops[gridPosition] = newPlantedCrop;
        return newPlantedCrop;
    }

    public void Plant(Vector3Int gridPosition, CropData cropToPlant)
    {
        if (cropToPlant == null || genericCropPrefab == null) return;

        if (soilTilemap.GetTile(gridPosition) == tilledSoilTile && !plantedCrops.ContainsKey(gridPosition))
        {
            PlayerItens.instance.RemoveQuantityFromCurrentSlot(1);
            Vector3 worldPosition = soilTilemap.GetCellCenterWorld(gridPosition);
            GameObject cropInstance = Instantiate(genericCropPrefab, worldPosition, Quaternion.identity, plantsContainer);
            cropInstance.name = $"{cropToPlant.name}_Planted";
            int targetLayer = LayerMask.NameToLayer(plantLayerName);
            if (targetLayer != -1) cropInstance.layer = targetLayer;
            SpriteRenderer sr = cropInstance.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.sortingLayerName = plantSortingLayer;
                sr.sortingOrder = 5;
            }
            PlantedCrop newPlantedCrop = new PlantedCrop(cropToPlant, gridPosition, cropInstance);
            if (tilledGroundState.ContainsKey(gridPosition) && tilledGroundState[gridPosition])
            {
                newPlantedCrop.isWatered = true;
            }
            plantedCrops.Add(gridPosition, newPlantedCrop);
        }
    }

    public void AdvanceDay()
    {
        List<PlantedCrop> cropsToProcess = new List<PlantedCrop>(plantedCrops.Values);
        foreach (var crop in cropsToProcess)
        {
            if (crop.isWatered)
            {
                crop.Grow();
                crop.isWatered = false;
                if (waterEffectTilemap != null) waterEffectTilemap.SetTile(crop.gridPosition, null);
                if (tilledGroundState.ContainsKey(crop.gridPosition)) tilledGroundState[crop.gridPosition] = false;
            }
        }
    }

    public void Dig(Vector3Int gridPosition)
    {
        if (soilTilemap.GetTile(gridPosition) != null && soilTilemap.GetTile(gridPosition) != tilledSoilTile)
        {
            soilTilemap.SetTile(gridPosition, tilledSoilTile);
            if (!tilledGroundState.ContainsKey(gridPosition))
            {
                tilledGroundState.Add(gridPosition, false);
            }
        }
    }

    public void Water(Vector3Int gridPosition)
    {
        if (soilTilemap.GetTile(gridPosition) == tilledSoilTile)
        {
            if (plantedCrops.ContainsKey(gridPosition))
            {
                plantedCrops[gridPosition].isWatered = true;
            }
            if (waterEffectTilemap != null) waterEffectTilemap.SetTile(gridPosition, wateredTile);
            tilledGroundState[gridPosition] = true;
        }
    }

    public bool CanHarvest(Vector3Int gridPosition)
    {
        return plantedCrops.ContainsKey(gridPosition) && plantedCrops[gridPosition].IsFullyGrown();
    }

    public void Harvest(Vector3Int gridPosition)
    {
        if (CanHarvest(gridPosition))
        {
            PlantedCrop cropToHarvest = plantedCrops[gridPosition];
            if (cropToHarvest.cropData.yieldItem != null)
            {
                GameObject itemPrefabToDrop = cropToHarvest.cropData.yieldItem.itemPrefab;
                if (itemPrefabToDrop != null)
                {
                    for (int i = 0; i < cropToHarvest.cropData.yieldAmount; i++)
                    {
                        Vector3 spawnPosition = soilTilemap.GetCellCenterWorld(gridPosition);
                        GameObject itemInstance = Instantiate(itemPrefabToDrop, spawnPosition, Quaternion.identity);
                        WorldItem worldItem = itemInstance.GetComponent<WorldItem>();
                        if (worldItem != null)
                        {
                            worldItem.itemData = cropToHarvest.cropData.yieldItem;
                            worldItem.quantity = 1;
                            Vector2 popDirection = new Vector2(Random.Range(-0.5f, 0.5f), 1f);
                            worldItem.SetupSpawnedItemParameters(spawnPosition, popDirection, 1.5f);
                        }
                    }
                }
            }
            Destroy(cropToHarvest.cropInstance);
            plantedCrops.Remove(gridPosition);
            if (waterEffectTilemap != null) waterEffectTilemap.SetTile(gridPosition, null);
            soilTilemap.SetTile(gridPosition, tilledSoilTile);
            if (tilledGroundState.ContainsKey(gridPosition))
            {
                tilledGroundState[gridPosition] = false;
            }
        }
    }

    public void RemoveCrop(PlantedCrop cropToRemove)
    {
        if (cropToRemove == null) return;
        Vector3Int gridPosition = cropToRemove.gridPosition;
        if (plantedCrops.ContainsKey(gridPosition))
        {
            plantedCrops.Remove(gridPosition);
            if (cropToRemove.cropInstance != null)
            {
                Destroy(cropToRemove.cropInstance);
            }
        }
    }

    // << BLOCO DE REGISTO ADICIONADO >>
    protected virtual void OnEnable()
    {
        if (SaveLoadManager.instance != null)
        {
            SaveLoadManager.instance.RegisterSavable(this);
        }
    }

    protected virtual void OnDisable()
    {
        if (SaveLoadManager.instance != null)
        {
            SaveLoadManager.instance.UnregisterSavable(this);
        }
    }
}