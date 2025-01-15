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
        // Functionality to manually select an era (if needed)
        GameManager.Instance.CurrentEra = eraName;
        BackgroundImage.sprite = GameManager.Instance.getEraImage(GameManager.Instance.CurrentEra);
        // You might also want to reset progress within the chosen era here.
    }

    public void ReturnButton()
    {
        SceneManager.LoadScene("MainMenuScene");
    }
}