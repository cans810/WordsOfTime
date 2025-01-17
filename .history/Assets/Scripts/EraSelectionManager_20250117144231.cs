using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class EraSelectionManager : MonoBehaviour
{
    public SpriteRenderer BackgroundImage;

    void Start()
    {
        BackgroundImage.sprite = GameManager.Instance.getEraImage(GameManager.Instance.CurrentEra);
    }

    public void SelectEra(string eraName)
    {
        GameManager.Instance.SelectEra(eraName);
        WordGameManager.Instance.StartNewGameInEra(); // Reset word game state for new era
        BackgroundImage.sprite = GameManager.Instance.getEraImage(GameManager.Instance.CurrentEra);
    }

    public void ReturnButton()
    {
        SceneManager.LoadScene("MainMenuScene");
    }
}