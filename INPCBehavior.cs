using UnityEngine;

public interface INPCBehavior
{
    bool FindWorkTarget(Vector3 searchPosition, out Transform target);
    float GetWorkStoppingDistance();
    float GetWorkDuration();
    GameObject GetWorkIconPrefab();
    void OnWorkCompleted(Transform target, GameObject npc);
    ItemData GetResourceItemData();
}