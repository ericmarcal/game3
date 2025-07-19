using UnityEngine;
using System.Collections.Generic;
using System; // Adicionado para StringComparer
using System.Linq;

public class ItemDatabase : MonoBehaviour
{
    private static ItemDatabase _instance;
    public static ItemDatabase Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<ItemDatabase>();
                if (_instance == null)
                {
                    GameObject singletonObject = new GameObject("ItemDatabase_Runtime");
                    _instance = singletonObject.AddComponent<ItemDatabase>();
                }
            }
            return _instance;
        }
    }

    private Dictionary<string, ItemData> itemsByName;
    public bool IsReady { get; private set; } = false;

    private void Awake()
    {
        if (_instance != null && _instance != this) { Destroy(this.gameObject); return; }
        _instance = this;
        DontDestroyOnLoad(this.gameObject);
        LoadAllItems();
    }

    private void LoadAllItems()
    {
        // *** LÓGICA CORRIGIDA PARA IGNORAR MAIÚSCULAS/MINÚSCULAS ***
        itemsByName = new Dictionary<string, ItemData>(StringComparer.InvariantCultureIgnoreCase);

        ItemData[] allItems = Resources.LoadAll<ItemData>("Items");
        foreach (var item in allItems)
        {
            if (item != null && !itemsByName.ContainsKey(item.itemName))
            {
                itemsByName.Add(item.itemName, item);
            }
        }
        IsReady = true;
        Debug.Log($"<color=orange>ItemDatabase:</color> {itemsByName.Count} itens carregados. A base de dados está pronta.");
    }

    public ItemData GetItemByName(string name)
    {
        if (string.IsNullOrEmpty(name) || itemsByName == null || !itemsByName.ContainsKey(name))
        {
            return null;
        }
        return itemsByName[name];
    }
}