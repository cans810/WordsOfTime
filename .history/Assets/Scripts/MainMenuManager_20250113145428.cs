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
        BackgroundImage = GameManager.Instance.er;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
