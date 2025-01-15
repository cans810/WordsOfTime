using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

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
            return eraImages[era.IndexOf()]
        }
        else if (era.Equals("Medieval Europe")){
            
        }
        else if (era.Equals("Ancient Rome")){
            
        }
        else if (era.Equals("Renaisannce")){
            
        }
        else if (era.Equals("Industrial Revolution")){
            
        }
        else if (era.Equals("Ancient Greece")){
            
        }
    }
}
