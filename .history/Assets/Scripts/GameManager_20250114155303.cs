public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    public List<string> EraList = new List<string>();
    public string CurrentEra { get; private set; }
    public List<Sprite> eraImages = new List<Sprite>();

    // Track current word index for the active era
    private int currentWordIndex = 0;

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

    public void SetCurrentEra(string era)
    {
        if (EraList.Contains(era))
        {
            CurrentEra = era;
            currentWordIndex = 0; // Reset index when starting new era
            Debug.Log($"Starting era: {era}");
        }
    }

    public int GetCurrentWordIndex()
    {
        return currentWordIndex;
    }

    public void AdvanceToNextWord()
    {
        if (!string.IsNullOrEmpty(CurrentEra))
        {
            currentWordIndex++;
            var words = WordValidator.GetWordsForEra(CurrentEra);
            
            if (currentWordIndex >= words.Count)
            {
                HandleEraCompletion();
            }
        }
    }

    private void HandleEraCompletion()
    {
        Debug.Log($"Era {CurrentEra} completed!");
        SceneManager.LoadScene("MainMenuScene");
    }

    public float GetEraProgress(string era)
    {
        if (era != CurrentEra) return 0f;
        
        var words = WordValidator.GetWordsForEra(era);
        if (words.Count == 0) return 0f;
        
        return (float)currentWordIndex / words.Count;
    }

    public Sprite getEraImage(string era)
    {
        // Your existing getEraImage implementation
        if (era.Equals("Ancient Egypt")) return eraImages[0];
        else if (era.Equals("Medieval Europe")) return eraImages[1];
        else if (era.Equals("Ancient Rome")) return eraImages[2];
        else if (era.Equals("Renaissance")) return eraImages[3];
        else if (era.Equals("Industrial Revolution")) return eraImages[4];
        else if (era.Equals("Ancient Greece")) return eraImages[5];
        return null;
    }
}