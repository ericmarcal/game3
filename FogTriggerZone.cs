using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class FogTriggerZone : MonoBehaviour
{
    [Header("Configuração do Teleporte")]
    [SerializeField] private Transform teleportTarget;
    public bool isQuestCompleted = false;

    private void Awake()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    private void Update()
    {
        if (isQuestCompleted)
        {
            gameObject.SetActive(false);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isQuestCompleted || !other.CompareTag("Player")) return;

        // LOG: Confirma que o jogador entrou no gatilho.
        Debug.Log("<color=blue>TRIGGER_ZONE:</color> Player entrou na zona. A chamar StartFogEffects.");

        if (FogManager.instance != null)
        {
            FogManager.instance.StartFogEffects();
        }

        Player player = other.GetComponent<Player>();
        if (player != null && teleportTarget != null)
        {
            player.TeleportTo(teleportTarget.position);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (isQuestCompleted || !other.CompareTag("Player")) return;

        // LOG: Confirma que o jogador saiu do gatilho.
        Debug.Log("<color=purple>TRIGGER_ZONE:</color> Player saiu da zona. A chamar StopFogEffects.");

        if (FogManager.instance != null)
        {
            FogManager.instance.StopFogEffects();
        }
    }
}