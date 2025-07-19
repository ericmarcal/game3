using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Collider2D))]
public class ClearableResource : MonoBehaviour, IDamageable
{
    [Header("Configuração do Recurso")]
    [Tooltip("A ferramenta necessária para limpar este recurso. 'None' significa que qualquer ferramenta ou o ataque básico funciona.")]
    [SerializeField] private ToolType requiredTool = ToolType.None;

    [Header("Recompensa")]
    [Tooltip("O item que será dropado quando este recurso for limpo.")]
    [SerializeField] private ItemData itemToDrop;
    [Tooltip("A quantidade de itens a serem dropados.")]
    [SerializeField] private int dropAmount = 1;

    [Header("Efeitos Visuais")]
    [Tooltip("Opcional: Um prefab com um sistema de partículas (ex: fumo, folhas) a ser tocado ao ser destruído.")]
    [SerializeField] private GameObject breakEffectPrefab;
    [Tooltip("A intensidade do efeito de tremer ao ser atingido.")]
    [SerializeField] private float shakeIntensity = 0.05f;
    [Tooltip("A duração em segundos do efeito de tremer.")]
    [SerializeField] private float shakeDuration = 0.2f;

    private bool isBeingCleared = false;
    private Vector3 originalPosition;

    private void Awake()
    {
        originalPosition = transform.position;
    }

    public void TakeDamage(float damageAmount)
    {
        // Impede que o objeto seja destruído várias vezes se for atingido rapidamente.
        if (isBeingCleared) return;

        // Verifica se a ferramenta está correta.
        if (requiredTool != ToolType.None && Player.instance.currentTool != requiredTool)
        {
            return;
        }

        isBeingCleared = true;
        StartCoroutine(ClearAndDestroyRoutine());
    }

    private IEnumerator ClearAndDestroyRoutine()
    {
        // Inicia o efeito de tremer.
        StartCoroutine(ShakeEffectCoroutine());

        // Espera a duração do "shake" para dar tempo de o efeito ser visível.
        yield return new WaitForSeconds(shakeDuration);

        // Dropa o item de recompensa.
        if (itemToDrop != null && itemToDrop.itemPrefab != null)
        {
            Vector3 spawnPosition = transform.position;
            for (int i = 0; i < dropAmount; i++)
            {
                GameObject itemInstance = Instantiate(itemToDrop.itemPrefab, spawnPosition, Quaternion.identity);
                WorldItem worldItem = itemInstance.GetComponent<WorldItem>();
                if (worldItem != null)
                {
                    worldItem.itemData = itemToDrop;
                    worldItem.quantity = 1;
                    Vector2 popDirection = new Vector2(Random.Range(-0.5f, 0.5f), 1f);
                    worldItem.SetupSpawnedItemParameters(spawnPosition, popDirection, 1.5f);
                }
            }
        }

        // Toca o efeito de partículas, se houver um.
        if (breakEffectPrefab != null)
        {
            Instantiate(breakEffectPrefab, transform.position, Quaternion.identity);
        }

        // Finalmente, destrói o objeto.
        Destroy(gameObject);
    }

    private IEnumerator ShakeEffectCoroutine()
    {
        float elapsed = 0.0f;
        while (elapsed < shakeDuration)
        {
            float x = originalPosition.x + Random.Range(-1f, 1f) * shakeIntensity;
            float y = originalPosition.y + Random.Range(-1f, 1f) * shakeIntensity;
            transform.position = new Vector3(x, y, originalPosition.z);
            elapsed += Time.deltaTime;
            yield return null;
        }
        // Garante que a posição volte ao normal caso o objeto não seja destruído.
        transform.position = originalPosition;
    }

    public bool IsDead()
    {
        return false;
    }
}