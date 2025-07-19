using Newtonsoft.Json;
using UnityEngine;

[System.Serializable]
public class InventorySlot
{
    public ItemData item;
    public int quantity;

    [System.Serializable]
    public struct SlotSaveData
    {
        public string itemName;
        public int quantity;
    }

    public SlotSaveData GetSaveData()
    {
        return new SlotSaveData
        {
            itemName = item?.itemName,
            quantity = this.quantity
        };
    }

    public InventorySlot(SlotSaveData saveData)
    {
        if (!string.IsNullOrEmpty(saveData.itemName))
        {
            // *** CORREÇÃO AQUI: Usando o novo "Instance" seguro ***
            this.item = ItemDatabase.Instance.GetItemByName(saveData.itemName);

            if (this.item == null)
            {
                Debug.LogError($"LOAD ERROR: Não foi possível encontrar o ItemData com o nome '{saveData.itemName}' na ItemDatabase.");
            }
        }
        this.quantity = saveData.quantity;
    }

    public InventorySlot() { ClearSlot(); }
    public InventorySlot(ItemData item, int quantity) { SetItem(item, quantity); }
    public void SetItem(ItemData newItem, int newQuantity) { item = newItem; quantity = newQuantity; }
    public void ClearSlot() { item = null; quantity = 0; }
}