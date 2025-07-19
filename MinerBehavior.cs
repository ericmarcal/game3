using UnityEngine;

public class MinerBehavior : MonoBehaviour, INPCBehavior
{
    [Header("Configurações do Minerador")]
    [SerializeField] private float workSearchRadius = 10f;
    [SerializeField] private float workDuration = 4f;
    [SerializeField] private float workStoppingDistance = 2f;

    [Tooltip("O prefab do BALÃO DE AÇÃO (com o ícone de picareta) para esta profissão.")]
    [SerializeField] private GameObject workIconPrefab;

    [Tooltip("O ItemData do minério/pedra que o NPC deve procurar.")]
    [SerializeField] private ItemData resourceItemData;

    // Lógica para encontrar o alvo de trabalho
    public bool FindWorkTarget(Vector3 searchPosition, out Transform target)
    {
        target = null;
        Collider2D[] hits = Physics2D.OverlapCircleAll(searchPosition, workSearchRadius);
        Transform closestResource = null;
        float closestDist = Mathf.Infinity;

        foreach (var hit in hits)
        {
            // A única diferença: procura por MineableResource em vez de Tree
            MineableResource resource = hit.GetComponent<MineableResource>();
            if (resource != null && resource.CurrentHealth > 0)
            {
                float dist = Vector3.Distance(searchPosition, hit.transform.position);
                if (dist < closestDist)
                {
                    closestDist = dist;
                    closestResource = hit.transform;
                }
            }
        }

        if (closestResource != null)
        {
            target = closestResource;
            return true;
        }

        return false;
    }

    // Lógica para quando o trabalho for concluído
    public void OnWorkCompleted(Transform target, GameObject npc)
    {
        // A única diferença: chama o OnHit do MineableResource
        target?.GetComponent<MineableResource>()?.OnHit(npc);
    }

    // O resto dos métodos são iguais aos do Lenhador
    public float GetWorkStoppingDistance() => workStoppingDistance;
    public float GetWorkDuration() => workDuration;
    public GameObject GetWorkIconPrefab() => workIconPrefab;
    public ItemData GetResourceItemData() => resourceItemData;
}