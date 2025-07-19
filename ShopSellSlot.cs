using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public class ShopSellSlot : MonoBehaviour, IDropHandler, IPointerClickHandler
{
    [Header("Referências da UI")]
    [SerializeField] private Image icon;
    [SerializeField] private TextMeshProUGUI quantityText;

    [Header("Referências do Sistema")]
    [SerializeField] private ShopUIManager shopUIManager;

    private InventorySlot itemToSell;

    private void Awake()
    {
        itemToSell = new InventorySlot();
        ClearVisuals();
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (itemToSell.item != null) return;

        InventorySlotUI sourceSlotUI = InventoryManager.instance.GetSourceSlotUI();
        if (sourceSlotUI == null || sourceSlotUI.GetLinkedSlotData().item.sellPrice <= 0)
        {
            InventoryManager.instance.CancelDrag();
            return;
        }

        InventorySlot sourceData = sourceSlotUI.GetLinkedSlotData();
        itemToSell.SetItem(sourceData.item, sourceData.quantity);
        PlayerItens.instance.ClearSlot(sourceSlotUI.GetContainerType(), sourceSlotUI.GetIndex());
        InventoryManager.instance.StopDrag();

        UpdateVisuals();
        shopUIManager.ActivateSellUI(itemToSell);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (itemToSell.item == null) return;

        ReturnItemToPlayer();
        shopUIManager.DeactivateSellUI();
    }

    public InventorySlot GetItemToSell()
    {
        return itemToSell;
    }

    // << NOVO MÉTODO >>
    // Diminui a quantidade no slot e retorna o que sobrou
    public void ConsumeQuantity(int amount)
    {
        if (itemToSell.item == null) return;
        itemToSell.quantity -= amount;

        // Se ainda sobrarem itens, devolve para o jogador
        if (itemToSell.quantity > 0)
        {
            PlayerItens.instance.AddItem(itemToSell.item, itemToSell.quantity);
        }
    }

    public void Clear()
    {
        itemToSell.ClearSlot();
        ClearVisuals();
    }

    private void ReturnItemToPlayer()
    {
        PlayerItens.instance.AddItem(itemToSell.item, itemToSell.quantity);
        Clear();
    }

    private void UpdateVisuals()
    {
        if (itemToSell.item != null)
        {
            icon.enabled = true;
            icon.sprite = itemToSell.item.icon;
            quantityText.enabled = itemToSell.quantity > 1;
            quantityText.text = itemToSell.quantity.ToString();
        }
        else
        {
            ClearVisuals();
        }
    }

    private void ClearVisuals()
    {
        icon.enabled = false;
        icon.sprite = null;
        quantityText.enabled = false;
    }
}