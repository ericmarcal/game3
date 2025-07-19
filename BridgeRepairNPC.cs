using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System.Linq;

[RequireComponent(typeof(UniqueID))]
public class BridgeRepairNPC : MonoBehaviour, ISavable
{
    public string ID => GetComponent<UniqueID>().ID;

    [Header("Configuração da Ponte")]
    [SerializeField] private GameObject completeBridgePrefab;
    [SerializeField] private Transform bridgeSpawnPoint;
    [SerializeField] private float buildDelay = 0.1f;

    [Header("Custo da Reparação")]
    [SerializeField] private List<RepairCost> repairCosts;

    [Header("Interação e Aparência")]
    [SerializeField] private bool comecarVirado = false;
    [SerializeField] private GameObject disappearEffectPrefab;
    [SerializeField] private KeyCode interactionKey = KeyCode.E;
    [SerializeField] private GameObject interactionPrompt;
    [SerializeField] private string messageMissingItems = "Ainda preciso de mais materiais...";

    private bool playerInRange = false;
    private bool isRepaired = false;

    public object CaptureState()
    {
        return isRepaired;
    }

    public void RestoreState(object state)
    {
        this.isRepaired = (bool)state;
        if (this.isRepaired)
        {
            BuildBridgeInstantly();
            gameObject.SetActive(false);
        }
    }

    private void Start()
    {
        if (interactionPrompt != null) interactionPrompt.SetActive(false);
        if (comecarVirado)
        {
            transform.localScale = new Vector3(-transform.localScale.x, transform.localScale.y, transform.localScale.z);
        }
    }

    private void Update()
    {
        if (playerInRange && !isRepaired && Input.GetKeyDown(interactionKey))
        {
            CheckAndBuildBridge();
        }
    }

    private void CheckAndBuildBridge()
    {
        if (HasAllRequiredItems())
        {
            RemoveRequiredItems();
            isRepaired = true;
            if (interactionPrompt != null) interactionPrompt.SetActive(false);
            if (GetComponent<Collider2D>() != null) GetComponent<Collider2D>().enabled = false;
            StartCoroutine(BuildBridgeCoroutine());
        }
        else
        {
            if (FloatingTextManager.instance != null)
            {
                FloatingTextManager.instance.ShowFloatingText(messageMissingItems, transform.position);
            }
        }
    }

    private bool HasAllRequiredItems()
    {
        if (PlayerItens.instance == null) return false;
        return repairCosts.All(cost => PlayerItens.instance.GetItemCount(cost.requiredItem) >= cost.quantity);
    }

    private void RemoveRequiredItems()
    {
        if (PlayerItens.instance == null) return;
        foreach (var cost in repairCosts)
        {
            PlayerItens.instance.RemoveItem(cost.requiredItem, cost.quantity);
        }
    }

    private IEnumerator BuildBridgeCoroutine()
    {
        if (completeBridgePrefab == null || bridgeSpawnPoint == null)
        {
            yield break;
        }

        GameObject bridgeInstance = Instantiate(completeBridgePrefab, bridgeSpawnPoint.position, bridgeSpawnPoint.rotation);

        List<Transform> pieces = new List<Transform>();
        foreach (Transform piece in bridgeInstance.transform)
        {
            pieces.Add(piece);
            piece.gameObject.SetActive(false);
        }

        List<Coroutine> runningAnimations = new List<Coroutine>();
        foreach (Transform piece in pieces)
        {
            piece.gameObject.SetActive(true);
            runningAnimations.Add(StartCoroutine(AnimatePiece(piece)));
            yield return new WaitForSeconds(buildDelay);
        }

        foreach (Coroutine animCoroutine in runningAnimations)
        {
            yield return animCoroutine;
        }

        if (bridgeSpawnPoint != null)
        {
            Destroy(bridgeSpawnPoint.gameObject);
        }

        if (disappearEffectPrefab != null)
        {
            Instantiate(disappearEffectPrefab, transform.position, Quaternion.identity);
        }

        if (GetComponent<SpriteRenderer>() != null) GetComponent<SpriteRenderer>().enabled = false;
        if (GetComponent<Collider2D>() != null) GetComponent<Collider2D>().enabled = false;
        this.enabled = false;
    }

    private void BuildBridgeInstantly()
    {
        if (completeBridgePrefab == null || bridgeSpawnPoint == null) return;
        Instantiate(completeBridgePrefab, bridgeSpawnPoint.position, bridgeSpawnPoint.rotation);
        if (bridgeSpawnPoint != null) Destroy(bridgeSpawnPoint.gameObject);
    }

    private IEnumerator AnimatePiece(Transform pieceTransform)
    {
        float duration = 0.3f;
        float timer = 0f;
        Vector3 originalScale = Vector3.one;
        Vector3 startScale = Vector3.zero;
        Vector3 overshootScale = originalScale * 1.2f;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float progress = timer / duration;
            if (progress < 0.7f) pieceTransform.localScale = Vector3.Lerp(startScale, overshootScale, progress / 0.7f);
            else pieceTransform.localScale = Vector3.Lerp(overshootScale, originalScale, (progress - 0.7f) / 0.3f);
            yield return null;
        }
        pieceTransform.localScale = originalScale;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") && !isRepaired)
        {
            playerInRange = true;
            if (interactionPrompt != null) interactionPrompt.SetActive(true);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = false;
            if (interactionPrompt != null) interactionPrompt.SetActive(false);
        }
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