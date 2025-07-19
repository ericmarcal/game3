using UnityEngine;
using UnityEngine.Tilemaps;

// Este script � adicionado a um "fantasma" tempor�rio para dar feedback visual ao jogador.
public class BuildingPlacementGhost : MonoBehaviour
{
    [Header("Cores de Feedback")]
    [SerializeField] private Color validPlacementColor = new Color(0, 1, 0, 0.5f); // Verde transparente
    [SerializeField] private Color invalidPlacementColor = new Color(1, 0, 0, 0.5f); // Vermelho transparente

    private TilemapRenderer[] tilemapRenderers;

    private void Awake()
    {
        // Encontra todos os renderizadores de tilemap que est�o como filhos do fantasma.
        tilemapRenderers = GetComponentsInChildren<TilemapRenderer>();
    }

    // M�todo p�blico para definir a cor de todos os tilemaps do fantasma.
    public void SetValidity(bool isValid)
    {
        Color colorToApply = isValid ? validPlacementColor : invalidPlacementColor;

        if (tilemapRenderers == null) return;

        foreach (var renderer in tilemapRenderers)
        {
            if (renderer != null)
            {
                // Para URP, temos de modificar a cor do material do renderer.
                if (renderer.material != null)
                {
                    renderer.material.color = colorToApply;
                }
            }
        }
    }
}