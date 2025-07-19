using UnityEngine;

public class FishingPoint : MonoBehaviour
{
    [Header("Configuração da Pesca")]
    public GameObject fishWorldItemPrefab;
    [SerializeField] private ItemData fishItemDataToDrop;
    [SerializeField] private float fishPopForce = 2f;
    private Player playerScript;

    private void Start()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) playerScript = playerObj.GetComponent<Player>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (playerScript == null) return;
        if (other.CompareTag("water"))
        {
            if (playerScript.canCatchFish)
            {
                InstantiateFish();
                playerScript.canCatchFish = false;
            }
        }
    }

    private void InstantiateFish()
    {
        if (fishWorldItemPrefab == null || fishItemDataToDrop == null) return;
        Vector3 spawnPositionAnzol = transform.position;
        GameObject fishInstance = Instantiate(fishWorldItemPrefab, spawnPositionAnzol, Quaternion.identity);
        WorldItem worldItemScript = fishInstance.GetComponent<WorldItem>();
        if (worldItemScript != null)
        {
            worldItemScript.itemData = fishItemDataToDrop;
            worldItemScript.quantity = 1;
            Vector2 popDirection = new Vector2(Random.Range(-0.2f, 0.2f), 1f);
            worldItemScript.SetupSpawnedItemParameters(spawnPositionAnzol, popDirection, fishPopForce);
        }
    }
}