using UnityEngine;
using System.IO;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    [Header("Game Settings")]
    public string EraSelected = "Ancient Egypt"; // Example selected era
    public int CurrentWordIndex { get; private set; } = 0; // Tracks the current word index for progression
    private List<WordSet> wordSets;

    public static GameManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        LoadWordSetsFromJSON();
        SetupProgressionForEra(EraSelected);
        StartGame();
    }

    // Load word sets from the JSON file
    private void LoadWordSetsFromJSON()
    {
        string json = File.ReadAllText("path_to_your_json_file.json");
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

    // Get the current word for the progression
    public string GetCurrentWord()
    {
        WordSet selectedEraSet = wordSets.Find(set => set.era == EraSelected);
        if (selectedEraSet != null && CurrentWordIndex < selectedEraSet.words.Count)
        {
            return selectedEraSet.words[CurrentWordIndex].word;
        }
        return null;
    }

    // Get the sentences for the current word
    public List<string> GetCurrentSentences()
    {
        WordSet selectedEraSet = wordSets.Find(set => set.era == EraSelected);
        if (selectedEraSet != null && CurrentWordIndex < selectedEraSet.words.Count)
        {
            return selectedEraSet.words[CurrentWordIndex].sentences;
        }
        return null;
    }

    // Move to the next word in the selected era
    public void NextWord()
    {
        CurrentWordIndex++;
        if (CurrentWordIndex >= wordSets[0].words.Count)
        {
            Debug.Log("All words in this era completed.");
            // Optionally transition to the next era or end the game
        }
        else
        {
            Debug.Log($"Next word: {GetCurrentWord()}");
        }
    }

    // Initialize the game (could be starting the first level, or something else)
    private void StartGame()
    {
        string word = GetCurrentWord();
        List<string> sentences = GetCurrentSentences();
        if (word != null && sentences != null)
        {
            // Pass the current word and sentence to the grid manager or UI
            GridManager.Instance.InitializeGame(word, sentences[0]); // For example, use the first sentence
        }
    }
}