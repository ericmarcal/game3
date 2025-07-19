using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

[System.Serializable]
public struct BuildingTile
{
    public Vector3Int position;
    public TileBase tile;
    public int sortingLayerID;
    public int orderInLayer;
}

[CreateAssetMenu(fileName = "New BuildingData", menuName = "Construction/Building Data")]
public class BuildingData : ScriptableObject
{
    [Header("Informações da Construção")]
    public string buildingName;
    public Vector2Int size; // A informação mais importante para o nosso novo sistema!

    [Header("Blueprint dos Tiles")]
    public List<BuildingTile> tiles;
}