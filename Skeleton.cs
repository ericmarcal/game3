using UnityEngine;
using System.Collections;
using UnityEngine.AI;

public class Skeleton : MonoBehaviour, IDamageable
{
    [Header("Atributos do Esqueleto")]
    [SerializeField] private float maxHealth = 3f;
    private float currentHealth;

    [Header("Feedback Visual de Dano")]
    [SerializeField] private Color damageColor = Color.red;
    [SerializeField] private float flashDuration = 0.1f;
    [SerializeField] private int numberOfFlashes = 2;

    // Referências
    private EnemyController aiController;
    private AnimationControl animControl;
    private SpriteRenderer spriteRenderer;
    private Color originalSpriteColor;
    private Coroutine damageFlashCoroutine;

    public bool IsDead() => currentHealth <= 0;

    private void Awake()
    {
        aiController = GetComponent<EnemyController>();
        animControl = GetComponent<AnimationControl>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    void Start()
    {
        currentHealth = maxHealth;
        if (spriteRenderer != null) originalSpriteColor = spriteRenderer.color;
    }

    public void TakeDamage(float damageAmount)
    {
        if (IsDead()) return;
        currentHealth -= damageAmount;

        if (aiController != null)
        {
            aiController.OnTakeDamage();
        }

        StartCoroutine(DamageFlashFeedback());
        if (currentHealth <= 0) Die();
    }

    private void Die()
    {
        this.enabled = false;
        GetComponent<Collider2D>().enabled = false;

        NavMeshAgent agent = GetComponent<NavMeshAgent>();
        if (agent != null) agent.enabled = false;

        if (aiController != null) aiController.enabled = false;

        if (animControl != null) animControl.PlayAnim(3);
        Destroy(gameObject, 2f);
    }

    private IEnumerator DamageFlashFeedback()
    {
        for (int i = 0; i < numberOfFlashes; i++)
        {
            spriteRenderer.color = damageColor;
            yield return new WaitForSeconds(flashDuration);
            spriteRenderer.color = originalSpriteColor;
            yield return new WaitForSeconds(flashDuration);
        }
    }

    // *** FUNÇÃO ADICIONADA AQUI ***
    // Esta é a função que a sua animação 'attack_skeleton' estava a procurar.
    public void OnAttackFinished()
    {
        // Avisa o controlador de IA que a animação de ataque terminou.
        if (aiController != null)
        {
            aiController.OnAttackAnimationFinished();
        }
    }
}