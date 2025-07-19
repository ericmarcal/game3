using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using TMPro; // << IMPORTANTE: Adicionar para usar TextMeshPro

public class HudController : MonoBehaviour
{
    [Header("Barras de Recursos")]
    [SerializeField] private Image woodUIBar;
    [SerializeField] private Image fishUIBar;
    [SerializeField] private Image carrotUIBar;

    [Header("Dinheiro")]
    [SerializeField] private TextMeshProUGUI moneyText; // << NOVO

    [Header("Referências de ItemData")]
    [SerializeField] private ItemData woodItemData;
    [SerializeField] private ItemData fishItemData;
    [SerializeField] private ItemData carrotItemData;

    void LateUpdate() // Mudado para LateUpdate para garantir que a UI atualize após a lógica
    {
        if (PlayerItens.instance == null) return;

        // Itera pelos recursos que o PlayerItens está rastreando
        foreach (var resource in PlayerItens.instance.trackedResources)
        {
            Image barToUpdate = null;

            if (resource.itemData == woodItemData) barToUpdate = woodUIBar;
            else if (resource.itemData == fishItemData) barToUpdate = fishUIBar;
            else if (resource.itemData == carrotItemData) barToUpdate = carrotUIBar;

            if (barToUpdate != null)
            {
                if (resource.limit > 0) // Evita divisão por zero
                {
                    barToUpdate.fillAmount = (float)resource.currentAmount / resource.limit;
                }
            }
        }

        // << NOVA LÓGICA >>
        // Atualiza o texto do dinheiro a cada frame
        if (moneyText != null)
        {
            moneyText.text = PlayerItens.instance.money.ToString();
        }
    }
}