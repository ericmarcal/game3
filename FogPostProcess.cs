using UnityEngine;

// Garante que o script corra no editor e que esteja sempre numa c�mara.
[ExecuteInEditMode]
[RequireComponent(typeof(Camera))]
public class FogPostProcess : MonoBehaviour
{
    [Tooltip("Arraste para c� o seu material de n�voa (FogScreenMaterial).")]
    public Material fogMaterial;

    // Este m�todo especial da Unity � chamado depois de a c�mara renderizar a cena.
    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        // Se o material n�o for nulo, aplica-o � imagem final.
        if (fogMaterial != null)
        {
            // Pega a imagem da cena (source), aplica o nosso material, e envia para o ecr� (destination).
            Graphics.Blit(source, destination, fogMaterial);
        }
        else
        {
            // Se n�o houver material, apenas passa a imagem original para o ecr�.
            Graphics.Blit(source, destination);
        }
    }
}