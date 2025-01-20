using UnityEngine;
using TMPro;

public class LocalizedText : MonoBehaviour
{
    [SerializeField] private string translationKey;
    private TextMeshProUGUI textComponent;

    private void Awake()
    {
        textComponent = GetComponent<TextMeshProUGUI>();
    }

    private void Start()
    {
        UpdateText();
    }

    public void UpdateText()
    {
        if (textComponent != null && !string.IsNullOrEmpty(translationKey))
        {
            string translation = TranslationManager.Instance.GetTranslation(translationKey);
            
            // Special handling for hint button
            if (translationKey == "hint_button" && GameManager.Instance != null)
            {
                textComponent.text = string.Format(translation, GameManager.HINT_COST);
            }
            else
            {
                textComponent.text = translation;
            }
        }
    }
} 