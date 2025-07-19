using UnityEngine;
using TMPro;

public class FloatingTextManager : MonoBehaviour
{
    public static FloatingTextManager instance;

    [SerializeField] private GameObject floatingTextPrefab;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void ShowFloatingText(string text, Vector3 position)
    {
        if (floatingTextPrefab == null) return;

        GameObject textInstance = Instantiate(floatingTextPrefab, position, Quaternion.identity, this.transform);
        FloatingText floatingText = textInstance.GetComponent<FloatingText>();
        if (floatingText != null)
        {
            floatingText.SetText(text);
        }
    }
}