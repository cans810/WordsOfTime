using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public List<string> EraList = new List<string>();
    public string CurrentEra { get; set; } = "";
    private int currentEraIndex = -1;

    public List<Sprite> eraImages = new List<Sprite>();

    private Dictionary<string, Dictionary<string, List<string>>> wordSetsWithSentences;
    private List<string> unsolvedWordsInCurrentEra;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        LoadWordSets();
        MoveToNextEra();
    }


    private void LoadWordSets()
    {
        string filePath = Application.dataPath + "/words.json";
        Debug.Log($"Attempting to load words from: {filePath}");

        if (System.IO.File.Exists(filePath))
        {
            try
            {
                string json = System.IO.File.ReadAllText(filePath);
                WordSetList wordSetList = JsonUtility.FromJson<WordSetList>(json);

                if (wordSetList != null && wordSetList.sets != null && wordSetList.sets.Length > 0)
                {
                    wordSetsWithSentences = new Dictionary<string, Dictionary<string, List<string>>>();

                    foreach (var wordSet in wordSetList.sets)
                    {
                        var wordDict = new Dictionary<string, List<string>>();
                        foreach (var wordEntry in wordSet.words)
                        {
                            wordDict[wordEntry.word.ToUpper()] = new List<string>(wordEntry.sentences);
                        }
                        wordSetsWithSentences[wordSet.era] = wordDict;
                    }
                }
                else
                {
                    Debug.LogError("Failed to parse JSON: WordSetList or sets array is null");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error parsing JSON file: {e.Message}\n{e.StackTrace}");
            }
        }
        else
        {
            Debug.LogError($"words.json file not found at path: {filePath}");
        }
    }


    public void MoveToNextEra()
    {
        currentEraIndex++;
        if (currentEraIndex < EraList.Count)
        {
            CurrentEra = EraList[currentEraIndex];
            unsolvedWordsInCurrentEra = new List<string>(wordSetsWithSentences[CurrentEra].Keys);
            Debug.Log($"Moved to next era: {CurrentEra}");
        }
        else
        {
            Debug.Log("All eras completed!");
            SceneManager.LoadScene("MainMenuScene"); 
        }
    }

    public string GetNextWord()
    {
        if (unsolvedWordsInCurrentEra == null || unsolvedWordsInCurrentEra.Count == 0)
        {
            Debug.Log("No more words in this era, moving to the next.");
            MoveToNextEra();
            if (CurrentEra != null && wordSetsWithSentences.ContainsKey(CurrentEra) && wordSetsWithSentences[CurrentEra].Count > 0)
            {
                 unsolvedWordsInCurrentEra = new List<string>(wordSetsWithSentences[CurrentEra].Keys);
            }
            else
            {
                Debug.LogError("Error getting next word - issue with current era or word data.");
                return null;
            }

        }

        int randomIndex = Random.Range(0, unsolvedWordsInCurrentEra.Count);
        string nextWord = unsolvedWordsInCurrentEra[randomIndex];
        unsolvedWordsInCurrentEra.RemoveAt(randomIndex);
        return nextWord;
    }

    public Sprite getEraImage(string era)
    {
        if (era.Equals("Ancient Egypt"))
        {
            return eraImages[0];
        }
        else if (era.Equals("Medieval Europe"))
        {
            return eraImages[1];
        }
        else if (era.Equals("Ancient Rome"))
        {
            return eraImages[2];
        }
        else if (era.Equals("Renaissance"))
        {
            return eraImages[3];
        }
        else if (era.Equals("Industrial Revolution"))
        {
            return eraImages[4];
        }
        else if (era.Equals("Ancient Greece"))
        {
            return eraImages[5];
        }
        return null;
    }
}