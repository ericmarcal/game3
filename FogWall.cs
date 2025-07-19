using UnityEngine;
using System.Collections;

// Garante que o objeto da névoa sempre tenha os componentes necessários
[RequireComponent(typeof(Collider2D), typeof(SpriteRenderer))]
public class FogWall : MonoBehaviour
{
    [Header("Configuração da Névoa")]
    [Tooltip("A duração em segundos do efeito de fade ao entrar e sair da névoa.")]
    [SerializeField] private float fadeDuration = 2.0f;
    [Tooltip("O nível máximo de transparência (alpha) que a névoa deve atingir.")]
    [Range(0f, 1f)]
    [SerializeField] private float maxFogAlpha = 0.8f;

    [Header("Configuração do Teleporte")]
    [Tooltip("Arraste para cá um objeto vazio que marca para onde o jogador será enviado.")]
    [SerializeField] private Transform teleportTarget;
    public bool isQuestCompleted = false;

    // Referência para o Sprite Renderer que vamos controlar
    private SpriteRenderer fogSpriteRenderer;
    private Color originalColor;
    private Coroutine currentFadeCoroutine;

    private void Awake()
    {
        GetComponent<Collider2D>().isTrigger = true;
        fogSpriteRenderer = GetComponent<SpriteRenderer>();

        // Guarda a cor original, mas começa com o alpha em zero (invisível)
        originalColor = fogSpriteRenderer.color;
        fogSpriteRenderer.color = new Color(originalColor.r, originalColor.g, originalColor.b, 0f);
    }

    private void Update()
    {
        if (isQuestCompleted && gameObject.activeSelf)
        {
            // Se a quest for concluída, inicia o fade de desaparecimento permanente
            FadeOut();
            this.enabled = false; // Desativa o script para não executar mais nada
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isQuestCompleted || !other.CompareTag("Player")) return;

        // Inicia o fade para a névoa aparecer
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

        // Inicia o fade para a névoa desaparecer
        FadeOut();
    }

    // Funções públicas para iniciar os fades
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