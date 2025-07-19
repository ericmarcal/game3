using UnityEngine;

public class TeleportTrigger : MonoBehaviour
{
    [Tooltip("A posi��o no mundo para onde o jogador ser� teleportado.")]
    public Vector3 targetPosition = new Vector3(0, 0, 0);

    void Update()
    {
        // Verifica se a tecla 'T' foi pressionada
        if (Input.GetKeyDown(KeyCode.T))
        {
            // Verifica se a inst�ncia do jogador existe antes de tentar us�-la
            if (Player.instance != null)
            {
                Debug.Log("Tecla T pressionada. A mover o jogador para " + targetPosition);

                // Chama a fun��o p�blica do script do jogador
                Player.instance.TeleportTo(targetPosition);
            }
            else
            {
                Debug.LogError("A inst�ncia do Player n�o foi encontrada!");
            }
        }
    }
}