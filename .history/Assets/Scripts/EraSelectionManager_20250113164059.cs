using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class EraSelectionManager : MonoBehaviour
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

    public void SelectEra(string eraName){
        GameManager.Instance.EraSelected = eraName;
    }

    public void ReturnButton(){
        SceneManager.
    }
}
