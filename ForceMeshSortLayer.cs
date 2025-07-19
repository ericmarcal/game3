using UnityEngine;

// Garante que este script só possa ser adicionado a objetos com um MeshRenderer.
[RequireComponent(typeof(MeshRenderer))]
[ExecuteInEditMode] // Permite que o script rode no editor para vermos o resultado imediatamente.
public class ForceMeshSortLayer : MonoBehaviour
{
    [Tooltip("O nome da Camada de Renderização (Sorting Layer) para a qual forçar este mesh.")]
    [SerializeField] private string sortingLayerName = "Default";

    [Tooltip("A ordem dentro da camada de renderização. Valores mais altos são desenhados por cima.")]
    [SerializeField] private int orderInLayer = 0;

    private MeshRenderer meshRenderer;

    private void Awake()
    {
        // Pega a referência do MeshRenderer.
        meshRenderer = GetComponent<MeshRenderer>();
        ApplySortingProperties();
    }

    // Chamado sempre que um valor é alterado no Inspector.
    private void OnValidate()
    {
        if (meshRenderer == null)
        {
            meshRenderer = GetComponent<MeshRenderer>();
        }
        ApplySortingProperties();
    }

    // Função que aplica as propriedades de renderização.
    private void ApplySortingProperties()
    {
        if (meshRenderer != null)
        {
            meshRenderer.sortingLayerName = sortingLayerName;
            meshRenderer.sortingOrder = orderInLayer;
        }
    }
}