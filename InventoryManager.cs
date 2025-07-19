using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager instance;

    [Header("Componentes da UI")]
    [SerializeField] private GameObject inventoryPanel;
    [SerializeField] private CanvasGroup inventoryCanvasGroup;
    [SerializeField] private Transform gridParent;
    [SerializeField] private GameObject slotPrefab;
    [SerializeField] private GridLayoutGroup gridLayoutGroup;

    [Header("Drag and Drop Visuals")]
    [SerializeField] private Image draggedItemIcon;
    [SerializeField] private TextMeshProUGUI draggedItemQuantityText;

    private bool isDragging = false;
    private InventorySlotUI sourceSlotUI;
    private Transform originalIconParent;
    private List<InventorySlotUI> uiSlots = new List<InventorySlotUI>();

    public bool IsDragging() => isDragging;
    public bool IsInventoryOpen() => inventoryPanel != null && inventoryPanel.activeSelf;
    public InventorySlotUI GetSourceSlotUI() => sourceSlotUI;

    private void Awake()
    {
        if (instance == null) instance = this; else Destroy(gameObject);
        if (inventoryCanvasGroup == null && inventoryPanel != null) inventoryCanvasGroup = inventoryPanel.GetComponent<CanvasGroup>();
        if (draggedItemIcon != null)
        {
            originalIconParent = draggedItemIcon.transform.parent;
            draggedItemIcon.raycastTarget = false;
            draggedItemIcon.gameObject.SetActive(false);
        }
        if (inventoryPanel != null) inventoryPanel.SetActive(false);
    }

    private void Start()
    {
        InitializeSlots();
    }

    private void Update()
    {
        if (isDragging) UpdateDragPosition(Input.mousePosition);
    }

    public void ToggleInventory(bool open)
    {
        if (!open && TooltipSystem.instance != null)
        {
            TooltipSystem.instance.Hide();
        }

        if (!open && isDragging) CancelDrag();

        Player playerRef = FindObjectOfType<Player>();
        if (playerRef != null)
        {
            bool isChestOpen = (ChestUIManager.instance != null && ChestUIManager.instance.currentContainer != null);
            if (!isChestOpen)
            {
                playerRef.isPaused = open;
            }
        }

        if (inventoryPanel != null) inventoryPanel.SetActive(open);

        if (inventoryCanvasGroup != null)
        {
            inventoryCanvasGroup.alpha = open ? 1f : 0f;
            inventoryCanvasGroup.interactable = open;
            inventoryCanvasGroup.blocksRaycasts = open;
        }

        if (!open && StackSplitterUI.instance != null) StackSplitterUI.instance.OnCancelClick();
        if (open) UpdateDisplay();
    }

    private void InitializeSlots()
    {
        if (PlayerItens.instance == null || slotPrefab == null || gridParent == null) return;
        for (int i = 0; i < PlayerItens.instance.inventorySize; i++)
        {
            GameObject slotGO = Instantiate(slotPrefab, gridParent);
            var slotUI = slotGO.GetComponent<InventorySlotUI>();
            slotUI.Link(i, ContainerType.Inventory);
            uiSlots.Add(slotUI);
        }
    }

    public void UpdateDisplay()
    {
        if (uiSlots == null) return;
        for (int i = 0; i < uiSlots.Count; i++)
        {
            if (i < PlayerItens.instance.inventorySlots.Count)
            {
                uiSlots[i].UpdateVisuals();
            }
        }
    }

    public void HandleSlotRightClick(InventorySlotUI clickedSlot)
    {
        if (isDragging) return;
        InventorySlot slotData = clickedSlot.GetLinkedSlotData();
        if (slotData?.item == null || slotData.quantity <= 1 || !slotData.item.isStackable) return;
        if (StackSplitterUI.instance == null) return;
        StackSplitterUI.instance.Show(slotData.item, slotData.quantity, (amountToSplit) => { ConfirmSplit(clickedSlot, amountToSplit); });
    }

    private void ConfirmSplit(InventorySlotUI fromSlot, int amountToSplit)
    {
        int emptySlotIndex = PlayerItens.instance.FindNextEmptyInventorySlot();
        if (emptySlotIndex == -1) return;
        ItemData itemToMove = fromSlot.GetLinkedSlotData().item;
        PlayerItens.instance.RemoveQuantityFromSlot(fromSlot.GetContainerType(), fromSlot.GetIndex(), amountToSplit);
        PlayerItens.instance.AddItemToSlot(ContainerType.Inventory, emptySlotIndex, itemToMove, amountToSplit);
        UpdateAllVisuals();
    }

    public void StartDrag(InventorySlotUI fromSlot)
    {
        if (fromSlot.GetLinkedSlotData()?.item == null || isDragging) return;
        sourceSlotUI = fromSlot;
        isDragging = true;
        draggedItemIcon.transform.SetParent(transform.root, true);
        draggedItemIcon.transform.SetAsLastSibling();
        draggedItemIcon.gameObject.SetActive(true);
        UpdateDraggedVisuals(sourceSlotUI.GetLinkedSlotData());
    }

    public void HandleDrop(InventorySlotUI destinationSlotUI)
    {
        if (sourceSlotUI == null || destinationSlotUI == null) { CancelDrag(); return; }

        if (destinationSlotUI.GetComponent<TrashSlot>() != null)
        {
            StopDrag();
            return;
        }

        InventorySlot sourceData = sourceSlotUI.GetLinkedSlotData();
        InventorySlot destinationData = destinationSlotUI.GetLinkedSlotData();
        if (sourceData == null || destinationData == null) { CancelDrag(); return; }

        SwapOrStack(sourceData, destinationData);

        StopDrag();
    }

    private void SwapOrStack(InventorySlot source, InventorySlot destination)
    {
        bool sourceIsEmpty = source.item == null;
        bool destinationIsEmpty = destination.item == null;

        if (!sourceIsEmpty && !destinationIsEmpty && source.item == destination.item && source.item.isStackable)
        {
            int spaceInDestination = destination.item.maxStackSize - destination.quantity;
            if (spaceInDestination > 0)
            {
                int amountToMove = Mathf.Min(source.quantity, spaceInDestination);
                destination.quantity += amountToMove;
                source.quantity -= amountToMove;
                if (source.quantity <= 0) source.ClearSlot();
            }
            else
            {
                SwapItems(source, destination);
            }
        }
        else
        {
            SwapItems(source, destination);
        }
    }

    private void SwapItems(InventorySlot source, InventorySlot destination)
    {
        ItemData tempItem = destination.item;
        int tempQuantity = destination.quantity;
        destination.SetItem(source.item, source.quantity);
        source.SetItem(tempItem, tempQuantity);
    }

    public void HandleWorldDrop()
    {
        if (sourceSlotUI != null)
        {
            if (sourceSlotUI.GetContainerType() == ContainerType.Inventory || sourceSlotUI.GetContainerType() == ContainerType.Hotbar)
            {
                InventorySlot sourceData = sourceSlotUI.GetLinkedSlotData();
                PlayerItens.instance.DropItemToWorld(sourceData);
                PlayerItens.instance.ClearSlot(sourceSlotUI.GetContainerType(), sourceSlotUI.GetIndex());
            }
        }
        StopDrag();
    }

    public void CancelDrag()
    {
        if (isDragging) StopDrag();
    }

    public void StopDrag()
    {
        isDragging = false;
        if (sourceSlotUI != null)
        {
            sourceSlotUI.GetComponent<CanvasGroup>().alpha = 1f;
            sourceSlotUI.GetComponent<CanvasGroup>().blocksRaycasts = true;
        }
        sourceSlotUI = null;
        if (draggedItemIcon != null)
        {
            draggedItemIcon.transform.SetParent(originalIconParent, true);
            draggedItemIcon.gameObject.SetActive(false);
        }
        UpdateAllVisuals();
    }

    private void UpdateDraggedVisuals(InventorySlot data) { if (draggedItemIcon == null || data?.item == null) return; draggedItemIcon.sprite = data.item.icon; draggedItemIcon.color = Color.white; UpdateDragPosition(Input.mousePosition); if (draggedItemQuantityText != null) { if (data.quantity > 1) { draggedItemQuantityText.text = data.quantity.ToString(); draggedItemQuantityText.enabled = true; } else { draggedItemQuantityText.enabled = false; } } }
    private void UpdateDragPosition(Vector2 screenPosition) { if (draggedItemIcon == null || !isDragging) return; draggedItemIcon.transform.position = screenPosition; }

    // << MÉTODO RESTAURADO >>
    public void UpdateAllVisuals()
    {
        if (InventoryManager.instance != null) InventoryManager.instance.UpdateDisplay();
        if (HotbarController.instance != null) HotbarController.instance.UpdateDisplay();
        if (ChestUIManager.instance != null && ChestUIManager.instance.currentContainer != null)
        {
            ChestUIManager.instance.UpdateDisplay();
        }
    }
}