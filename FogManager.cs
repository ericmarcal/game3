using UnityEngine;
using UnityEngine.Rendering;
using System.Collections;
using UnityEngine.Rendering.Universal;

public class FogManager : MonoBehaviour
{
    public static FogManager instance;

    [Header("Referências dos Efeitos")]
    [Tooltip("Arraste para cá o seu GameObject 'Global Volume' da cena.")]
    [SerializeField] private Volume postProcessVolume;

    // << MUDANÇA PRINCIPAL: De SpriteRenderer para MeshRenderer >>
    [Tooltip("Arraste para cá o GameObject 'FogScreenQuad' que é filho da sua câmara.")]
    [SerializeField] private MeshRenderer fogMeshRenderer;

    [Header("Configurações do Fade")]
    [SerializeField] private float fadeDuration = 3.0f;
    [SerializeField] private float maxVignetteIntensity = 0.6f;
    [SerializeField] private float maxFogAlpha = 0.8f;

    private Vignette vignetteEffect;
    private Coroutine currentFadeCoroutine;
    private Material fogInstanceMaterial;
    private static readonly int GlobalAlphaProperty = Shader.PropertyToID("_GlobalAlpha");

    private void Awake()
    {
        if (instance == null) { instance = this; DontDestroyOnLoad(gameObject); }
        else { Destroy(gameObject); return; }

        if (postProcessVolume == null || !postProcessVolume.profile.TryGet(out vignetteEffect))
        {
            Debug.LogError("FogManager: O 'Global Volume' ou o override 'Vignette' não foi encontrado!", this);
        }

        if (fogMeshRenderer != null)
        {
            // A lógica para obter a instância do material é a mesma.
            fogInstanceMaterial = fogMeshRenderer.material;
        }
        else
        {
            Debug.LogError("FogManager: O 'Fog Mesh Renderer' não foi atribuído no Inspector!", this);
            this.enabled = false;
        }
    }

    private void Start()
    {
        ResetEffects();
    }

    private void OnApplicationQuit()
    {
        ResetEffects();
    }

    private void ResetEffects()
    {
        if (vignetteEffect != null) vignetteEffect.intensity.value = 0f;
        if (fogInstanceMaterial != null) fogInstanceMaterial.SetFloat(GlobalAlphaProperty, 0f);
    }

    public void StartFogEffects()
    {
        if (currentFadeCoroutine != null) StopCoroutine(currentFadeCoroutine);
        currentFadeCoroutine = StartCoroutine(FadeEffects(maxVignetteIntensity, maxFogAlpha));
    }

    public void StopFogEffects()
    {
        if (currentFadeCoroutine != null) StopCoroutine(currentFadeCoroutine);
        currentFadeCoroutine = StartCoroutine(FadeEffects(0f, 0f));
    }

    private IEnumerator FadeEffects(float targetVignette, float targetFogAlpha)
    {
        if (vignetteEffect == null || fogInstanceMaterial == null) yield break;

        float startVignette = vignetteEffect.intensity.value;
        float startFogAlpha = fogInstanceMaterial.GetFloat(GlobalAlphaProperty);
        float time = 0;

        while (time < fadeDuration)
        {
            time += Time.deltaTime;
            float progress = time / fadeDuration;

            vignetteEffect.intensity.value = Mathf.Lerp(startVignette, targetVignette, progress);
            fogInstanceMaterial.SetFloat(GlobalAlphaProperty, Mathf.Lerp(startFogAlpha, targetFogAlpha, progress));

            yield return null;
        }

        vignetteEffect.intensity.value = targetVignette;
        fogInstanceMaterial.SetFloat(GlobalAlphaProperty, targetFogAlpha);
    }
}