using UnityEngine;

[RequireComponent(typeof(Animator))]
public class DoorController : MonoBehaviour
{
    [Header("Configurações da Porta")]
    [Tooltip("O colisor que bloqueia fisicamente o jogador. DEVE ter 'Is Trigger' DESMARCADO.")]
    [SerializeField] private Collider2D physicalCollider;

    [Tooltip("A tecla que o jogador deve pressionar para interagir.")]
    [SerializeField] private KeyCode interactionKey = KeyCode.E;

    private Animator animator;
    private bool playerInRange = false;
    private bool isDoorOpen = false;

    private static readonly int OpenTrigger = Animator.StringToHash("Open");
    private static readonly int CloseTrigger = Animator.StringToHash("Close");

    private void Update()
    {
        // Se o jogador está no alcance, a porta está fechada, e ele pressiona a tecla de interação...
        if (playerInRange && !isDoorOpen && Input.GetKeyDown(interactionKey))
        {
            // ...abre a porta.
            isDoorOpen = true;
            animator.SetTrigger(OpenTrigger);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Se o objeto que entrou na área de deteção é o jogador...
        if (other.CompareTag("Player"))
        {
            // ...marca que o jogador está no alcance.
            playerInRange = true;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        // Se o objeto que saiu da área de deteção é o jogador...
        if (other.CompareTag("Player"))
        {
            // ...marca que o jogador já não está no alcance.
            playerInRange = false;

            // << MUDANÇA PRINCIPAL >>
            // Se a porta estiver aberta no momento em que o jogador sai...
            if (isDoorOpen)
            {
                // ...inicia o processo de fechar a porta.
                isDoorOpen = false;
                animator.SetTrigger(CloseTrigger);
            }
        }
    }

    // ===================================================================
    //  MÉTODOS PÚBLICOS PARA SEREM CHAMADOS PELOS ANIMATION EVENTS
    // ===================================================================

    public void DisablePhysicalCollider()
    {
        if (physicalCollider != null)
        {
            physicalCollider.enabled = false;
        }
    }

    public void EnablePhysicalCollider()
    {
        if (physicalCollider != null)
        {
            physicalCollider.enabled = true;
        }
    }

    // ===================================================================
    //  CONFIGURAÇÃO INICIAL
    // ===================================================================

    private void Awake()
    {
        animator = GetComponent<Animator>();
        // Garante que a porta comece fechada e sólida
        EnablePhysicalCollider();
    }
}