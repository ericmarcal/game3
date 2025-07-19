using UnityEngine;

public class TeleportTrigger : MonoBehaviour
{
    [Tooltip("A posição no mundo para onde o jogador será teleportado.")]
    public Vector3 targetPosition = new Vector3(0, 0, 0);

    void Update()
    {
        // Verifica se a tecla 'T' foi pressionada
        if (Input.GetKeyDown(KeyCode.T))
        {
            // Verifica se a instância do jogador existe antes de tentar usá-la
            if (Player.instance != null)
            {
                Debug.Log("Tecla T pressionada. A mover o jogador para " + targetPosition);

                // Chama a função pública do script do jogador
                Player.instance.TeleportTo(targetPosition);
            }
            else
            {
                Debug.LogError("A instância do Player não foi encontrada!");
            }
        }
    }
}