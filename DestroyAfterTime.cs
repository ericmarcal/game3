using UnityEngine;

public class DestroyAfterTime : MonoBehaviour
{
    [Tooltip("O tempo em segundos ap�s o qual este objeto ser� destru�do.")]
    [SerializeField] private float lifetime = 2f;

    private void Start()
    {
        // Agenda a destrui��o do pr�prio GameObject para depois do tempo de 'lifetime'.
        Destroy(gameObject, lifetime);
    }
}