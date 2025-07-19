using UnityEngine;
using System.Collections.Generic;

public enum ItemType
{
    Recurso,
    Ferramenta,
    Consumivel,
    Semente,
    Placeable,
    ConstructionKit // << NOVO TIPO DE ITEM >>
}

[CreateAssetMenu(fileName = "New ItemData", menuName = "Inventory/Item Data")]
public class ItemData : ScriptableObject
{
    [Header("Informações Básicas")]
    public string itemName = "New Item";
    [TextArea(3, 10)]
    public string description = "Descrição do item aqui...";
    public Sprite icon = null;
    public GameObject itemPrefab;

    [Header("Categorização e Comportamento")]
    public ItemType itemType = ItemType.Recurso;
    public ToolType associatedTool = ToolType.None;

    // << NOVO CAMPO >>
    [Header("Construção")]
    [Tooltip("Se o ItemType for 'ConstructionKit', arraste o BuildingData correspondente para aqui.")]
    public BuildingData buildingData;

    [Header("Posicionável (Placeable)")]
    public GameObject placeablePrefab;

    [Header("Plantação")]
    public CropData cropData;

    [Header("Consumível")]
    public bool isConsumable = false;
    public float healthToRestore = 0;
    public float staminaToRestore = 0;

    [Header("Economia")]
    public int buyPrice = 10;
    public int sellPrice = 5;

    [Header("Empilhamento")]
    public bool isStackable = true;
    public int maxStackSize = 64;

    [Header("Comportamento e UI")]
    public bool isCollectible = true;
    public bool isTrackedOnHUD = false;
    public int trackedLimit = 50;
}