using UnityEngine;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(ItemContainer))]
public class ChestInteraction : MonoBehaviour
{
    [Header("Configuração de Interação")]
    [SerializeField] private KeyCode interactionKey = KeyCode.E;

    [Header("Configuração de Animação")]
    [Tooltip("O nome EXATO do estado de animação de ABRIR no Animator.")]
    [SerializeField] private string openAnimationName = "Chest_Open";
    [Tooltip("O nome EXATO do estado de animação de FECHAR no Animator.")]
    [SerializeField] private string closeAnimationName = "Chest_Close";

    private bool playerInRange = false;
    private bool isChestOpen = false;

    private Animator animator;
    private ItemContainer itemContainer;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        itemContainer = GetComponent<ItemContainer>();
    }

    private void Update()
    {
        // A lógica para o player interagir quando está no alcance permanece a mesma
        // mas agora é gerenciada pelo Player.cs, esta parte pode ser simplificada ou removida
        // dependendo de como a chamada é feita. Manteremos por segurança.
        if (playerInRange && Input.GetKeyDown(interactionKey))
        {
            ToggleChest();
        }

        // Permite fechar com ESC
        if (isChestOpen && Input.GetKeyDown(KeyCode.Escape))
        {
            CloseChest();
        }
    }

    // Este método é chamado pelo Player.cs
    public void ToggleChest()
    {
        isChestOpen = !isChestOpen;
        if (isChestOpen)
        {
            OpenChest();
        }
        else
        {
            CloseChest();
        }
    }

    private void OpenChest()
    {
        isChestOpen = true;

        // << CORREÇÃO AQUI >>
        // Só toca a animação se o objeto NÃO tiver a tag "NPC"
        if (!gameObject.CompareTag("NPC"))
        {
            animator.Play(openAnimationName);
        }

        ChestUIManager.instance.OpenChestUI(itemContainer);
    }

    private void CloseChest()
    {
        isChestOpen = false;

        // << CORREÇÃO AQUI >>
        // Só toca a animação se o objeto NÃO tiver a tag "NPC"
        if (!gameObject.CompareTag("NPC"))
        {
            animator.Play(closeAnimationName);
        }

        ChestUIManager.instance.CloseChestUI();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = true;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = false;
            if (isChestOpen)
            {
                CloseChest();
            }
        }
    }
}