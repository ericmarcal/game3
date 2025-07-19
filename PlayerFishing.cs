using UnityEngine;
using UnityEngine.Tilemaps;

[RequireComponent(typeof(Player))]
public class PlayerFishing : MonoBehaviour
{
    [Header("Referências")]
    [Tooltip("Arraste para cá o seu Tilemap que contém a água.")]
    [SerializeField] private Tilemap waterTilemap; // Essencial que esta referência seja preenchida no Inspector

    [Header("Configurações de Pesca")]
    [SerializeField] private int fishingPercentage = 30;

    [Header("Prefab e Item do Peixe")]
    [SerializeField] private GameObject fishWorldItemPrefab;
    [SerializeField] private ItemData fishItemData;
    [SerializeField] private float fishPopForce = 2f;

    private Player player;

    private void Awake()
    {
        player = GetComponent<Player>();
    }

    // Esta função deve ser chamada pelo Player.cs quando a ação de pescar é ativada
    public void StartFishingAction()
    {
        // 1. Verificações de segurança para garantir que tudo está configurado
        if (player == null || player.grid == null || player.playerActionPoint == null)
        {
            Debug.LogError("PlayerFishing: Componente Player, Grid ou Action Point não encontrado!");
            player.FinishCurrentAction();
            return;
        }
        if (waterTilemap == null)
        {
            Debug.LogError("PlayerFishing: A referência do 'Water Tilemap' não foi atribuída no Inspector!", this);
            player.FinishCurrentAction();
            return;
        }

        // 2. Converte a posição do ponto de ação em uma coordenada do grid
        Vector3Int targetCell = player.grid.WorldToCell(player.playerActionPoint.position);

        // 3. Verifica se existe um tile de água na coordenada encontrada
        if (waterTilemap.HasTile(targetCell))
        {
            // Se encontrou água, inicia a animação e a lógica de pesca
            player.isFishing = true;
            Debug.Log("<color=cyan>PESCA:</color> Água detectada! Iniciando animação de pesca.");

            // A lógica de "pegar o peixe" será chamada pela animação através de PerformToolActionCheck()
            // Isso garante que o peixe só apareça no final da animação
            player.canCatchFish = (Random.Range(1, 101) <= fishingPercentage);
        }
        else
        {
            // Se não encontrou água, cancela a ação
            Debug.LogWarning($"<color=yellow>PESCA:</color> Nenhum tile de água encontrado na posição {targetCell}.");
            player.FinishCurrentAction();
        }
    }

    // Esta função é chamada pelo Player.cs no final da animação de pesca
    public void SpawnFishIfCaught()
    {
        if (player.canCatchFish)
        {
            Debug.Log("<color=green>PESCA:</color> Peixe fisgado! Instanciando o item.");
            InstantiateFish();
            player.canCatchFish = false; // Reseta a flag
        }
        else
        {
            Debug.Log("<color=orange>PESCA:</color> O peixe escapou desta vez.");
        }
    }

    private void InstantiateFish()
    {
        if (fishWorldItemPrefab == null || fishItemData == null) return;
        GameObject fishInstance = Instantiate(fishWorldItemPrefab, player.playerActionPoint.position, Quaternion.identity);
        WorldItem worldItemScript = fishInstance.GetComponent<WorldItem>();
        if (worldItemScript != null)
        {
            worldItemScript.itemData = fishItemData;
            worldItemScript.quantity = 1;
            Vector2 popDirection = new Vector2(Random.Range(-0.2f, 0.2f), 1f);
            worldItemScript.SetupSpawnedItemParameters(player.playerActionPoint.position, popDirection, fishPopForce);
        }
    }
}