using UnityEngine;

[RequireComponent(typeof(Animator))]
public class DoorController : MonoBehaviour
{
    [Header("Configura��es da Porta")]
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
        // Se o jogador est� no alcance, a porta est� fechada, e ele pressiona a tecla de intera��o...
        if (playerInRange && !isDoorOpen && Input.GetKeyDown(interactionKey))
        {
            // ...abre a porta.
            isDoorOpen = true;
            animator.SetTrigger(OpenTrigger);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Se o objeto que entrou na �rea de dete��o � o jogador...
        if (other.CompareTag("Player"))
        {
            // ...marca que o jogador est� no alcance.
            playerInRange = true;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        // Se o objeto que saiu da �rea de dete��o � o jogador...
        if (other.CompareTag("Player"))
        {
            // ...marca que o jogador j� n�o est� no alcance.
            playerInRange = false;

            // << MUDAN�A PRINCIPAL >>
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
    //  M�TODOS P�BLICOS PARA SEREM CHAMADOS PELOS ANIMATION EVENTS
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
    //  CONFIGURA��O INICIAL
    // ===================================================================

    private void Awake()
    {
        animator = GetComponent<Animator>();
        // Garante que a porta comece fechada e s�lida
        EnablePhysicalCollider();
    }
}