using UnityEngine;
using TMPro;

public class FloatingText : MonoBehaviour
{
    public float moveSpeed = 1f;
    public float fadeOutTime = 1f;
    public TextMeshProUGUI textMesh;

    private float fadeTimer;
    private Color startColor;

    private void Awake()
    {
        textMesh = GetComponentInChildren<TextMeshProUGUI>();
        if (textMesh != null)
        {
            startColor = textMesh.color;
        }
        fadeTimer = fadeOutTime;
    }

    private void Update()
    {
        // Move o texto para cima
        transform.position += new Vector3(0, moveSpeed * Time.deltaTime, 0);

        // Desaparece com o tempo
        fadeTimer -= Time.deltaTime;
        if (fadeTimer <= 0)
        {
            Destroy(gameObject);
        }
        else
        {
            if (textMesh != null)
            {
                float alpha = fadeTimer / fadeOutTime;
                textMesh.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
            }
        }
    }

    public void SetText(string text)
    {
        if (textMesh != null)
        {
            textMesh.text = text;
        }
    }
}