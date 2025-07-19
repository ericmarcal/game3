using UnityEngine;

// Garante que este script s� possa ser adicionado a objetos com um MeshRenderer.
[RequireComponent(typeof(MeshRenderer))]
[ExecuteInEditMode] // Permite que o script rode no editor para vermos o resultado imediatamente.
public class ForceMeshSortLayer : MonoBehaviour
{
    [Tooltip("O nome da Camada de Renderiza��o (Sorting Layer) para a qual for�ar este mesh.")]
    [SerializeField] private string sortingLayerName = "Default";

    [Tooltip("A ordem dentro da camada de renderiza��o. Valores mais altos s�o desenhados por cima.")]
    [SerializeField] private int orderInLayer = 0;

    private MeshRenderer meshRenderer;

    private void Awake()
    {
        // Pega a refer�ncia do MeshRenderer.
        meshRenderer = GetComponent<MeshRenderer>();
        ApplySortingProperties();
    }

    // Chamado sempre que um valor � alterado no Inspector.
    private void OnValidate()
    {
        if (meshRenderer == null)
        {
            meshRenderer = GetComponent<MeshRenderer>();
        }
        ApplySortingProperties();
    }

    // Fun��o que aplica as propriedades de renderiza��o.
    private void ApplySortingProperties()
    {
        if (meshRenderer != null)
        {
            meshRenderer.sortingLayerName = sortingLayerName;
            meshRenderer.sortingOrder = orderInLayer;
        }
    }
}