using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class EraSelectionManager : MonoBehaviour
{

    public Image BackgroundImage;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void SelectEra(string eraName){
        GameManager.Instance.EraSelected = eraName;
    }
}
