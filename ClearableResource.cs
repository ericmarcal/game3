using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Collider2D))]
public class ClearableResource : MonoBehaviour, IDamageable
{
    [Header("Configura��o do Recurso")]
    [Tooltip("A ferramenta necess�ria para limpar este recurso. 'None' significa que qualquer ferramenta ou o ataque b�sico funciona.")]
    [SerializeField] private ToolType requiredTool = ToolType.None;

    [Header("Recompensa")]
    [Tooltip("O item que ser� dropado quando este recurso for limpo.")]
    [SerializeField] private ItemData itemToDrop;
    [Tooltip("A quantidade de itens a serem dropados.")]
    [SerializeField] private int dropAmount = 1;

    [Header("Efeitos Visuais")]
    [Tooltip("Opcional: Um prefab com um sistema de part�culas (ex: fumo, folhas) a ser tocado ao ser destru�do.")]
    [SerializeField] private GameObject breakEffectPrefab;
    [Tooltip("A intensidade do efeito de tremer ao ser atingido.")]
    [SerializeField] private float shakeIntensity = 0.05f;
    [Tooltip("A dura��o em segundos do efeito de tremer.")]
    [SerializeField] private float shakeDuration = 0.2f;

    private bool isBeingCleared = false;
    private Vector3 originalPosition;

    private void Awake()
    {
        originalPosition = transform.position;
    }

    public void TakeDamage(float damageAmount)
    {
        // Impede que o objeto seja destru�do v�rias vezes se for atingido rapidamente.
        if (isBeingCleared) return;

        // Verifica se a ferramenta est� correta.
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

        // Espera a dura��o do "shake" para dar tempo de o efeito ser vis�vel.
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

        // Toca o efeito de part�culas, se houver um.
        if (breakEffectPrefab != null)
        {
            Instantiate(breakEffectPrefab, transform.position, Quaternion.identity);
        }

        // Finalmente, destr�i o objeto.
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
        // Garante que a posi��o volte ao normal caso o objeto n�o seja destru�do.
        transform.position = originalPosition;
    }

    public bool IsDead()
    {
        return false;
    }
}