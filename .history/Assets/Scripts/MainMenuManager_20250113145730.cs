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
        BackgroundImage = GameManager.Instance.getEraImage(GameManager.Instance.EraSelected);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
