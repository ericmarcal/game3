using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "New Crop Data", menuName = "Farming/Crop Data")]
public class CropData : ScriptableObject
{
    [Header("Configuração da Plantação")]
    public ItemData seedItem;
    public GameObject cropPrefab;
    public List<Sprite> growthSprites;

    [Tooltip("O tempo TOTAL em segundos que a planta leva para amadurecer.")]
    public float secondsToGrow = 10f; // << CAMPO ATUALIZADO

    [Header("Colheita")]
    public ItemData yieldItem;
    public int yieldAmount = 1;

    // Propriedade para calcular o número de estágios baseado nos sprites
    public int GrowthStages => growthSprites.Count;
}