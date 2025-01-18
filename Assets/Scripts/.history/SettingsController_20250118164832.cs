using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SettingsController : MonoBehaviour
{

    public GameObject LanguageSelectionPanel;
    public GameObject SettingsPanel;

    // Start is called before the first frame update
    void Start()
    {
        if (LanguageSelectionPanel != null)
        {
            LanguageSelectionPanel.SetActive(false);
        }
    }

    // Update is called once per frame
    void Update()
    {
        
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

    public void OnLanguageButtonClicked()
    {
        if (LanguageSelectionPanel != null)
        {
            LanguageSelectionPanel.SetActive(true);
        }
    }

    public void OnSaveButtonClicked()
    {
        
    }

    public void OnHelpButtonClicked()
    {
        
    }

    public void OnReturnButtonClicked()
    {
        if (LanguageSelectionPanel != null)
        {
            LanguageSelectionPanel.SetActive(false);
        }
        
        if (SettingsPanel != null)
        {
            SettingsPanel.SetActive(false);
        }
    }
}
