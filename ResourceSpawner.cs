using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Tilemaps;
using UnityEngine.AI;
using Newtonsoft.Json.Linq;

[System.Serializable]
public struct SpawnConfiguration
{
    [Tooltip("O prefab do recurso ou inimigo a ser instanciado.")]
    public GameObject prefab;
    [Tooltip("Quantas instâncias deste prefab devem ser criadas.")]
    public int count;
}

[RequireComponent(typeof(PolygonCollider2D), typeof(UniqueID))]
public class ResourceSpawner : MonoBehaviour, ISavable
{
    [Header("Configuração de Spawn")]
    [SerializeField] private List<SpawnConfiguration> itemsToSpawn;

    [Header("Regras Globais de Spawn")]
    [SerializeField] private float minDistanceBetweenSpawns = 1.0f;
    [Tooltip("Máximo de tentativas para encontrar uma posição válida para CADA objeto individualmente.")]
    [SerializeField] private int maxPlacementAttempts = 20;

    [Header("Validação de Posição por Tilemap")]
    [Tooltip("O tilemap base onde os objetos podem ser criados (ex: a relva).")]
    [SerializeField] private Tilemap spawnableTilemap;

    // << MUDANÇA PRINCIPAL: Campo dedicado para a água >>
    [Tooltip("O tilemap da água. Qualquer tile aqui irá BLOQUEAR o spawn.")]
    [SerializeField] private Tilemap waterTilemap;

    [Tooltip("Outros tilemaps que devem BLOQUEAR o spawn (ex: caminhos, casas).")]
    [SerializeField] private List<Tilemap> overlayBlockerTilemaps;

    [Header("Validação de Posição por Física")]
    [SerializeField] private LayerMask obstacleLayerMask;
    [SerializeField] private float overlapCheckRadius = 0.4f;

    [Header("Organização e Navegação")]
    [SerializeField] private Transform spawnedObjectsParent;
    [SerializeField] private NavMeshSurface2d navMeshSurface;

    private List<GameObject> spawnedObjects = new List<GameObject>();
    private bool hasBeenRestored = false;

    public string ID => GetComponent<UniqueID>().ID;

    private void Awake()
    {
        if (spawnedObjectsParent == null)
        {
            spawnedObjectsParent = this.transform;
        }
    }

    private void Start()
    {
        if (!hasBeenRestored)
        {
            SpawnInitialObjects();
        }
    }

    public void SpawnInitialObjects()
    {
        foreach (Transform child in spawnedObjectsParent)
        {
            Destroy(child.gameObject);
        }
        spawnedObjects.Clear();

        var spawnArea = GetComponent<PolygonCollider2D>();
        Bounds bounds = spawnArea.bounds;

        int totalSuccess = 0;
        int totalFail = 0;

        foreach (var config in itemsToSpawn)
        {
            int successCount = 0;
            for (int i = 0; i < config.count; i++)
            {
                bool positionFound = false;
                for (int attempt = 0; attempt < maxPlacementAttempts; attempt++)
                {
                    float randomX = Random.Range(bounds.min.x, bounds.max.x);
                    float randomY = Random.Range(bounds.min.y, bounds.max.y);
                    Vector2 potentialPosition = new Vector2(randomX, randomY);

                    if (spawnArea.OverlapPoint(potentialPosition) && IsValidPlacement(potentialPosition))
                    {
                        GameObject newInstance = Instantiate(config.prefab, potentialPosition, Quaternion.identity, spawnedObjectsParent);
                        spawnedObjects.Add(newInstance);
                        positionFound = true;
                        break;
                    }
                }
                if (positionFound) successCount++;
            }

            totalSuccess += successCount;
            if (successCount < config.count)
            {
                totalFail += (config.count - successCount);
            }
        }

        if (navMeshSurface != null)
        {
            navMeshSurface.UpdateNavMesh(navMeshSurface.navMeshData);
        }
    }

    private bool IsValidPlacement(Vector3 position)
    {
        Vector3Int cellPosition = spawnableTilemap.WorldToCell(position);

        // << LÓGICA DE VALIDAÇÃO HIERÁRQUICA >>

        // 1. Verificação de Água (Prioridade Máxima)
        if (waterTilemap != null && waterTilemap.HasTile(cellPosition))
        {
            return false; // Encontrou água, local inválido.
        }

        // 2. Verificação de Outras Camadas de Bloqueio
        if (overlayBlockerTilemaps != null)
        {
            foreach (var overlayMap in overlayBlockerTilemaps)
            {
                if (overlayMap != null && overlayMap.HasTile(cellPosition))
                {
                    return false; // Encontrou um bloqueador, local inválido.
                }
            }
        }

        // 3. Verificação do Chão Permitido (Relva)
        if (spawnableTilemap == null || !spawnableTilemap.HasTile(cellPosition))
        {
            return false; // Não está sobre a relva, local inválido.
        }

        // 4. Verificação de Colisores Físicos
        if (Physics2D.OverlapCircle(position, overlapCheckRadius, obstacleLayerMask))
        {
            return false;
        }

        // 5. Verificação de Distância Mínima
        foreach (var existingObject in spawnedObjects)
        {
            if (existingObject != null && Vector3.Distance(position, existingObject.transform.position) < minDistanceBetweenSpawns)
            {
                return false;
            }
        }

        // Se passou por todas as verificações, o local é válido.
        return true;
    }

    [System.Serializable]
    private struct SpawnedObjectSaveData
    {
        public string prefabName;
        public float[] position;
        public float[] rotation;
        public float[] scale;
        public bool isActive;
        public bool hasSavableComponent;
        public object objectState;
    }

    public object CaptureState()
    {
        var spawnedObjectsData = new List<SpawnedObjectSaveData>();

        foreach (Transform childTransform in spawnedObjectsParent)
        {
            GameObject childObject = childTransform.gameObject;
            var saveData = new SpawnedObjectSaveData
            {
                prefabName = childObject.name.Replace("(Clone)", ""),
                position = new float[] { childObject.transform.position.x, childObject.transform.position.y, childObject.transform.position.z },
                rotation = new float[] { childObject.transform.rotation.x, childObject.transform.rotation.y, childObject.transform.rotation.z, childObject.transform.rotation.w },
                scale = new float[] { childObject.transform.localScale.x, childObject.transform.localScale.y, childObject.transform.localScale.z },
                isActive = childObject.activeSelf,
                hasSavableComponent = false,
                objectState = null
            };

            var savable = childObject.GetComponent<ISavable>();
            if (savable != null)
            {
                saveData.hasSavableComponent = true;
                saveData.objectState = savable.CaptureState();
            }

            spawnedObjectsData.Add(saveData);
        }
        return spawnedObjectsData;
    }

    public void RestoreState(object state)
    {
        hasBeenRestored = true;

        foreach (Transform child in spawnedObjectsParent)
        {
            Destroy(child.gameObject);
        }
        spawnedObjects.Clear();

        var dataList = ((JArray)state).ToObject<List<SpawnedObjectSaveData>>();

        foreach (var data in dataList)
        {
            SpawnConfiguration config = itemsToSpawn.FirstOrDefault(c => c.prefab.name == data.prefabName);
            if (config.prefab != null)
            {
                Vector3 position = new Vector3(data.position[0], data.position[1], data.position[2]);
                Quaternion rotation = new Quaternion(data.rotation[0], data.rotation[1], data.rotation[2], data.rotation[3]);
                Vector3 scale = new Vector3(data.scale[0], data.scale[1], data.scale[2]);

                GameObject newInstance = Instantiate(config.prefab, position, rotation, spawnedObjectsParent);
                newInstance.transform.localScale = scale;
                newInstance.SetActive(data.isActive);

                if (data.hasSavableComponent)
                {
                    var savable = newInstance.GetComponent<ISavable>();
                    if (savable != null)
                    {
                        savable.RestoreState(data.objectState);
                    }
                }
                spawnedObjects.Add(newInstance);
            }
        }

        if (navMeshSurface != null)
        {
            navMeshSurface.UpdateNavMesh(navMeshSurface.navMeshData);
        }
    }

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