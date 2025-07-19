using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class Room : MonoBehaviour
{
    private BoxCollider2D roomBounds;

    public Bounds Bounds => roomBounds.bounds;

    private void Awake()
    {
        roomBounds = GetComponent<BoxCollider2D>();
        // Este colisor não deve ter física, é apenas para definir a área.
        roomBounds.isTrigger = true;
    }

    // Quando o jogador entra na área desta sala...
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            // ...diz à câmara para se prender aos limites desta sala.
            if (Camera.main != null)
            {
                Camera.main.GetComponent<CameraFollow>().SetBounds(this.Bounds);
            }
        }
    }
}