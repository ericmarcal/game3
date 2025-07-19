// GameSettings.cs
using UnityEngine;

public class GameSettings : MonoBehaviour
{
    public static GameSettings instance;

    [Header("Configurações de Física para Itens")]
    [Tooltip("O 'atrito' global para todos os itens dropados. Controla o quão rápido eles param.")]
    public float itemLinearDrag = 8f;

    [Header("Drop do Inventário")]
    [Tooltip("A distância que um item é empurrado para a frente quando dropado do inventário.")]
    public float inventoryDropDistance = 1.0f;

    // *** CAMPO ADICIONADO AQUI ***
    [Tooltip("A velocidade com que o item é empurrado ao ser dropado.")]
    public float inventoryDropSpeed = 5f;


    [Header("Configurações de Camadas (Layers)")]
    [Tooltip("O nome EXATO da camada em que os itens dropados devem ficar.")]
    public string droppedItemLayerName = "Default";

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }
}