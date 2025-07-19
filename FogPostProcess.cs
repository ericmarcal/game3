using UnityEngine;

// Garante que o script corra no editor e que esteja sempre numa câmara.
[ExecuteInEditMode]
[RequireComponent(typeof(Camera))]
public class FogPostProcess : MonoBehaviour
{
    [Tooltip("Arraste para cá o seu material de névoa (FogScreenMaterial).")]
    public Material fogMaterial;

    // Este método especial da Unity é chamado depois de a câmara renderizar a cena.
    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        // Se o material não for nulo, aplica-o à imagem final.
        if (fogMaterial != null)
        {
            // Pega a imagem da cena (source), aplica o nosso material, e envia para o ecrã (destination).
            Graphics.Blit(source, destination, fogMaterial);
        }
        else
        {
            // Se não houver material, apenas passa a imagem original para o ecrã.
            Graphics.Blit(source, destination);
        }
    }
}