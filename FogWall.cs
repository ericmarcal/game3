using UnityEngine;
using System.Collections;

// Garante que o objeto da n�voa sempre tenha os componentes necess�rios
[RequireComponent(typeof(Collider2D), typeof(SpriteRenderer))]
public class FogWall : MonoBehaviour
{
    [Header("Configura��o da N�voa")]
    [Tooltip("A dura��o em segundos do efeito de fade ao entrar e sair da n�voa.")]
    [SerializeField] private float fadeDuration = 2.0f;
    [Tooltip("O n�vel m�ximo de transpar�ncia (alpha) que a n�voa deve atingir.")]
    [Range(0f, 1f)]
    [SerializeField] private float maxFogAlpha = 0.8f;

    [Header("Configura��o do Teleporte")]
    [Tooltip("Arraste para c� um objeto vazio que marca para onde o jogador ser� enviado.")]
    [SerializeField] private Transform teleportTarget;
    public bool isQuestCompleted = false;

    // Refer�ncia para o Sprite Renderer que vamos controlar
    private SpriteRenderer fogSpriteRenderer;
    private Color originalColor;
    private Coroutine currentFadeCoroutine;

    private void Awake()
    {
        GetComponent<Collider2D>().isTrigger = true;
        fogSpriteRenderer = GetComponent<SpriteRenderer>();

        // Guarda a cor original, mas come�a com o alpha em zero (invis�vel)
        originalColor = fogSpriteRenderer.color;
        fogSpriteRenderer.color = new Color(originalColor.r, originalColor.g, originalColor.b, 0f);
    }

    private void Update()
    {
        if (isQuestCompleted && gameObject.activeSelf)
        {
            // Se a quest for conclu�da, inicia o fade de desaparecimento permanente
            FadeOut();
            this.enabled = false; // Desativa o script para n�o executar mais nada
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isQuestCompleted || !other.CompareTag("Player")) return;

        // Inicia o fade para a n�voa aparecer
        FadeIn();

        // Teleporta o jogador
        Player player = other.GetComponent<Player>();
        if (player != null && teleportTarget != null)
        {
            player.TeleportTo(teleportTarget.position);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (isQuestCompleted || !other.CompareTag("Player")) return;

        // Inicia o fade para a n�voa desaparecer
        FadeOut();
    }

    // Fun��es p�blicas para iniciar os fades
    public void FadeIn()
    {
        if (currentFadeCoroutine != null) StopCoroutine(currentFadeCoroutine);
        currentFadeCoroutine = StartCoroutine(FadeRoutine(maxFogAlpha));
    }

    public void FadeOut()
    {
        if (currentFadeCoroutine != null) StopCoroutine(currentFadeCoroutine);
        currentFadeCoroutine = StartCoroutine(FadeRoutine(0f));
    }

    private IEnumerator FadeRoutine(float targetAlpha)
    {
        float startAlpha = fogSpriteRenderer.color.a;
        float time = 0;

        while (time < fadeDuration)
        {
            time += Time.deltaTime;
            float newAlpha = Mathf.Lerp(startAlpha, targetAlpha, time / fadeDuration);
            fogSpriteRenderer.color = new Color(originalColor.r, originalColor.g, originalColor.b, newAlpha);
            yield return null;
        }

        fogSpriteRenderer.color = new Color(originalColor.r, originalColor.g, originalColor.b, targetAlpha);
    }
}