using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class Room : MonoBehaviour
{
    private BoxCollider2D roomBounds;

    public Bounds Bounds => roomBounds.bounds;

    private void Awake()
    {
        roomBounds = GetComponent<BoxCollider2D>();
        // Este colisor n�o deve ter f�sica, � apenas para definir a �rea.
        roomBounds.isTrigger = true;
    }

    // Quando o jogador entra na �rea desta sala...
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            // ...diz � c�mara para se prender aos limites desta sala.
            if (Camera.main != null)
            {
                Camera.main.GetComponent<CameraFollow>().SetBounds(this.Bounds);
            }
        }
    }
}