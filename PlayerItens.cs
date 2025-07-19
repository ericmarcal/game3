using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

[System.Serializable]
public struct StartingItem { public ItemData item; public int quantity; }

[System.Serializable]
public class TrackedResource { public ItemData itemData; public int currentAmount; public int limit; }

public class PlayerItens : MonoBehaviour, ISavable
{
    public string ID => "PlayerItens";
    public static PlayerItens instance;

    [Header("Dinheiro Inicial")]
    [SerializeField] private int startingMoney = 50;
    public int money { get; private set; }

    [Header("Inventário e Hotbar")]
    public int inventorySize = 16;
    public int hotbarSize = 5;
    public List<InventorySlot> inventorySlots = new List<InventorySlot>();
    public List<InventorySlot> hotbarSlots = new List<InventorySlot>();

    [Header("Itens Iniciais")]
    [SerializeField] private List<StartingItem> startingInventoryItems = new List<StartingItem>();
    [SerializeField] private List<StartingItem> startingHotbarItems = new List<StartingItem>();

    [Header("Rastreamento de Recursos (HUD)")]
    public List<TrackedResource> trackedResources = new List<TrackedResource>();

    private void Awake()
    {
        if (instance == null) instance = this;
        else if (instance != this) Destroy(gameObject);

        InitializeSlots(inventorySlots, inventorySize);
        InitializeSlots(hotbarSlots, hotbarSize);
        AddStartingItems();
    }

    [System.Serializable]
    private struct PlayerItensSaveData { public int money; public List<InventorySlot.SlotSaveData> inventorySlots; public List<InventorySlot.SlotSaveData> hotbarSlots; }

    public object CaptureState()
    {
        var saveData = new PlayerItensSaveData
        {
            money = this.money,
            inventorySlots = this.inventorySlots.Select(s => s.GetSaveData()).ToList(),
            hotbarSlots = this.hotbarSlots.Select(s => s.GetSaveData()).ToList()
        };
        return saveData;
    }

    public void RestoreState(object state)
    {
        var saveData = ((JObject)state).ToObject<PlayerItensSaveData>();

        this.money = saveData.money;

        inventorySlots.Clear();
        for (int i = 0; i < inventorySize; i++)
        {
            if (i < saveData.inventorySlots.Count)
            {
                inventorySlots.Add(new InventorySlot(saveData.inventorySlots[i]));
            }
            else
            {
                inventorySlots.Add(new InventorySlot());
            }
        }

        hotbarSlots.Clear();
        for (int i = 0; i < hotbarSize; i++)
        {
            if (i < saveData.hotbarSlots.Count)
            {
                hotbarSlots.Add(new InventorySlot(saveData.hotbarSlots[i]));
            }
            else
            {
                hotbarSlots.Add(new InventorySlot());
            }
        }

        if (InventoryManager.instance != null) InventoryManager.instance.UpdateAllVisuals();
        if (HotbarController.instance != null) HotbarController.instance.UpdateDisplay();
    }

    private void InitializeSlots(List<InventorySlot> slots, int size) { slots.Clear(); for (int i = 0; i < size; i++) { slots.Add(new InventorySlot()); } }
    private void AddStartingItems() { for (int i = 0; i < startingHotbarItems.Count && i < hotbarSize; i++) { if (startingHotbarItems[i].item != null && startingHotbarItems[i].quantity > 0) { hotbarSlots[i].SetItem(startingHotbarItems[i].item, startingHotbarItems[i].quantity); } } for (int i = 0; i < startingInventoryItems.Count && i < inventorySize; i++) { if (startingInventoryItems[i].item != null && startingInventoryItems[i].quantity > 0) { inventorySlots[i].SetItem(startingInventoryItems[i].item, startingInventoryItems[i].quantity); } } }
    public void DropItemToWorld(InventorySlot slotData) { if (slotData == null || slotData.item == null || Player.instance == null) return; GameObject worldItemPrefab = slotData.item.itemPrefab; if (worldItemPrefab != null) { Vector3 spawnPosition = Player.instance.transform.position + new Vector3(0, -0.2f, 0); GameObject droppedItemGO = Instantiate(worldItemPrefab, spawnPosition, Quaternion.identity); WorldItem worldItem = droppedItemGO.GetComponent<WorldItem>(); if (worldItem != null) { worldItem.itemData = slotData.item; worldItem.quantity = slotData.quantity; worldItem.InitializeAsInventoryDrop(Player.instance.lastMoveDirection); } } }
    public void AddMoney(int amount) { if (amount > 0) { money += amount; if (InventoryManager.instance != null) InventoryManager.instance.UpdateAllVisuals(); } }
    public bool RemoveMoney(int amount) { if (amount > 0 && money >= amount) { money -= amount; if (InventoryManager.instance != null) InventoryManager.instance.UpdateAllVisuals(); return true; } return false; }
    public void BuyItem(ItemData item, int quantity) { if (item == null || quantity <= 0) return; int totalPrice = item.buyPrice * quantity; if (RemoveMoney(totalPrice)) { AddItem(item, quantity); } }
    public void SellItem(ContainerType container, int index) { InventorySlot slotToSell = GetSlot(container, index); if (slotToSell?.item != null) { AddMoney(slotToSell.item.sellPrice); RemoveQuantityFromSlot(container, index, 1); } }
    public void SellItemStack(ContainerType container, int index) { InventorySlot slotToSell = GetSlot(container, index); if (slotToSell?.item != null) { int totalValue = slotToSell.item.sellPrice * slotToSell.quantity; AddMoney(totalValue); ClearSlot(container, index); } }
    public void SellItemStack(InventorySlot slotToSell) { if (slotToSell?.item != null) { int totalValue = slotToSell.item.sellPrice * slotToSell.quantity; AddMoney(totalValue); } }
    public int AddItem(ItemData item, int quantity) { if (item == null || quantity <= 0) return quantity; foreach (var slot in inventorySlots) { if (quantity <= 0) break; if (slot.item == item && slot.item.isStackable && slot.quantity < item.maxStackSize) { int toAdd = Mathf.Min(quantity, item.maxStackSize - slot.quantity); slot.quantity += toAdd; quantity -= toAdd; } } if (quantity > 0) { foreach (var slot in inventorySlots) { if (quantity <= 0) break; if (slot.item == null) { int toAdd = Mathf.Min(quantity, item.maxStackSize); slot.SetItem(item, toAdd); quantity -= toAdd; } } } if (InventoryManager.instance != null) InventoryManager.instance.UpdateAllVisuals(); return quantity; }
    public InventorySlot GetSlot(ContainerType type, int index) { List<InventorySlot> list; switch (type) { case ContainerType.Inventory: list = inventorySlots; break; case ContainerType.Hotbar: list = hotbarSlots; break; default: return null; } if (list == null || index < 0 || index >= list.Count) return null; return list[index]; }
    public void RemoveQuantityFromSlot(ContainerType type, int index, int quantityToRemove) { InventorySlot slot = GetSlot(type, index); if (slot?.item == null || quantityToRemove <= 0) return; slot.quantity -= quantityToRemove; if (slot.quantity <= 0) { ClearSlot(type, index); } if (InventoryManager.instance != null) InventoryManager.instance.UpdateAllVisuals(); }
    public void RemoveQuantityFromCurrentSlot(int quantity) { if (Player.instance == null) return; RemoveQuantityFromSlot(ContainerType.Hotbar, Player.instance.currentHotbarIndex, quantity); }
    public void ClearSlot(ContainerType type, int index) { InventorySlot slot = GetSlot(type, index); slot?.ClearSlot(); if (InventoryManager.instance != null) InventoryManager.instance.UpdateAllVisuals(); }
    public bool HasIngredients(List<Ingredient> ingredients) { foreach (var ingredient in ingredients) { if (GetItemCount(ingredient.item) < ingredient.quantity) { return false; } } return true; }
    public void RemoveIngredients(List<Ingredient> ingredients) { foreach (var ingredient in ingredients) { RemoveItem(ingredient.item, ingredient.quantity); } }
    public void RemoveItem(ItemData itemToRemove, int quantity) { List<InventorySlot> allSlots = hotbarSlots.Concat(inventorySlots).ToList(); for (int i = allSlots.Count - 1; i >= 0; i--) { if (quantity <= 0) break; InventorySlot slot = allSlots[i]; if (slot.item == itemToRemove) { int toRemove = Mathf.Min(quantity, slot.quantity); slot.quantity -= toRemove; quantity -= toRemove; if (slot.quantity <= 0) { slot.ClearSlot(); } } } if (InventoryManager.instance != null) InventoryManager.instance.UpdateAllVisuals(); }
    public int GetItemCount(ItemData item) { return inventorySlots.Concat(hotbarSlots).Where(s => s.item == item).Sum(s => s.quantity); }
    public int FindNextEmptyInventorySlot() { for (int i = 0; i < inventorySlots.Count; i++) { if (inventorySlots[i].item == null) { return i; } } return -1; }
    public void AddItemToSlot(ContainerType type, int index, ItemData item, int quantity) { InventorySlot slot = GetSlot(type, index); if (slot != null && slot.item == null) { slot.SetItem(item, quantity); } }
    public void ConsumeFirstAvailableHotbarItem() { for (int i = 0; i < hotbarSlots.Count; i++) { InventorySlot slot = hotbarSlots[i]; if (slot.item != null && slot.item.isConsumable) { ConsumeItem(slot); return; } } }
    public void ConsumeItemInSlot(ContainerType type, int index) { InventorySlot slot = GetSlot(type, index); if (slot != null && slot.item != null && slot.item.isConsumable) { ConsumeItem(slot); } }
    private void ConsumeItem(InventorySlot slot) { if (Player.instance == null || slot.item == null) return; Player.instance.RestoreHealth(slot.item.healthToRestore); Player.instance.RestoreStamina(slot.item.staminaToRestore); slot.quantity--; if (slot.quantity <= 0) { slot.ClearSlot(); } InventoryManager.instance.UpdateAllVisuals(); }
    public bool CanAddItem(ItemData item, int quantity) { if (item == null) return false; if (item.isStackable) { int availableSpace = inventorySlots.Where(s => s.item == item && s.quantity < item.maxStackSize).Sum(s => item.maxStackSize - s.quantity); if (availableSpace >= quantity) return true; } int emptySlots = inventorySlots.Count(s => s.item == null); if (item.isStackable) { int remainingQuantity = quantity; foreach (var slot in inventorySlots) { if (slot.item == item && slot.quantity < item.maxStackSize) { remainingQuantity -= (item.maxStackSize - slot.quantity); if (remainingQuantity <= 0) return true; } } return emptySlots * item.maxStackSize >= remainingQuantity; } else { return emptySlots >= quantity; } }
    public void TransferItem(ItemContainer sourceContainer, int sourceIndex) { if (sourceContainer == null || sourceIndex < 0 || sourceIndex >= sourceContainer.slots.Count) return; InventorySlot sourceSlot = sourceContainer.slots[sourceIndex]; if (sourceSlot.item == null) return; int remaining = AddItem(sourceSlot.item, sourceSlot.quantity); if (remaining == 0) { sourceContainer.slots[sourceIndex].ClearSlot(); } else { sourceContainer.slots[sourceIndex].quantity = remaining; } }

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