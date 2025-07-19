using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;
using System.Linq;

public class ConstructionManager : MonoBehaviour
{
    public static ConstructionManager instance;

    // Um dicionário para guardar referências aos tilemaps da cena e evitar procurá-los repetidamente.
    private Dictionary<string, Tilemap> tilemapCache = new Dictionary<string, Tilemap>();

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // A função principal que irá construir o nosso edifício.
    public void Build(BuildingData buildingData, Vector3Int basePosition)
    {
        if (buildingData == null)
        {
            Debug.LogError("Tentativa de construir com BuildingData nulo!");
            return;
        }

        // Itera sobre cada "instrução" (cada tile) do nosso blueprint.
        foreach (var buildingTile in buildingData.tiles)
        {
            // Encontra ou cria o tilemap de destino com as propriedades corretas.
            Tilemap targetMap = GetOrCreateTilemap(buildingTile.sortingLayerID, buildingTile.orderInLayer);

            if (targetMap != null)
            {
                // Calcula a posição final do tile no mundo.
                Vector3Int finalTilePosition = basePosition + buildingTile.position;
                // "Pinta" o tile no local e tilemap corretos.
                targetMap.SetTile(finalTilePosition, buildingTile.tile);
            }
        }

        Debug.Log($"Construção '{buildingData.buildingName}' concluída na posição {basePosition}!");
    }

    // Método auxiliar para encontrar um tilemap na cena ou criar um novo se não existir.
    private Tilemap GetOrCreateTilemap(int sortingLayerID, int orderInLayer)
    {
        // Cria uma chave única para o nosso dicionário com base nas propriedades de renderização.
        string key = $"{sortingLayerID}_{orderInLayer}";

        // Se já encontrámos este tilemap antes, usa a referência guardada.
        if (tilemapCache.ContainsKey(key))
        {
            return tilemapCache[key];
        }

        // Se não, procura por um tilemap na cena que já tenha estas propriedades.
        foreach (var map in FindObjectsOfType<Tilemap>())
        {
            TilemapRenderer renderer = map.GetComponent<TilemapRenderer>();
            if (renderer != null && renderer.sortingLayerID == sortingLayerID && renderer.sortingOrder == orderInLayer)
            {
                tilemapCache[key] = map; // Guarda na cache para a próxima vez.
                return map;
            }
        }

        // Se não encontrou nenhum tilemap correspondente, cria um novo.
        // Isto é útil para garantir que o sistema nunca falhe.
        GameObject newTilemapObject = new GameObject($"Tilemap_{SortingLayer.IDToName(sortingLayerID)}_Order{orderInLayer}");
        newTilemapObject.transform.SetParent(this.transform); // Organiza-o como filho do ConstructionManager
        Tilemap newTilemap = newTilemapObject.AddComponent<Tilemap>();
        TilemapRenderer newRenderer = newTilemapObject.AddComponent<TilemapRenderer>();

        newRenderer.sortingLayerID = sortingLayerID;
        newRenderer.sortingOrder = orderInLayer;

        Debug.Log($"<color=orange>Criado novo tilemap: {newTilemapObject.name}</color>");

        tilemapCache[key] = newTilemap; // Guarda na cache.
        return newTilemap;
    }
}