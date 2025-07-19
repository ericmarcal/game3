using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

public class DecorationSpawner : MonoBehaviour
{
    // Uma estrutura para definir cada tipo de item que pode ser spawnado
    [System.Serializable]
    public class SpawnableObject
    {
        public enum SpawnType { GameObject, Tile }
        [Tooltip("Escolha se quer spawnar um GameObject (como uma árvore) ou pintar um Tile (como grama animada).")]
        public SpawnType type = SpawnType.GameObject;

        [Tooltip("Use se o 'Type' for GameObject.")]
        public GameObject gameObjectPrefab;
        [Tooltip("Use se o 'Type' for Tile. Pode ser qualquer tile, incluindo Animated Tiles.")]
        public TileBase tileBase;

        [Tooltip("Uma chance maior em relação aos outros itens na lista significa que este aparecerá com mais frequência.")]
        public float weight = 1f; // Peso/chance de spawnar este item
    }

    [Header("Itens para Spawnar")]
    [Tooltip("A lista de todos os possíveis itens (GameObjects ou Tiles) que este spawner pode criar.")]
    public List<SpawnableObject> itemsToSpawn = new List<SpawnableObject>();

    [Header("Parâmetros de Spawn")]
    [Tooltip("Número aproximado de decorações a serem instanciadas.")]
    public int quantidadeParaSpawnar = 50;
    [Tooltip("Tamanho da área retangular (Largura X, Altura Y) onde as decorações serão spawnadas.")]
    public Vector2 areaDeSpawn = new Vector2(10f, 10f);
    [Tooltip("Opcional: Um GameObject pai para organizar os GameObjects instanciados.")]
    public Transform parenteDosObjetosSpawnados;

    [Header("Validação de Posição")]
    [Tooltip("O Tilemap principal onde os itens podem ser spawnados (ex: Tilemap de Chão/Grama).")]
    public Tilemap tilemapSpawnavel;
    [Tooltip("Opcional: Tilemap onde os itens NÃO PODEM ser spawnados (ex: Tilemap de Água, Caminhos).")]
    public Tilemap tilemapNaoSpawnavel;
    [Tooltip("Camadas que são consideradas obstáculos e onde os itens não devem spawnar (ex: árvores, pedras grandes).")]
    public LayerMask layerDeObstaculos;
    [Tooltip("Raio da checagem de overlap para obstáculos. Deve ser pequeno para decorações.")]
    public float raioDeChecagemDeOverlap = 0.1f;

    [Header("Ajustes Visuais (Opcional - para GameObjects)")]
    [Tooltip("Permite rotações aleatórias no eixo Z.")]
    public bool rotacaoAleatoria = false;
    public float maxRandomRotationZ = 15f;
    [Tooltip("Permite variação aleatória na escala.")]
    public bool escalaAleatoria = false;
    public Vector2 rangeEscalaX = new Vector2(0.8f, 1.2f);
    public Vector2 rangeEscalaY = new Vector2(0.8f, 1.2f);
    [Tooltip("Define um Sorting Order base para os sprites de GameObjects instanciados.")]
    public int sortingOrderBase = 0;
    [Tooltip("Usa a posição Y para ajustar a ordem de renderização (efeito de profundidade).")]
    public bool usarPosicaoYParaSorting = true;

    private float totalWeight;

    // Removemos Start() para que ele funcione apenas como uma ferramenta de editor
    // a menos que você queira que ele gere coisas no início do jogo.

    private void CalculateTotalWeight()
    {
        totalWeight = 0f;
        if (itemsToSpawn == null) return;
        foreach (var spawnable in itemsToSpawn)
        {
            totalWeight += spawnable.weight;
        }
    }

    [ContextMenu("Gerar Decorações")]
    public void SpawnDecorations()
    {
        ClearDecorations();

        if (itemsToSpawn == null || itemsToSpawn.Count == 0)
        {
            Debug.LogWarning($"DecorationSpawner em '{gameObject.name}': Nenhum item configurado em 'Items To Spawn'.", this);
            return;
        }

        if (parenteDosObjetosSpawnados == null)
        {
            parenteDosObjetosSpawnados = this.transform;
        }

        CalculateTotalWeight();

        int spawnedCount = 0;
        int maxAttemptsPerItem = 20;

        for (int i = 0; i < quantidadeParaSpawnar; i++)
        {
            for (int attempt = 0; attempt < maxAttemptsPerItem; attempt++)
            {
                float randomX = Random.Range(-areaDeSpawn.x / 2, areaDeSpawn.x / 2);
                float randomY = Random.Range(-areaDeSpawn.y / 2, areaDeSpawn.y / 2);
                Vector3 potentialSpawnPosition = transform.position + new Vector3(randomX, randomY, 0);

                if (IsValidSpawnLocation(potentialSpawnPosition))
                {
                    SpawnableObject objectToSpawn = GetRandomSpawnable();
                    if (objectToSpawn == null) continue;

                    if (objectToSpawn.type == SpawnableObject.SpawnType.GameObject)
                    {
                        if (objectToSpawn.gameObjectPrefab != null)
                        {
                            GameObject instance = Instantiate(objectToSpawn.gameObjectPrefab, potentialSpawnPosition, Quaternion.identity, parenteDosObjetosSpawnados);
                            ApplyVisualAdjustments(instance);
                        }
                    }
                    else if (objectToSpawn.type == SpawnableObject.SpawnType.Tile)
                    {
                        if (objectToSpawn.tileBase != null && tilemapSpawnavel != null)
                        {
                            Vector3Int cellPosition = tilemapSpawnavel.WorldToCell(potentialSpawnPosition);
                            // Checa se a célula já está ocupada no tilemap alvo antes de pintar
                            if (!tilemapSpawnavel.HasTile(cellPosition))
                            {
                                tilemapSpawnavel.SetTile(cellPosition, objectToSpawn.tileBase);
                            }
                            else
                            {
                                continue; // Posição de tile já ocupada, tenta outra
                            }
                        }
                    }

                    spawnedCount++;
                    break;
                }
            }
        }
        Debug.Log($"DecorationSpawner '{gameObject.name}': Geração concluída. {spawnedCount}/{quantidadeParaSpawnar} itens instanciados/pintados.", this);
    }

    [ContextMenu("Limpar Decorações (Apenas GameObjects)")]
    public void ClearDecorations()
    {
        Transform parent = (parenteDosObjetosSpawnados != null) ? parenteDosObjetosSpawnados : this.transform;

        // Usar um loop for reverso é mais seguro ao destruir objetos de uma lista ou filhos
        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(parent.GetChild(i).gameObject);
        }

        Debug.LogWarning("A limpeza de TILES não é automática. Para limpar os tiles, use a borracha no Tile Palette ou limpe o Tilemap dedicado manualmente.");
    }

    private SpawnableObject GetRandomSpawnable()
    {
        if (totalWeight <= 0) return null;
        float randomValue = Random.Range(0, totalWeight);
        foreach (var spawnable in itemsToSpawn)
        {
            if (randomValue <= spawnable.weight)
            {
                return spawnable;
            }
            randomValue -= spawnable.weight;
        }
        return null;
    }

    bool IsValidSpawnLocation(Vector3 position)
    {
        if (tilemapSpawnavel != null)
        {
            Vector3Int cellPosition = tilemapSpawnavel.WorldToCell(position);
            if (!tilemapSpawnavel.HasTile(cellPosition)) return false;
            if (tilemapNaoSpawnavel != null && tilemapNaoSpawnavel.HasTile(tilemapNaoSpawnavel.WorldToCell(position))) return false;
        }
        if (layerDeObstaculos.value != 0)
        {
            if (Physics2D.OverlapCircle(position, raioDeChecagemDeOverlap, layerDeObstaculos)) return false;
        }
        return true;
    }

    void ApplyVisualAdjustments(GameObject decoInstance)
    {
        if (rotacaoAleatoria)
        {
            float randomZ = Random.Range(-maxRandomRotationZ, maxRandomRotationZ);
            decoInstance.transform.eulerAngles = new Vector3(0, 0, randomZ);
        }
        if (escalaAleatoria)
        {
            float newScaleX = Random.Range(rangeEscalaX.x, rangeEscalaY.y);
            float newScaleY = Random.Range(rangeEscalaY.x, rangeEscalaY.y);
            decoInstance.transform.localScale = new Vector3(newScaleX, newScaleY, 1f);
        }

        SpriteRenderer sr = decoInstance.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            if (usarPosicaoYParaSorting)
            {
                sr.sortingOrder = sortingOrderBase - Mathf.RoundToInt(decoInstance.transform.position.y * 10);
            }
            else
            {
                sr.sortingOrder = sortingOrderBase;
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.3f, 1f, 0.3f, 0.4f);
        Vector3 center = transform.position;
        center.z = 0;
        Vector3 size = new Vector3(areaDeSpawn.x, areaDeSpawn.y, 0.1f);
        Gizmos.DrawCube(center, size);
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(center, size);
    }
}