using UnityEngine;

public class MerchantBehavior : MonoBehaviour, INPCBehavior
{
    // Este NPC não procura alvos de trabalho, então sempre retorna falso.
    public bool FindWorkTarget(Vector3 searchPosition, out Transform target)
    {
        target = null;
        return false;
    }

    // A ação de trabalho dele é simplesmente não fazer nada.
    public void OnWorkCompleted(Transform target, GameObject npc) { }

    // O resto dos métodos retorna valores padrão que não serão usados.
    public float GetWorkStoppingDistance() => 0f;
    public float GetWorkDuration() => 0f;
    public GameObject GetWorkIconPrefab() => null;
    public ItemData GetResourceItemData() => null;
}