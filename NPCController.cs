using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(ItemContainer))]
public class NPCController : MonoBehaviour
{
    private enum NPCState { Idling, Patrolling, MovingToWork, Working, ReturningHome, WaitingForPickup, InteractingWithPlayer, Talking }
    [SerializeField] private NPCState currentState = NPCState.Patrolling;

    [Header("Configurações de Patrulha")]
    public float patrolSpeed = 1.5f;
    public List<Transform> patrolPoints = new List<Transform>();
    public Vector2 patrolWaitTimeRange = new Vector2(2f, 5f);

    [Header("Configuração da Casa")]
    [SerializeField] private Transform homePoint;

    [Header("Referências")]
    [SerializeField] private Transform actionIconSpawnPoint;

    private NavMeshAgent agent;
    private Animator anim;
    private NPC_Dialogue dialogueScript;
    private INPCBehavior npcBehavior;
    private ItemContainer itemContainer;

    private int patrolIndex = 0;
    private float waitTimer = 0f;
    private Transform workTarget;
    private GameObject currentActionIcon;
    private static readonly int DoWorkTrigger = Animator.StringToHash("doWork");

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        anim = GetComponent<Animator>();
        dialogueScript = GetComponent<NPC_Dialogue>();
        npcBehavior = GetComponent<INPCBehavior>();
        itemContainer = GetComponent<ItemContainer>();
    }

    private void Start()
    {
        if (homePoint == null)
        {
            homePoint = new GameObject($"{this.name}_HomePoint").transform;
            homePoint.position = this.transform.position;
        }

        agent.speed = patrolSpeed;
        agent.updateRotation = false;
        agent.updateUpAxis = false;
    }

    private void Update()
    {
        if (currentState == NPCState.InteractingWithPlayer)
        {
            agent.isStopped = true;
            return;
        }

        if (dialogueScript != null && dialogueScript.IsDialogueActive)
        {
            if (currentState != NPCState.Talking)
            {
                currentState = NPCState.Talking;
                agent.isStopped = true;
                if (currentActionIcon != null) Destroy(currentActionIcon);
            }
            return;
        }
        else if (currentState == NPCState.Talking)
        {
            currentState = NPCState.Idling;
        }

        switch (currentState)
        {
            case NPCState.Idling: HandleIdlingState(); break;
            case NPCState.Patrolling: HandlePatrollingState(); break;
            case NPCState.MovingToWork: HandleMovingToWorkState(); break;
            case NPCState.ReturningHome: HandleReturningHomeState(); break;
            case NPCState.WaitingForPickup: HandleWaitingForPickupState(); break;
            case NPCState.Working: break;
        }

        UpdateAnimationAndRotation();
    }

    public void PauseAIForInteraction()
    {
        currentState = NPCState.InteractingWithPlayer;
        agent.isStopped = true;
    }

    public void ResumeAI()
    {
        if (currentState == NPCState.InteractingWithPlayer)
        {
            currentState = NPCState.Idling;
            agent.isStopped = false;
        }
    }

    private void HandleIdlingState()
    {
        agent.isStopped = true;
        waitTimer -= Time.deltaTime;

        if (waitTimer <= 0)
        {
            if (itemContainer != null && itemContainer.IsFull())
            {
                currentState = NPCState.ReturningHome;
            }
            else if (npcBehavior != null && npcBehavior.FindWorkTarget(transform.position, out workTarget))
            {
                currentState = NPCState.MovingToWork;
            }
            else if (patrolPoints.Count > 0)
            {
                currentState = NPCState.Patrolling;
                agent.SetDestination(patrolPoints[patrolIndex].position);
            }
        }
    }

    private void HandlePatrollingState()
    {
        if (patrolPoints.Count == 0)
        {
            currentState = NPCState.Idling;
            return;
        }
        agent.isStopped = false;
        agent.stoppingDistance = 0.1f;

        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
        {
            currentState = NPCState.Idling;
            waitTimer = Random.Range(patrolWaitTimeRange.x, patrolWaitTimeRange.y);
            patrolIndex = (patrolIndex + 1) % patrolPoints.Count;
        }
    }

    private void HandleReturningHomeState()
    {
        if (homePoint == null)
        {
            currentState = NPCState.Idling; return;
        }
        agent.isStopped = false;
        agent.stoppingDistance = 0.1f;
        agent.SetDestination(homePoint.position);

        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
        {
            currentState = NPCState.WaitingForPickup;
        }
    }

    private void HandleWaitingForPickupState()
    {
        agent.isStopped = true;
        // A interação agora é iniciada pelo Player.cs
    }

    private void HandleMovingToWorkState()
    {
        if (workTarget == null)
        {
            currentState = NPCState.Idling; return;
        }
        agent.isStopped = false;
        agent.stoppingDistance = npcBehavior.GetWorkStoppingDistance();
        agent.SetDestination(workTarget.position);
        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
        {
            currentState = NPCState.Working;
            StartCoroutine(PerformWorkCoroutine());
        }
    }

    private IEnumerator PerformWorkCoroutine()
    {
        if (npcBehavior == null)
        {
            currentState = NPCState.Idling;
            yield break;
        }

        agent.isStopped = true;
        if (workTarget != null) FaceTarget(workTarget.position);
        anim.SetTrigger(DoWorkTrigger);

        GameObject iconPrefab = npcBehavior.GetWorkIconPrefab();
        if (iconPrefab != null && actionIconSpawnPoint != null)
        {
            currentActionIcon = Instantiate(iconPrefab, actionIconSpawnPoint.position, actionIconSpawnPoint.rotation, actionIconSpawnPoint);
        }

        yield return new WaitForSeconds(npcBehavior.GetWorkDuration());

        if (workTarget != null)
        {
            npcBehavior.OnWorkCompleted(workTarget, this.gameObject);
        }

        if (currentActionIcon != null) Destroy(currentActionIcon);
        workTarget = null;
        currentState = NPCState.Idling;
        waitTimer = Random.Range(patrolWaitTimeRange.x, patrolWaitTimeRange.y);
    }

    private void UpdateAnimationAndRotation()
    {
        if (currentState == NPCState.InteractingWithPlayer || currentState == NPCState.Talking)
        {
            anim.SetBool("isWalking", false);
            return;
        }

        if (agent == null) return;
        bool isMoving = !agent.isStopped && agent.velocity.sqrMagnitude > 0.01f;
        anim.SetBool("isWalking", isMoving);

        // << LÓGICA DE VIRAR CORRIGIDA >>
        if (isMoving)
        {
            Vector3 currentScale = transform.localScale;
            if (agent.velocity.x > 0.1f)
            {
                currentScale.x = -1 * Mathf.Abs(currentScale.x); // Vira para a direita
            }
            else if (agent.velocity.x < -0.1f)
            {
                currentScale.x = 1 * Mathf.Abs(currentScale.x); // Vira para a esquerda
            }
            transform.localScale = currentScale;
        }
    }

    private void FaceTarget(Vector3 targetPosition)
    {
        // << LÓGICA DE VIRAR CORRIGIDA >>
        Vector3 currentScale = transform.localScale;
        if (transform.position.x > targetPosition.x)
        {
            currentScale.x = 1 * Mathf.Abs(currentScale.x); // Vira para a esquerda
        }
        else if (transform.position.x < targetPosition.x)
        {
            currentScale.x = -1 * Mathf.Abs(currentScale.x); // Vira para a direita
        }
        transform.localScale = currentScale;
    }
}