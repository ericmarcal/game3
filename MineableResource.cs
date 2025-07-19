using UnityEngine;
using System.Collections;
using Newtonsoft.Json.Linq;

[RequireComponent(typeof(UniqueID))]
public class MineableResource : MonoBehaviour, ISavable
{
    public string ID => GetComponent<UniqueID>().ID;

    [Header("Configuração do Recurso")]
    public int maxHealth = 3;
    private int currentHealth;
    public int CurrentHealth => currentHealth;
    public Sprite[] damageStagesSprites;

    [Header("Configuração do Drop")]
    public ItemData itemToDrop;
    public int quantityToDrop = 1;
    [SerializeField] private float dropPopForce = 1.5f;

    [Header("Respawn (Opcional)")]
    public bool canRespawn = true;
    public float respawnTime = 10f;

    [Header("Feedback Visual de Hit")]
    public Color hitFlashColor = Color.white;
    public float hitFlashDuration = 0.1f;
    public int numberOfFlashes = 1;
    public float shakeIntensity = 0.05f;
    public float shakeDuration = 0.15f;

    private SpriteRenderer spriteRenderer;
    private Collider2D DesteCollider2D;
    private bool isDestroyed = false;
    private UnityEngine.AI.NavMeshObstacle navMeshObstacle;
    private Color originalSpriteColor;
    private Coroutine hitFeedbackCoroutine;
    private Vector3 originalPosition;

    [System.Serializable]
    private struct ResourceSaveData
    {
        public int health;
        public bool destroyed;
        public float[] position;
    }

    public object CaptureState()
    {
        return new ResourceSaveData
        {
            health = this.currentHealth,
            destroyed = this.isDestroyed,
            position = new float[] { transform.position.x, transform.position.y, transform.position.z }
        };
    }

    public void RestoreState(object state)
    {
        var saveData = ((JObject)state).ToObject<ResourceSaveData>();
        this.currentHealth = saveData.health;
        this.isDestroyed = saveData.destroyed;

        transform.position = new Vector3(saveData.position[0], saveData.position[1], saveData.position[2]);

        if (this.isDestroyed)
        {
            gameObject.SetActive(false);
            if (canRespawn)
            {
                StartCoroutine(RespawnTimer());
            }
        }
        else
        {
            UpdateSprite();
            gameObject.SetActive(true);
        }
    }

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        DesteCollider2D = GetComponent<Collider2D>();
        if (TryGetComponent<UnityEngine.AI.NavMeshObstacle>(out var obstacle)) { navMeshObstacle = obstacle; }
        if (spriteRenderer != null) { originalSpriteColor = spriteRenderer.color; }
        originalPosition = transform.position;
        InitializeResource();
    }

    void InitializeResource()
    {
        currentHealth = maxHealth;
        isDestroyed = false;
        if (DesteCollider2D != null) DesteCollider2D.enabled = true;
        if (navMeshObstacle != null) navMeshObstacle.enabled = true;
        UpdateSprite();
        transform.position = originalPosition;
        gameObject.SetActive(true);
    }

    public void OnHit(GameObject hitter, int damageAmount = 1)
    {
        if (isDestroyed || currentHealth <= 0) return;
        currentHealth -= damageAmount;
        if (hitFeedbackCoroutine != null) StopCoroutine(hitFeedbackCoroutine);
        hitFeedbackCoroutine = StartCoroutine(HitFeedbackCoroutine());
        UpdateSprite();

        if (currentHealth <= 0)
        {
            if (hitter.TryGetComponent<ItemContainer>(out var itemContainer) && hitter.CompareTag("NPC"))
            {
                itemContainer.AddItem(itemToDrop, quantityToDrop);
                DestroyResource(false);
            }
            else
            {
                DestroyResource(true);
            }
        }
    }

    IEnumerator HitFeedbackCoroutine()
    {
        originalPosition = transform.position;
        if (spriteRenderer != null)
        {
            for (int i = 0; i < numberOfFlashes; i++)
            {
                spriteRenderer.color = hitFlashColor;
                yield return new WaitForSeconds(hitFlashDuration / (numberOfFlashes * 2));
                spriteRenderer.color = originalSpriteColor;
                yield return new WaitForSeconds(hitFlashDuration / (numberOfFlashes * 2));
            }
            spriteRenderer.color = originalSpriteColor;
        }

        float elapsed = 0.0f;
        while (elapsed < shakeDuration)
        {
            float x = originalPosition.x + Random.Range(-1f, 1f) * shakeIntensity;
            float y = originalPosition.y + Random.Range(-1f, 1f) * shakeIntensity;
            transform.position = new Vector3(x, y, originalPosition.z);
            elapsed += Time.deltaTime;
            yield return null;
        }
        transform.position = originalPosition;
        hitFeedbackCoroutine = null;
    }

    void UpdateSprite()
    {
        if (spriteRenderer == null || damageStagesSprites == null || damageStagesSprites.Length == 0) return;
        int spriteIndex = Mathf.Max(0, maxHealth - currentHealth);
        if (currentHealth <= 0) spriteIndex = damageStagesSprites.Length - 1;
        spriteIndex = Mathf.Clamp(spriteIndex, 0, damageStagesSprites.Length - 1);
        if (damageStagesSprites[spriteIndex] != null) spriteRenderer.sprite = damageStagesSprites[spriteIndex];
    }

    void DestroyResource(bool shouldDropItem)
    {
        isDestroyed = true;

        if (shouldDropItem) DropItems();

        if (canRespawn)
        {
            gameObject.SetActive(true);
            StartCoroutine(RespawnTimer());
        }
        else
        {
            Destroy(gameObject, 0.1f);
        }

        if (spriteRenderer != null) spriteRenderer.enabled = false;
        if (DesteCollider2D != null) DesteCollider2D.enabled = false;
    }

    void DropItems()
    {
        if (itemToDrop == null || itemToDrop.itemPrefab == null || quantityToDrop <= 0) return;
        Vector3 dropPosition = transform.position;
        GameObject droppedItemGO = Instantiate(itemToDrop.itemPrefab, dropPosition, Quaternion.identity);
        WorldItem worldItemScript = droppedItemGO.GetComponent<WorldItem>();
        if (worldItemScript != null)
        {
            worldItemScript.itemData = itemToDrop;
            worldItemScript.quantity = quantityToDrop;
            Vector2 popDirection = new Vector2(Random.Range(-0.5f, 0.5f), 1f);
            worldItemScript.SetupSpawnedItemParameters(dropPosition, popDirection, dropPopForce);
        }
    }

    IEnumerator RespawnTimer()
    {
        if (spriteRenderer != null) spriteRenderer.enabled = false;
        if (DesteCollider2D != null) DesteCollider2D.enabled = false;

        yield return new WaitForSeconds(respawnTime);
        InitializeResource();
    }

    // << BLOCO DE REGISTO ADICIONADO >>
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