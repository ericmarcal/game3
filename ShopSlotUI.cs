using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

// << CORREÇÃO AQUI >>
// O enum foi movido para fora da classe para se tornar publicamente acessível.
public enum ShopSlotType { Buy, Sell }

public class ShopSlotUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Referências")]
    [SerializeField] private Image icon;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI priceText;
    [SerializeField] private Image selectionHighlight;

    public InventorySlot linkedSlot { get; private set; }
    public ShopSlotType slotType { get; private set; }
    public int originalIndex { get; private set; }
    public ContainerType originalContainer { get; private set; }

    private Button button;

    private void Awake()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(OnSlotClick);
        if (selectionHighlight != null) selectionHighlight.enabled = false;
    }

    public void Setup(InventorySlot slot, ShopSlotType type, int index, ContainerType container = ContainerType.Chest)
    {
        linkedSlot = slot;
        slotType = type;
        originalIndex = index;
        originalContainer = container;

        icon.sprite = slot.item.icon;
        nameText.text = $"{slot.item.itemName} ({slot.quantity})";

        if (type == ShopSlotType.Buy)
        {
            priceText.text = slot.item.buyPrice.ToString();
        }
        else
        {
            priceText.text = slot.item.sellPrice.ToString();
        }
    }

    public void SetSelected(bool isSelected)
    {
        if (selectionHighlight != null)
        {
            selectionHighlight.enabled = isSelected;
        }
    }

    private void OnSlotClick()
    {
        ShopUIManager.instance.SelectSlot(this);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (linkedSlot?.item != null && TooltipSystem.instance != null)
        {
            TooltipSystem.instance.Show(linkedSlot.item, TooltipSystem.TooltipPositionMode.FollowMouse);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (TooltipSystem.instance != null)
        {
            TooltipSystem.instance.Hide();
        }
    }
}