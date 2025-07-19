using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(UniqueID))]
public class ItemContainer : MonoBehaviour, ISavable
{
    public string ID => GetComponent<UniqueID>().ID;

    [Header("Configuração do Container")]
    public int containerSize = 12;
    public List<InventorySlot> slots = new List<InventorySlot>();

    private void Awake()
    {
        while (slots.Count < containerSize)
        {
            slots.Add(new InventorySlot());
        }
    }

    public object CaptureState()
    {
        return slots.Select(s => s.GetSaveData()).ToList();
    }

    public void RestoreState(object state)
    {
        var slotDataList = ((Newtonsoft.Json.Linq.JArray)state).ToObject<List<InventorySlot.SlotSaveData>>();
        slots.Clear();
        foreach (var savedSlot in slotDataList)
        {
            slots.Add(new InventorySlot(savedSlot));
        }
    }

    public bool IsFull() { foreach (var slot in slots) { if (slot.item == null) return false; if (slot.item.isStackable && slot.quantity < slot.item.maxStackSize) return false; } return true; }
    public bool HasItems() => slots.Any(slot => slot.item != null);
    public bool AddItem(ItemData item, int quantity) { foreach (var slot in slots) { if (slot.item == item && slot.item.isStackable && slot.quantity < item.maxStackSize) { int canAdd = item.maxStackSize - slot.quantity; int toAdd = Mathf.Min(quantity, canAdd); slot.quantity += toAdd; quantity -= toAdd; if (quantity <= 0) return true; } } foreach (var slot in slots) { if (slot.item == null) { slot.SetItem(item, quantity); return true; } } return false; }

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