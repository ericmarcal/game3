using UnityEngine;

public class MerchantBehavior : MonoBehaviour, INPCBehavior
{
    // Este NPC n�o procura alvos de trabalho, ent�o sempre retorna falso.
    public bool FindWorkTarget(Vector3 searchPosition, out Transform target)
    {
        target = null;
        return false;
    }

    // A a��o de trabalho dele � simplesmente n�o fazer nada.
    public void OnWorkCompleted(Transform target, GameObject npc) { }

    // O resto dos m�todos retorna valores padr�o que n�o ser�o usados.
    public float GetWorkStoppingDistance() => 0f;
    public float GetWorkDuration() => 0f;
    public GameObject GetWorkIconPrefab() => null;
    public ItemData GetResourceItemData() => null;
}