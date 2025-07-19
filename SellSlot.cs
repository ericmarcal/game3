using UnityEngine;
using UnityEngine.EventSystems;

public class SellSlot : MonoBehaviour, IDropHandler
{
    public void OnDrop(PointerEventData eventData)
    {
        InventorySlotUI sourceSlotUI = InventoryManager.instance.GetSourceSlotUI();
        if (sourceSlotUI == null) return;

        InventorySlot sourceSlotData = sourceSlotUI.GetLinkedSlotData();
        if (sourceSlotData == null || sourceSlotData.item == null) return;

        if (sourceSlotData.item.sellPrice > 0)
        {
            // Vende o stack inteiro ao arrastar para a área
            PlayerItens.instance.SellItemStack(sourceSlotUI.GetContainerType(), sourceSlotUI.GetIndex());
        }

        InventoryManager.instance.CancelDrag();
    }
}