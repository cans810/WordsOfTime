using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrianglePickerController : MonoBehaviour
{
    public BoxCollider2D triggerCollider;
    private Prize currentPrize;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    // Add method to get current prize
    public Prize GetCurrentPrize()
    {
        return currentPrize;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        Prize prize = other.GetComponent<Prize>();
        if (prize != null)
        {
            currentPrize = prize;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        Prize prize = other.GetComponent<Prize>();
        if (prize != null && prize == currentPrize)
        {
            currentPrize = null;
        }
    }
}
