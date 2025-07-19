using UnityEngine;

public class LumberjackBehavior : MonoBehaviour, INPCBehavior
{
    [Header("Configurações do Lenhador")]
    [SerializeField] private float workSearchRadius = 10f;
    [SerializeField] private float workDuration = 4f;
    [SerializeField] private float workStoppingDistance = 2f;

    // << CAMPO RESTAURADO AQUI >>
    [Tooltip("O prefab do BALÃO DE AÇÃO (que já contém o ícone dentro) para esta profissão.")]
    [SerializeField] private GameObject workIconPrefab;

    [SerializeField] private ItemData resourceItemData;

    public void OnWorkCompleted(Transform target, GameObject npc)
    {
        Tree treeComponent = target?.GetComponent<Tree>();
        if (treeComponent != null)
        {
            treeComponent.OnHit(npc);
        }
    }

    public bool FindWorkTarget(Vector3 searchPosition, out Transform target)
    {
        target = null;
        Collider2D[] hits = Physics2D.OverlapCircleAll(searchPosition, workSearchRadius);
        Transform closestTree = null;
        float closestDist = Mathf.Infinity;
        foreach (var hit in hits)
        {
            Tree tree = hit.GetComponent<Tree>();
            if (tree != null && tree.CurrentHealth > 0)
            {
                float dist = Vector3.Distance(searchPosition, hit.transform.position);
                if (dist < closestDist)
                {
                    closestDist = dist;
                    closestTree = hit.transform;
                }
            }
        }
        if (closestTree != null)
        {
            target = closestTree;
            return true;
        }
        return false;
    }

    public float GetWorkStoppingDistance()
    {
        return workStoppingDistance;
    }

    public float GetWorkDuration()
    {
        return workDuration;
    }

    public GameObject GetWorkIconPrefab()
    {
        return workIconPrefab;
    }

    public ItemData GetResourceItemData()
    {
        return resourceItemData;
    }
}