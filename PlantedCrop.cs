using UnityEngine;

[System.Serializable]
public class PlantedCrop
{
    public CropData cropData;
    public Vector3Int gridPosition;
    public int currentGrowthStage = 0;
    public bool isWatered = false;
    public float growthTimer = 0f;

    public GameObject cropInstance;
    public SpriteRenderer cropSpriteRenderer;

    public PlantedCrop(CropData data, Vector3Int position, GameObject instance)
    {
        cropData = data;
        gridPosition = position;
        cropInstance = instance;
        currentGrowthStage = 0;
        isWatered = false;
        if (instance != null)
        {
            cropSpriteRenderer = instance.GetComponent<SpriteRenderer>();
        }
        UpdateSprite();
    }

    public void Grow()
    {
        if (IsFullyGrown()) return;

        currentGrowthStage++;
        growthTimer = 0f;
        isWatered = false;
        // --- DEBUG LOG ---
        Debug.Log($"Planta em {gridPosition} cresceu para o estágio {currentGrowthStage}.");
        UpdateSprite();
    }

    public void UpdateSprite()
    {
        if (cropSpriteRenderer != null && cropData.growthSprites != null && cropData.growthSprites.Count > 0)
        {
            int spriteIndex = Mathf.Clamp(currentGrowthStage, 0, cropData.growthSprites.Count - 1);
            if (spriteIndex < cropData.growthSprites.Count)
            {
                cropSpriteRenderer.sprite = cropData.growthSprites[spriteIndex];
            }
        }

        if (cropInstance != null)
        {
            if (IsFullyGrown())
            {
                // --- DEBUG LOG ---
                Debug.Log($"<color=green>PLANTA PRONTA:</color> A planta em {gridPosition} está pronta para ser colhida.");

                cropInstance.layer = LayerMask.NameToLayer("Interactable");
                // --- DEBUG LOG ---
                Debug.Log($"Layer da planta em {gridPosition} mudada para 'Interactable'.");

                BoxCollider2D collider = cropInstance.GetComponent<BoxCollider2D>();
                if (collider == null)
                {
                    collider = cropInstance.AddComponent<BoxCollider2D>();
                    collider.isTrigger = true;
                    // --- DEBUG LOG ---
                    Debug.Log($"BoxCollider2D adicionado à planta em {gridPosition}.");
                }

                // *** LÓGICA DA "ETIQUETA" ADICIONADA AQUI ***
                Harvestable harvestable = cropInstance.GetComponent<Harvestable>();
                if (harvestable == null)
                {
                    harvestable = cropInstance.AddComponent<Harvestable>();
                    // --- DEBUG LOG ---
                    Debug.Log($"<color=cyan>ETIQUETA:</color> Componente 'Harvestable' adicionado à planta em {gridPosition}.");
                }
                // Liga esta instância da planta à etiqueta, para o jogador saber o que colher.
                harvestable.crop = this;
            }
            else
            {
                cropInstance.layer = LayerMask.NameToLayer("Default");
                if (cropInstance.GetComponent<Harvestable>() != null)
                {
                    Object.Destroy(cropInstance.GetComponent<Harvestable>());
                }
                if (cropInstance.GetComponent<BoxCollider2D>() != null)
                {
                    Object.Destroy(cropInstance.GetComponent<BoxCollider2D>());
                }
            }
        }
    }

    public bool IsFullyGrown()
    {
        return currentGrowthStage >= cropData.GrowthStages - 1;
    }
}