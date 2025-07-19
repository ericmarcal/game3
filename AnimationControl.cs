using UnityEngine;

public class AnimationControl : MonoBehaviour
{
    [Header("Ataque do Inimigo")]
    [SerializeField] private Transform attackPoint;
    [SerializeField] private float radius;
    [SerializeField] private LayerMask playerLayer;
    [SerializeField] private float attackDamage = 1f;

    private Animator anim;
    private static readonly int TransitionHash = Animator.StringToHash("transition");
    private static readonly int AttackTriggerHash = Animator.StringToHash("Attack");

    private void Awake()
    {
        anim = GetComponent<Animator>();
    }

    public void PlayAnim(int value)
    {
        if (anim != null)
        {
            anim.SetInteger(TransitionHash, value);
        }
    }

    public void TriggerAttack()
    {
        if (anim != null)
        {
            anim.SetTrigger(AttackTriggerHash);
        }
    }

    // *** MÉTODO ATUALIZADO COM DEBUG LOGS ***
    public void Attack()
    {
        if (attackPoint == null)
        {
            Debug.LogError($"AttackPoint não foi atribuído no inimigo {gameObject.name}!", this);
            return;
        }

        // 1. Tenta encontrar o jogador na área de ataque.
        Collider2D hit = Physics2D.OverlapCircle(attackPoint.position, radius, playerLayer);

        // 2. Verifica o resultado e envia mensagens para o console.
        if (hit != null)
        {
            // Encontrou algo!
            Debug.Log($"<color=green>SUCESSO:</color> O ataque de '{gameObject.name}' atingiu o objeto '{hit.gameObject.name}'.");

            // Tenta causar dano
            if (hit.TryGetComponent<IDamageable>(out var damageable))
            {
                damageable.TakeDamage(attackDamage);
                Debug.Log($"<color=cyan>DANO:</color> {attackDamage} de dano aplicado a '{hit.gameObject.name}'.");
            }
            else
            {
                Debug.LogError($"<color=orange>AVISO:</color> O objeto atingido '{hit.gameObject.name}' não tem um componente 'IDamageable' e não pode receber dano.");
            }
        }
        else
        {
            // Não encontrou nada.
            Debug.LogError($"<color=red>FALHA:</color> O ataque de '{gameObject.name}' não atingiu nada na layer 'Player'. Verifique o raio do ataque e a layer do jogador.");
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (attackPoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(attackPoint.position, radius);
        }
    }
}