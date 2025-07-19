using UnityEngine;
using System.Collections.Generic;
using TMPro;
using System.Collections;

public class NPC_Dialogue : MonoBehaviour
{
    [Header("Configurações de Diálogo")]
    public float dialogueRange = 2f;
    public LayerMask playerLayer;
    public DialogueSettings dialogue;

    [Header("Configurações do Balão de Fala")]
    [SerializeField] private GameObject speechBubblePrefab;
    [SerializeField] private Vector3 speechBubbleOffset = new Vector3(0, 1.5f, 0);
    [SerializeField] private float typingSpeed = 0.05f;
    [SerializeField] private Vector2 bubblePadding = new Vector2(0.4f, 0.2f);
    [SerializeField] private float maxBubbleWidth = 5f;

    // << NOVA PROPRIEDADE PÚBLICA >>
    // Agora outros scripts podem ler de forma segura se o diálogo está acontecendo.
    public bool IsDialogueActive { get; private set; }

    private List<string> _sentences = new List<string>();
    private bool _playerInRange;
    private int currentSentenceIndex = 0;
    private GameObject currentSpeechBubble;
    private TextMeshPro speechTextMeshPro;
    private SpriteRenderer backgroundSpriteRenderer;
    private Coroutine typingCoroutine;
    // A referência ao antigo "npcController" foi REMOVIDA.

    private void Start()
    {
        LoadDialogue();
        IsDialogueActive = false;
        //if (dialogue == null) Debug.LogWarning($"NPC_Dialogue em {gameObject.name} não tem DialogueSettings atribuído.", this);
        //if (speechBubblePrefab == null) Debug.LogWarning($"NPC_Dialogue em {gameObject.name} não tem SpeechBubblePrefab atribuído.", this);
    }

    private void Update()
    {
        // A lógica de checagem do player e input continua a mesma
        if (_playerInRange && Input.GetKeyDown(KeyCode.E))
        {
            ShowNextBubble();
        }

        // Esconde o balão se o jogador sair do alcance
        if (!_playerInRange && IsDialogueActive)
        {
            HideBubble();
        }
    }

    private void ShowNextBubble()
    {
        // Se estiver no meio da digitação, completa a frase imediatamente
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            if (currentSentenceIndex > 0 && currentSentenceIndex <= _sentences.Count && speechTextMeshPro != null)
                speechTextMeshPro.text = _sentences[currentSentenceIndex - 1];
            if (speechTextMeshPro != null) AdjustBubbleSize();
            typingCoroutine = null;
            return;
        }

        // Se não há mais frases, esconde o balão e encerra o diálogo
        if (currentSentenceIndex >= _sentences.Count)
        {
            HideBubble();
            return;
        }

        // Inicia o diálogo
        if (!IsDialogueActive)
        {
            IsDialogueActive = true;
        }

        // Destrói o balão anterior se houver
        if (currentSpeechBubble != null)
        {
            Destroy(currentSpeechBubble);
        }

        currentSpeechBubble = Instantiate(speechBubblePrefab, transform.position + speechBubbleOffset, Quaternion.identity, transform);
        speechTextMeshPro = currentSpeechBubble.GetComponentInChildren<TextMeshPro>();
        backgroundSpriteRenderer = currentSpeechBubble.GetComponentInChildren<SpriteRenderer>();

        if (speechTextMeshPro == null || backgroundSpriteRenderer == null)
        {
            HideBubble();
            return;
        }
        RectTransform textRect = speechTextMeshPro.GetComponent<RectTransform>();
        if (textRect != null) textRect.sizeDelta = new Vector2(maxBubbleWidth, textRect.sizeDelta.y);
        if (backgroundSpriteRenderer.drawMode != SpriteDrawMode.Sliced && backgroundSpriteRenderer.drawMode != SpriteDrawMode.Tiled)
            backgroundSpriteRenderer.drawMode = SpriteDrawMode.Sliced;

        if (currentSentenceIndex < _sentences.Count)
        {
            string sentenceToDisplay = _sentences[currentSentenceIndex];
            typingCoroutine = StartCoroutine(TypeSentence(sentenceToDisplay));
        }
        currentSentenceIndex++;
    }

    private void HideBubble()
    {
        if (typingCoroutine != null) { StopCoroutine(typingCoroutine); typingCoroutine = null; }
        if (currentSpeechBubble != null) { Destroy(currentSpeechBubble); currentSpeechBubble = null; }

        // Reseta o estado do diálogo
        IsDialogueActive = false;
        currentSentenceIndex = 0;
        speechTextMeshPro = null;
        backgroundSpriteRenderer = null;
    }

    void AdjustBubbleSize()
    {
        if (speechTextMeshPro == null || backgroundSpriteRenderer == null) return;
        speechTextMeshPro.ForceMeshUpdate();
        Vector2 textSize = speechTextMeshPro.GetRenderedValues(false);
        textSize.x = Mathf.Max(textSize.x, 0.5f); textSize.y = Mathf.Max(textSize.y, 0.5f);
        backgroundSpriteRenderer.size = textSize + bubblePadding;
    }

    IEnumerator TypeSentence(string sentence)
    {
        speechTextMeshPro.text = "";
        AdjustBubbleSize();
        foreach (char letter in sentence.ToCharArray())
        {
            speechTextMeshPro.text += letter;
            AdjustBubbleSize();
            yield return new WaitForSeconds(typingSpeed);
        }
        typingCoroutine = null;
    }

    private void LoadDialogue()
    {
        _sentences.Clear();
        if (dialogue == null) return;
        if (DialogueControl.instance == null)
        {
            foreach (var sentenceInfo in dialogue.dialogues) _sentences.Add(sentenceInfo.sentence.portuguese);
            return;
        }
        foreach (var sentenceInfo in dialogue.dialogues)
        {
            switch (DialogueControl.instance.language)
            {
                case DialogueControl.Idioma.pt: _sentences.Add(sentenceInfo.sentence.portuguese); break;
                case DialogueControl.Idioma.eng: _sentences.Add(sentenceInfo.sentence.english); break;
                case DialogueControl.Idioma.spa: _sentences.Add(sentenceInfo.sentence.spanish); break;
            }
        }
    }
    private void FixedUpdate() { Collider2D hit = Physics2D.OverlapCircle(transform.position, dialogueRange, playerLayer); _playerInRange = hit != null; }
    private void OnDrawGizmosSelected() { Gizmos.color = Color.blue; Gizmos.DrawWireSphere(transform.position, dialogueRange); }
}