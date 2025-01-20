using UnityEngine;
using TMPro;

public class TranslatedUI : MonoBehaviour
{
    [SerializeField] private string translationKey;
    private TextMeshProUGUI textComponent;

    private void Awake()
    {
        textComponent = GetComponent<TextMeshProUGUI>();
        UpdateText();
    }

    private void OnEnable()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnLanguageChanged += UpdateText;
        }
    }

    private void OnDisable()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnLanguageChanged -= UpdateText;
        }
    }

    public void UpdateText()
    {
        if (textComponent != null && TranslationManager.Instance != null)
        {
            textComponent.text = TranslationManager.Instance.GetTranslation(
                translationKey, 
                GameManager.Instance.CurrentLanguage
            );
        }
    }
} 