using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SettingsController : MonoBehaviour
{
    public GameObject LanguageSelectionPanel;

    void Start()
    {
        // Initially hide the language panel
        if (LanguageSelectionPanel != null)
        {
            LanguageSelectionPanel.SetActive(false);
        }
    }

    public void OnLanguageButtonClicked()
    {
        if (LanguageSelectionPanel != null)
        {
            LanguageSelectionPanel.SetActive(true);
        }
    }

    public void OnReturnButtonClicked()
    {
        // Hide language panel
        if (LanguageSelectionPanel != null)
        {
            LanguageSelectionPanel.SetActive(false);
        }
        
        // Hide this settings panel (gameObject refers to the object this script is attached to)
        gameObject.SetActive(false);
    }

    public void OnMusicToggle()
    {
        
    }

    public void OnSoundToggle()
    {
        
    }

    public void OnNotificationToggle()
    {
        
    }

    public void OnSaveButtonClicked()
    {
        
    }

    public void OnHelpButtonClicked()
    {
        
    }
}
