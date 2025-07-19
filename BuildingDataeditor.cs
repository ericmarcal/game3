using UnityEngine;
using UnityEditor;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

public class BuildingDataEditor : EditorWindow
{
    private BuildingData targetBuildingData;
    private BoxCollider2D blueprintBounds;

    [MenuItem("Tools/Building Blueprint Tool")]
    public static void ShowWindow()
    {
        GetWindow<BuildingDataEditor>("Building Blueprint Tool");
    }

    private void OnGUI()
    {
        GUILayout.Label("Building Blueprint Generator (Collider Mode)", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Desenhe a sua construção na cena. Adicione um GameObject com um BoxCollider2D para definir a área a ser copiada. Depois, arraste os assets para os campos abaixo e gere o blueprint.", MessageType.Info);

        targetBuildingData = (BuildingData)EditorGUILayout.ObjectField("Target Building Data", targetBuildingData, typeof(BuildingData), false);
        blueprintBounds = (BoxCollider2D)EditorGUILayout.ObjectField("Blueprint Area Collider", blueprintBounds, typeof(BoxCollider2D), true);

        EditorGUILayout.Space();

        if (targetBuildingData != null && blueprintBounds != null)
        {
            if (GUILayout.Button("Generate Blueprint From Collider Area"))
            {
                GenerateBlueprint();
            }
        }
        else
        {
            GUI.enabled = false;
            GUILayout.Button("Preencha ambos os campos para gerar");
            GUI.enabled = true;
        }
    }

    private void GenerateBlueprint()
    {
        if (!EditorUtility.DisplayDialog("Confirmar Geração de Blueprint",
            "Isto irá apagar a lista de tiles atual e recriá-la com base na área do colisor. Tem a certeza?", "Sim, Gerar", "Cancelar"))
        {
            return;
        }

        targetBuildingData.tiles.Clear();
        Undo.RecordObject(targetBuildingData, "Generate Building Blueprint");

        Bounds bounds = blueprintBounds.bounds;
        Tilemap[] allTilemapsInScene = FindObjectsOfType<Tilemap>();

        if (allTilemapsInScene.Length == 0)
        {
            EditorUtility.DisplayDialog("Erro", "Nenhum Tilemap foi encontrado na cena atual.", "OK");
            return;
        }

        // << LÓGICA CORRIGIDA: Define o canto inferior esquerdo como a nossa âncora >>
        Vector3 anchorPoint = bounds.center - new Vector3(bounds.extents.x, bounds.extents.y, 0);

        foreach (var map in allTilemapsInScene)
        {
            TilemapRenderer renderer = map.GetComponent<TilemapRenderer>();
            if (renderer == null) continue;

            for (int x = map.cellBounds.xMin; x < map.cellBounds.xMax; x++)
            {
                for (int y = map.cellBounds.yMin; y < map.cellBounds.yMax; y++)
                {
                    Vector3Int tileLocalPos = new Vector3Int(x, y, 0);
                    Vector3 tileWorldPos = map.CellToWorld(tileLocalPos);

                    if (bounds.Contains(tileWorldPos))
                    {
                        TileBase tile = map.GetTile(tileLocalPos);
                        if (tile != null)
                        {
                            BuildingTile newTile = new BuildingTile
                            {
                                // << LÓGICA CORRIGIDA: Guarda a posição relativa à âncora (canto inferior esquerdo) >>
                                position = map.WorldToCell(tileWorldPos) - map.WorldToCell(anchorPoint),
                                tile = tile,
                                sortingLayerID = renderer.sortingLayerID,
                                orderInLayer = renderer.sortingOrder
                            };
                            targetBuildingData.tiles.Add(newTile);
                        }
                    }
                }
            }
        }

        targetBuildingData.size = new Vector2Int(Mathf.CeilToInt(bounds.size.x), Mathf.CeilToInt(bounds.size.y));

        EditorUtility.SetDirty(targetBuildingData);
        EditorUtility.DisplayDialog("Sucesso", "Blueprint gerado com sucesso a partir da área do colisor! " + targetBuildingData.tiles.Count + " tiles foram copiados.", "OK");
    }
}