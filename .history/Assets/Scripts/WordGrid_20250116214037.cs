using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class WordGrid
{
    public List<char> grid = new List<char>(new char[36]); // 6x6 grid flattened into a list
}
