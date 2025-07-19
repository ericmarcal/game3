using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class DayNightManager : MonoBehaviour
{
    public static DayNightManager instance;

    [Header("Referências")]
    [Tooltip("Arraste para cá o seu GameObject 'PostProcessingVolume' da cena.")]
    [SerializeField] private Volume postProcessVolume;

    [Header("Configurações do Ciclo")]
    [Tooltip("A duração completa de um dia em segundos.")]
    [SerializeField] private float dayDurationInSeconds = 900f; // Padrão de 15 minutos

    [Tooltip("O gradiente que define a COR do filtro ao longo do dia.")]
    [SerializeField] private Gradient colorFilterGradient;

    [Tooltip("A curva que define o BRILHO da cena ao longo do dia.")]
    [SerializeField] private AnimationCurve exposureCurve;

    // << NOVO CAMPO ADICIONADO AQUI >>
    [Header("Configuração Inicial")]
    [Range(0f, 1f)]
    [Tooltip("O ponto inicial do ciclo do dia (0 = meia-noite, 0.25 = amanhecer, 0.5 = meio-dia).")]
    [SerializeField] private float startingTimeOfDay = 0.25f;

    // Controle de tempo
    private float timeOfDay;
    private int currentDay = 0;

    // Referência para o efeito que vamos controlar
    private ColorAdjustments colorAdjustments;

    private void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        if (postProcessVolume == null || !postProcessVolume.profile.TryGet(out colorAdjustments))
        {
            Debug.LogError("DayNightManager: PostProcessingVolume ou o Override 'ColorAdjustments' não foi encontrado! Adicione um Volume com o efeito.", this);
            this.enabled = false;
            return;
        }

        // << LÓGICA ATUALIZADA AQUI >>
        // Define o tempo inicial do dia baseado na nossa nova variável
        timeOfDay = startingTimeOfDay;
    }

    private void Update()
    {
        if (colorAdjustments == null) return;

        // Avança o tempo do dia
        timeOfDay += Time.deltaTime / dayDurationInSeconds;

        if (timeOfDay >= 1f)
        {
            timeOfDay -= 1f;
            currentDay++;
            if (FarmingManager.instance != null)
            {
                FarmingManager.instance.AdvanceDay();
            }
        }

        // Atualiza os efeitos de pós-processamento
        UpdatePostProcessingEffects();
    }

    private void UpdatePostProcessingEffects()
    {
        colorAdjustments.colorFilter.value = colorFilterGradient.Evaluate(timeOfDay);
        colorAdjustments.postExposure.value = exposureCurve.Evaluate(timeOfDay);
    }
}