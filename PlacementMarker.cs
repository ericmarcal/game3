using UnityEngine;

// Script simples para um marcador visual de posicionamento.
public class PlacementMarker : MonoBehaviour
{
    private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    // Método público que permite a outros scripts mudarem a cor do marcador.
    public void SetColor(Color color)
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.color = color;
        }
    }
}