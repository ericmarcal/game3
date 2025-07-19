using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;
using System.Collections;

public class ShopUIManager : MonoBehaviour
{
    public static ShopUIManager instance;

    [Header("Componentes da UI da Loja")]
    [SerializeField] private GameObject shopPanel;
    [SerializeField] private Transform buyItemsGridParent;

    [Header("Prefabs")]
    [SerializeField] private GameObject shopSlotPrefab;

    [Header("Referências de Ação (Compra)")]
    [Tooltip("O objeto que contém o fundo do slot para o item selecionado.")]
    [SerializeField] private GameObject selectedItemIconContainer;
    [Tooltip("A imagem que efetivamente mostrará o sprite do item.")]
    [SerializeField] private Image selectedItemIcon;
    [SerializeField] private TextMeshProUGUI selectedItemPriceText;
    [SerializeField] private Button actionButton;
    [SerializeField] private TextMeshProUGUI actionButtonText;
    [SerializeField] private TextMeshProUGUI playerMoneyText;

    [Header("Controle de Quantidade (Compra)")]
    [SerializeField] private Slider quantitySlider;
    [SerializeField] private TextMeshProUGUI quantityText;

    [Header("Referências de Ação (Venda)")]
    [SerializeField] private ShopSellSlot sellSlot;
    [SerializeField] private Button sellButton;
    [SerializeField] private TextMeshProUGUI sellButtonText;
    [SerializeField] private GameObject sellQuantityControlsContainer;
    [SerializeField] private Slider sellQuantitySlider;
    [SerializeField] private TextMeshProUGUI sellQuantityText;

    private ItemContainer currentNpcStock;
    private ShopSlotUI selectedSlotUI;

    public bool IsShopOpen => shopPanel != null && shopPanel.activeSelf;

    private void Awake()
    {
        if (instance == null) instance = this;
        else if (instance != this) Destroy(gameObject);
    }

    private void Start()
    {
        if (actionButton != null) actionButton.onClick.AddListener(OnActionButtonClick);
        if (sellButton != null) sellButton.onClick.AddListener(OnSellButtonClick);
        if (quantitySlider != null) quantitySlider.onValueChanged.AddListener(UpdateBuyButtonText);
        if (sellQuantitySlider != null) sellQuantitySlider.onValueChanged.AddListener(UpdateSellButtonText);
        if (shopPanel != null) shopPanel.SetActive(false);
    }

    private void Update()
    {
        if (IsShopOpen)
        {
            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Tab))
            {
                CloseShop();
            }
            if (playerMoneyText != null && PlayerItens.instance != null)
            {
                playerMoneyText.text = PlayerItens.instance.money.ToString();
            }
        }
    }

    public void OpenShop(ItemContainer npcStock)
    {
        currentNpcStock = npcStock;
        shopPanel.SetActive(true);
        if (InventoryManager.instance != null) InventoryManager.instance.ToggleInventory(true);
        if (Player.instance != null) Player.instance.isPaused = true;

        DeselectAll();
        UpdatePanels();
        DeactivateSellUI();
    }

    public void CloseShop()
    {
        if (sellSlot != null && sellSlot.GetItemToSell().item != null)
        {
            PlayerItens.instance.AddItem(sellSlot.GetItemToSell().item, sellSlot.GetItemToSell().quantity);
            sellSlot.Clear();
        }

        shopPanel.SetActive(false);
        if (TooltipSystem.instance != null) TooltipSystem.instance.Hide();
        if (InventoryManager.instance != null && InventoryManager.instance.IsInventoryOpen())
        {
            InventoryManager.instance.ToggleInventory(false);
        }
        if (Player.instance != null) Player.instance.isPaused = false;
        currentNpcStock = null;
    }

    public void UpdatePanels()
    {
        foreach (Transform child in buyItemsGridParent) Destroy(child.gameObject);
        if (currentNpcStock != null)
        {
            for (int i = 0; i < currentNpcStock.slots.Count; i++)
            {
                if (currentNpcStock.slots[i].item != null)
                {
                    GameObject slotGO = Instantiate(shopSlotPrefab, buyItemsGridParent);
                    slotGO.GetComponent<ShopSlotUI>().Setup(currentNpcStock.slots[i], ShopSlotType.Buy, i);
                }
            }
        }
    }

    public void SelectSlot(ShopSlotUI slot)
    {
        DeselectAll();
        selectedSlotUI = slot;
        slot.SetSelected(true);

        ItemData selectedItem = slot.linkedSlot.item;

        // << LÓGICA CORRIGIDA >>
        // Garante que o container (fundo) esteja ativo
        if (selectedItemIconContainer != null) selectedItemIconContainer.SetActive(true);
        // Ativa a imagem e define o sprite
        if (selectedItemIcon != null)
        {
            selectedItemIcon.enabled = true;
            selectedItemIcon.sprite = selectedItem.icon;
        }

        if (selectedItemPriceText != null) selectedItemPriceText.text = selectedItem.buyPrice.ToString();

        if (selectedItem.isStackable)
        {
            int maxAffordable = PlayerItens.instance.money / selectedItem.buyPrice;
            int maxStackSize = selectedItem.maxStackSize;
            int maxCanBuy = Mathf.Min(maxAffordable, maxStackSize);
            if (maxCanBuy > 0)
            {
                quantitySlider.interactable = true;
                quantitySlider.minValue = 1;
                quantitySlider.maxValue = maxCanBuy;
                quantitySlider.value = 1;
            }
            else
            {
                quantitySlider.interactable = false;
                quantitySlider.value = 1;
            }
        }
        else
        {
            quantitySlider.interactable = false;
            quantitySlider.value = 1;
        }
        UpdateBuyButtonText(quantitySlider.value);
    }

    private void DeselectAll()
    {
        selectedSlotUI = null;

        // << LÓGICA CORRIGIDA >>
        // Mantém o container (fundo) visível
        if (selectedItemIconContainer != null) selectedItemIconContainer.SetActive(true);
        // Apenas desabilita o componente de imagem do ícone para "limpá-lo"
        if (selectedItemIcon != null) selectedItemIcon.enabled = false;

        if (selectedItemPriceText != null) selectedItemPriceText.text = "--";
        if (TooltipSystem.instance != null) TooltipSystem.instance.Hide();
        if (actionButton != null)
        {
            actionButton.interactable = false;
            if (actionButtonText != null) actionButtonText.text = "Comprar";
        }
        if (quantitySlider != null) quantitySlider.interactable = false;
        if (quantityText != null) quantityText.text = "1x";
        if (buyItemsGridParent != null)
        {
            foreach (Transform child in buyItemsGridParent)
            {
                if (child != null) child.GetComponent<ShopSlotUI>()?.SetSelected(false);
            }
        }
    }

    private void UpdateBuyButtonText(float value)
    {
        int quantity = (int)value;
        if (quantityText != null) quantityText.text = quantity.ToString() + "x";

        if (selectedSlotUI != null)
        {
            int totalPrice = selectedSlotUI.linkedSlot.item.buyPrice * quantity;
            if (actionButtonText != null) actionButtonText.text = "Comprar";
            if (selectedItemPriceText != null) selectedItemPriceText.text = totalPrice.ToString();
            if (actionButton != null) actionButton.interactable = PlayerItens.instance.money >= totalPrice;
        }
    }

    public void OnActionButtonClick()
    {
        if (selectedSlotUI == null) return;
        int quantityToBuy = (int)quantitySlider.value;
        if (selectedSlotUI.slotType == ShopSlotType.Buy)
        {
            PlayerItens.instance.BuyItem(selectedSlotUI.linkedSlot.item, quantityToBuy);
        }
        UpdatePanels();
        DeselectAll();
    }

    public void ActivateSellUI(InventorySlot slotToSell)
    {
        if (sellButton != null) sellButton.interactable = true;
        if (slotToSell.item.isStackable && slotToSell.quantity > 1)
        {
            if (sellQuantitySlider != null)
            {
                sellQuantitySlider.interactable = true;
                sellQuantitySlider.minValue = 1;
                sellQuantitySlider.maxValue = slotToSell.quantity;
                sellQuantitySlider.value = 1;
            }
        }
        else
        {
            if (sellQuantitySlider != null)
            {
                sellQuantitySlider.interactable = false;
                sellQuantitySlider.value = 1;
            }
        }
        UpdateSellButtonText(1);
    }

    public void DeactivateSellUI()
    {
        if (sellButton != null) sellButton.interactable = false;
        if (sellButtonText != null) sellButtonText.text = "Vender";
        if (sellQuantitySlider != null) sellQuantitySlider.interactable = false;
        if (sellQuantityText != null) sellQuantityText.text = "1x";
    }

    private void UpdateSellButtonText(float value)
    {
        if (sellSlot == null || sellSlot.GetItemToSell().item == null) return;
        int quantity = (int)value;
        if (sellQuantityText != null) sellQuantityText.text = quantity.ToString() + "x";
        if (sellButtonText != null) sellButtonText.text = "Vender";
    }

    private void OnSellButtonClick()
    {
        if (sellSlot == null || sellSlot.GetItemToSell().item == null) return;
        int quantityToSell = (int)sellQuantitySlider.value;
        PlayerItens.instance.AddMoney(sellSlot.GetItemToSell().item.sellPrice * quantityToSell);
        sellSlot.ConsumeQuantity(quantityToSell);
        sellSlot.Clear();
        DeactivateSellUI();
    }
}