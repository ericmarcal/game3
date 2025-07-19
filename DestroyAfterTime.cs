using UnityEngine;

public class DestroyAfterTime : MonoBehaviour
{
    [Tooltip("O tempo em segundos após o qual este objeto será destruído.")]
    [SerializeField] private float lifetime = 2f;

    private void Start()
    {
        // Agenda a destruição do próprio GameObject para depois do tempo de 'lifetime'.
        Destroy(gameObject, lifetime);
    }
}