// WorldItem.cs
using UnityEngine;
using System.Collections;

[RequireComponent(typeof(SpriteRenderer), typeof(Collider2D))]
public class WorldItem : MonoBehaviour
{
    [Header("Configuração do Item")]
    public ItemData itemData;
    public int quantity;

    [Header("Coleta e Comportamento")]
    public float pickupDelay = 0.5f;
    public bool isMagnetic = true;

    [Header("Distância e Velocidade de Coleta")]
    [SerializeField] private float magnetDistance = 2.0f;
    [SerializeField] private float magnetSpeed = 2.5f;

    private bool canBeCollected = false;
    private bool isCollected = false;
    private Transform playerTransform;
    private Collider2D col;
    private bool setupCalled = false; // Flag para controlar se a inicialização foi chamada

    private void Awake()
    {
        col = GetComponent<Collider2D>();
        col.isTrigger = true;

        int targetLayer = LayerMask.NameToLayer("DroppedItem");
        if (targetLayer != -1) gameObject.layer = targetLayer;
        else Debug.LogError("A Layer 'DroppedItem' não foi encontrada!");

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) playerTransform = playerObj.transform;
    }

    // *** LÓGICA DE FALLBACK ADICIONADA AQUI ***
    private void Start()
    {
        // Se nenhuma função de setup foi chamada pelo spawner,
        // inicia um timer de segurança para garantir que o item possa ser coletado.
        if (!setupCalled)
        {
            StartCoroutine(EnableCollectionAfterDelay());
        }
    }

    // Função para itens de recursos (árvores, etc.)
    public void SetupSpawnedItemParameters(Vector3 initialPosition, Vector2 forceDirection, float popForce)
    {
        setupCalled = true; // Marca que a inicialização foi feita
        GetComponent<SpriteRenderer>().sprite = itemData.icon;
        transform.position = initialPosition;

        if (!TryGetComponent<Rigidbody2D>(out var rb))
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
        }

        rb.isKinematic = false;
        rb.gravityScale = 0f;
        rb.velocity = Vector2.zero;
        rb.AddForce(forceDirection.normalized * popForce, ForceMode2D.Impulse);

        if (gameObject.activeInHierarchy)
        {
            StartCoroutine(SettlePhysicsCoroutine(rb));
        }
    }

    private IEnumerator SettlePhysicsCoroutine(Rigidbody2D rbToSettle)
    {
        // A contagem de tempo agora é reutilizada pela função de fallback
        yield return StartCoroutine(EnableCollectionAfterDelay());

        if (rbToSettle != null)
        {
            Destroy(rbToSettle);
        }
    }

    // Função para itens do inventário
    public void InitializeAsInventoryDrop(Vector2 direction)
    {
        setupCalled = true; // Marca que a inicialização foi feita
        this.isMagnetic = false;
        GetComponent<SpriteRenderer>().sprite = itemData.icon;

        if (TryGetComponent<Rigidbody2D>(out var rb))
        {
            Destroy(rb);
        }

        StartCoroutine(MoveForwardCoroutine(direction));
    }

    private IEnumerator MoveForwardCoroutine(Vector2 direction)
    {
        float dropDistance = GameSettings.instance.inventoryDropDistance;
        float dropSpeed = GameSettings.instance.inventoryDropSpeed;

        float dropDuration = (dropSpeed > 0) ? dropDistance / dropSpeed : 0;

        Vector3 startPosition = transform.position;
        Vector3 endPosition = startPosition + ((Vector3)direction * dropDistance);
        float timer = 0f;

        while (timer < dropDuration)
        {
            timer += Time.deltaTime;
            transform.position = Vector3.Lerp(startPosition, endPosition, timer / dropDuration);
            yield return null;
        }

        transform.position = endPosition;
        canBeCollected = true;
    }

    // Corotina reutilizável para habilitar a coleta
    private IEnumerator EnableCollectionAfterDelay()
    {
        yield return new WaitForSeconds(pickupDelay);
        canBeCollected = true;
    }

    void Update()
    {
        if (!canBeCollected || isCollected || playerTransform == null || !itemData.isCollectible) return;

        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);

        if (isMagnetic && distanceToPlayer <= magnetDistance)
        {
            if (PlayerItens.instance != null && PlayerItens.instance.CanAddItem(itemData, quantity))
            {
                transform.position = Vector3.MoveTowards(transform.position, playerTransform.position, magnetSpeed * Time.deltaTime);
            }
        }
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (!canBeCollected || isCollected) return;

        if (other.CompareTag("Player"))
        {
            TryCollect();
        }
    }

    public void TryCollect()
    {
        if (isCollected || itemData == null || !itemData.isCollectible) return;

        if (PlayerItens.instance != null && PlayerItens.instance.CanAddItem(itemData, quantity))
        {
            int remaining = PlayerItens.instance.AddItem(itemData, quantity);
            if (remaining == 0)
            {
                isCollected = true;
                Destroy(gameObject);
            }
            else
            {
                quantity = remaining;
            }
        }
    }
}