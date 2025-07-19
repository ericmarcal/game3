using UnityEngine;

[RequireComponent(typeof(ItemContainer))]
public class ShopInteraction : MonoBehaviour
{
    private ItemContainer itemContainer;

    private void Awake()
    {
        itemContainer = GetComponent<ItemContainer>();
    }

    public void ToggleShop()
    {
        if (ShopUIManager.instance == null) return;

        if (ShopUIManager.instance.IsShopOpen)
        {
            ShopUIManager.instance.CloseShop();
        }
        else
        {
            ShopUIManager.instance.OpenShop(itemContainer);
        }
    }
}