using UnityEngine;

// O enum foi simplificado, removendo os estados direcionais verticais.
public enum PlayerAnimState { Idle = 0, WalkSide = 1, Cutting = 3, Dig = 4, Watering = 5, Hammering = 12 }

public class PlayerAnim : MonoBehaviour
{
    private Player player;
    private Animator anim;
    private bool isFishingAnimationRunning;

    // Hashes dos parâmetros do Animator
    private static readonly int TransitionHash = Animator.StringToHash("Transition");
    private static readonly int IsRollTriggerHash = Animator.StringToHash("isRoll");
    private static readonly int IsCastingHash = Animator.StringToHash("isCasting");
    private static readonly int DoAttackTriggerHash = Animator.StringToHash("DoAttack");
    private static readonly int DoMineTriggerHash = Animator.StringToHash("DoMine");
    private static readonly int TakeHitTriggerHash = Animator.StringToHash("TakeHit");
    private static readonly int AttackComboStepHash = Animator.StringToHash("AttackComboStep");
    private static readonly int AnimationSpeedMultiplierHash = Animator.StringToHash("AnimationSpeedMultiplier");
    private static readonly int IsDeadTriggerHash = Animator.StringToHash("IsDead");

    // Parâmetros de direção vertical foram removidos.

    [Header("Animation Speed Settings")]
    [SerializeField] private float walkAnimSpeed = 1.0f;
    [SerializeField] private float runAnimSpeedMultiplier = 1.5f;

    void Start()
    {
        player = GetComponent<Player>();
        anim = GetComponent<Animator>();
    }

    void Update()
    {
        if (player == null || anim == null || player.IsDead()) return;

        if (player.isFishing)
        {
            if (!isFishingAnimationRunning) OnCastingStarted();
            return;
        }
        else
        {
            if (isFishingAnimationRunning) OnCastingEnded();
        }

        if (player.IsBusy()) return;

        // LÓGICA DE MOVIMENTO SIMPLIFICADA
        HandleMovement();
    }

    // Método totalmente reescrito para ser mais simples
    private void HandleMovement()
    {
        // Define a velocidade da animação (andar ou correr)
        if (player.isRunning && player.direction.sqrMagnitude > 0.01f)
        {
            anim.SetFloat(AnimationSpeedMultiplierHash, runAnimSpeedMultiplier);
        }
        else
        {
            anim.SetFloat(AnimationSpeedMultiplierHash, walkAnimSpeed);
        }

        // Se o jogador está se movendo (em qualquer direção)
        if (player.direction.sqrMagnitude > 0.01f)
        {
            // Toca a animação de andar para os lados
            anim.SetInteger(TransitionHash, (int)PlayerAnimState.WalkSide);

            // Vira o sprite para a esquerda ou direita com base na direção horizontal
            if (Mathf.Abs(player.direction.x) > 0.01f)
            {
                transform.localScale = new Vector3(Mathf.Sign(player.direction.x), 1f, 1f);
            }
        }
        else // Se o jogador está parado
        {
            // Toca a animação de Idle
            anim.SetInteger(TransitionHash, (int)PlayerAnimState.Idle);
        }
    }

    // Método simplificado: não precisa mais de direção de animação (cima/baixo)
    public void TriggerToolAnimation(ToolType tool)
    {
        if (anim == null) return;

        // Apenas vira o sprite na direção correta
        transform.localScale = new Vector3(Mathf.Sign(player.lastMoveDirection.x), 1f, 1f);

        int toolState;
        switch (tool)
        {
            case ToolType.Axe: toolState = (int)PlayerAnimState.Cutting; break;
            case ToolType.Shovel: toolState = (int)PlayerAnimState.Dig; break;
            case ToolType.WateringCan: toolState = (int)PlayerAnimState.Watering; break;
            default: toolState = (int)PlayerAnimState.Idle; break;
        }
        anim.SetInteger(TransitionHash, toolState);
    }

    // Método simplificado
    public void TriggerMineAnimation()
    {
        if (anim == null) return;
        transform.localScale = new Vector3(Mathf.Sign(player.lastMoveDirection.x), 1f, 1f);
        anim.SetTrigger(DoMineTriggerHash);
    }

    // Método simplificado
    public void TriggerAttackAnimation(int comboStep)
    {
        if (anim == null) return;
        transform.localScale = new Vector3(Mathf.Sign(player.lastMoveDirection.x), 1f, 1f);
        anim.SetInteger(AttackComboStepHash, comboStep);
        anim.SetTrigger(DoAttackTriggerHash);
    }

    // Método simplificado
    public void TriggerRollAnimation(Vector2 rollDirection)
    {
        if (anim == null) return;
        // Apenas vira o sprite na direção do rolamento
        if (Mathf.Abs(rollDirection.x) > 0.01f)
        {
            transform.localScale = new Vector3(Mathf.Sign(rollDirection.x), 1f, 1f);
        }
        anim.SetTrigger(IsRollTriggerHash);
    }

    // Métodos restantes permanecem iguais
    public void TriggerDeathAnimation() { if (anim != null) anim.SetTrigger(IsDeadTriggerHash); }
    public void ResetAttackAnimationParams() { if (anim == null) return; anim.SetInteger(AttackComboStepHash, 0); anim.ResetTrigger(DoAttackTriggerHash); }
    public void TriggerTakeHitAnimation() { if (anim != null) anim.SetTrigger(TakeHitTriggerHash); }
    public void OnCastingStarted() { if (anim != null) { isFishingAnimationRunning = true; anim.SetTrigger(IsCastingHash); } }
    public void OnCastingEnded() { isFishingAnimationRunning = false; }
}