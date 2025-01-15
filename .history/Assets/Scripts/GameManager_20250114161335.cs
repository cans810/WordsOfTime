using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public List<string> EraList = new List<string>();

    public string EraSelected = "";

    public List<Sprite> eraImages = new List<Sprite>();

    public int CurrentWordIndex { get; private set; } = 0; // Tracks the current word index for progression
    private List<WordSet> wordSets;

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

        int randomEra = UnityEngine.Random.Range(0, EraList.Count);
        EraSelected = EraList[randomEra];

        Debug.Log(EraSelected);
    }

    // Start is called before the first frame update
    private void Start()
    {
        LoadWordSetsFromJSON();
        SetupProgressionForEra(EraSelected);
        StartGame();
    }

    // Load word sets from the JSON file
    private void LoadWordSetsFromJSON()
    {
        string json = File.ReadAllText("words.json");
        wordSets = JsonUtility.FromJson<WordSetsWrapper>(json).sets;
    }

    // Set up the progression for the selected era
    public void SetupProgressionForEra(string selectedEra)
    {
        WordSet selectedEraSet = wordSets.Find(set => set.era == selectedEra);
        if (selectedEraSet != null)
        {
            // Reset word index to start at the beginning
            CurrentWordIndex = 0;
            Debug.Log($"Progression for era {selectedEra} started.");
        }
        else
        {
            Debug.LogError($"No words found for the selected era: {selectedEra}");
        }
    }

    // Update is called once per frame
    void Update()
    {

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
        else if (era.Equals("Renaissance")){
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
