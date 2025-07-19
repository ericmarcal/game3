using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Linq; // Adicionado para usar .Count() com condição

public class ChestUIManager : MonoBehaviour
{
    public static ChestUIManager instance;

    [Header("Componentes da UI")]
    [SerializeField] private GameObject chestPanel;
    [SerializeField] private Transform gridParent;
    [SerializeField] private GameObject slotPrefab;
    [SerializeField] private GridLayoutGroup gridLayoutGroup;

    [Header("Configuração de Layout")]
    [Tooltip("O número máximo de colunas que o painel pode ter.")]
    [SerializeField] private int maxColumns = 5;

    public ItemContainer currentContainer { get; private set; }

    private void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);
        if (chestPanel != null) chestPanel.SetActive(false);
    }

    public void OpenChestUI(ItemContainer containerToOpen)
    {
        currentContainer = containerToOpen;
        if (chestPanel != null) chestPanel.SetActive(true);
        if (InventoryManager.instance != null) InventoryManager.instance.ToggleInventory(true);

        if (currentContainer.TryGetComponent<NPCController>(out var npcController))
        {
            npcController.PauseAIForInteraction();
        }

        UpdateDisplay();
    }

    public void CloseChestUI()
    {
        if (currentContainer != null)
        {
            if (currentContainer.TryGetComponent<NPCController>(out var npcController))
            {
                npcController.ResumeAI();
            }
        }

        currentContainer = null;
        if (chestPanel != null) chestPanel.SetActive(false);
        if (InventoryManager.instance != null && InventoryManager.instance.IsInventoryOpen())
        {
            InventoryManager.instance.ToggleInventory(false);
        }
    }

    public void UpdateDisplay()
    {
        if (currentContainer == null || gridParent == null || slotPrefab == null) return;

        foreach (Transform child in gridParent)
        {
            Destroy(child.gameObject);
        }

        if (currentContainer.slots == null) currentContainer.slots = new List<InventorySlot>();

        for (int i = 0; i < currentContainer.slots.Count; i++)
        {
            GameObject slotGO = Instantiate(slotPrefab, gridParent);
            var slotUI = slotGO.GetComponent<InventorySlotUI>();
            if (slotUI != null) slotUI.Link(i, ContainerType.Chest);
        }

        StartCoroutine(ResizePanelCoroutine());
    }

    private IEnumerator ResizePanelCoroutine()
    {
        yield return new WaitForEndOfFrame();

        if (gridLayoutGroup == null || currentContainer == null) yield break;

        RectTransform panelRect = chestPanel.GetComponent<RectTransform>();
        if (panelRect == null) yield break;

        int totalSlots = currentContainer.slots.Count;

        if (totalSlots == 0)
        {
            float minWidth = (gridLayoutGroup.padding.left + gridLayoutGroup.padding.right + gridLayoutGroup.cellSize.x);
            float minHeight = (gridLayoutGroup.padding.top + gridLayoutGroup.padding.bottom + gridLayoutGroup.cellSize.y);
            panelRect.sizeDelta = new Vector2(minWidth, minHeight);
            yield break;
        }

        float padHorizontal = gridLayoutGroup.padding.left + gridLayoutGroup.padding.right;
        float padVertical = gridLayoutGroup.padding.top + gridLayoutGroup.padding.bottom;
        float spacingX = gridLayoutGroup.spacing.x;
        float spacingY = gridLayoutGroup.spacing.y;
        float cellWidth = gridLayoutGroup.cellSize.x;
        float cellHeight = gridLayoutGroup.cellSize.y;

        int columnCount = Mathf.Clamp(totalSlots, 1, maxColumns);
        gridLayoutGroup.constraintCount = columnCount;

        // << CORREÇÃO AQUI >>
        // Usando .Count em vez de .Count()
        int rowCount = Mathf.CeilToInt((float)currentContainer.slots.Count / columnCount);

        float totalWidth = padHorizontal + (columnCount * cellWidth) + ((columnCount - 1) * spacingX);
        float totalHeight = padVertical + (rowCount * cellHeight) + ((rowCount - 1) * spacingY);

        panelRect.sizeDelta = new Vector2(totalWidth, totalHeight);
    }
}