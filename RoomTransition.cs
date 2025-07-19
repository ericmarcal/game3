using UnityEngine;
using System.Collections;

[RequireComponent(typeof(BoxCollider2D))]
public class RoomTransition : MonoBehaviour
{
    // << ENUM ADICIONADO AQUI >>
    // Esta é a definição que estava em falta e a causar todos os erros.
    public enum Direction { North, South, East, West, Invalid }

    [Header("Configuração da Transição (Automática)")]
    // Estes campos agora serão preenchidos pela nossa ferramenta.
    [SerializeField] private Direction transitionDirection;
    [SerializeField] private Room targetRoom;
    [SerializeField] private Transform cameraTargetPoint;
    [SerializeField] private Transform playerSpawnPoint;
    [SerializeField] private float panDuration = 0.5f;

    private bool isTransitioning = false;

    // Métodos públicos para a ferramenta poder aceder e modificar os campos.
    public Transform GetCameraTargetPoint() => cameraTargetPoint;
    public Transform GetPlayerSpawnPoint() => playerSpawnPoint;
    public void LinkTo(Room target, Transform camTarget, Transform spawnPoint)
    {
        targetRoom = target;
        cameraTargetPoint = camTarget;
        playerSpawnPoint = spawnPoint;
    }

    private void Awake()
    {
        GetComponent<BoxCollider2D>().isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") && !isTransitioning)
        {
            if (targetRoom == null || cameraTargetPoint == null || playerSpawnPoint == null)
            {
                Debug.LogError($"Transição '{name}' em '{transform.parent.name}' não está ligada! Use a ferramenta 'Room Tool' para ligar as salas.", this);
                return;
            }
            StartCoroutine(TransitionRoutine());
        }
    }

    private IEnumerator TransitionRoutine()
    {
        isTransitioning = true;

        yield return Player.instance.PerformRoomTransition(
            playerSpawnPoint.position,
            targetRoom.Bounds,
            cameraTargetPoint.position,
            panDuration
        );

        isTransitioning = false;
    }
}