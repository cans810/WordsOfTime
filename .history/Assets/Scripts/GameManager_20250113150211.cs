using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public List<string> EraList = new List<string>();

    public string EraSelected = "Ancient Egypt";

    public List<Sprite> eraImages = new List<Sprite>();

    // Awake is called when the script instance is being loaded
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject); 
            return;
        }

        Instance = this;

        DontDestroyOnLoad(gameObject);

        int randomEra = UnityEngine.Random.Range(0, 0);
        EraSelected = EraList[randomEra];
    }

    // Start is called before the first frame update
    void Start()
    {

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

    public Sprite getEraImage(string era){
        if (era.Equals("Ancient Egypt")){
            return eraImages[0];
        }
        else if (era.Equals("Medieval Europe")){
            return eraImages[1];
        }
        else if (era.Equals("Ancient Rome")){
            return eraImages[2];
        }
        else if (era.Equals("Renaisannce")){
            return eraImages[3];
        }
        else if (era.Equals("Industrial Revolution")){
            return eraImages[4];
        }
        else if (era.Equals("Ancient Greece")){
            return eraImages[5];
        }
        return null;
    }
}
