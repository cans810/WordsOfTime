using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MainMenuManager : MonoBehaviour
{
    public Image BackgroundImage;

    // Start is called before the first frame update
    void Start()
    {
        BackgroundImage.sprite = GameManager.Instance.getEraImage(GameManager.Instance.EraSelected);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void PlayButton(){
        SceneManager.LoadScene("GameScene");
    }

    public void SelectEraButton(){
        SceneManager.LoadScene("EraSelectionScene");
    }
}
