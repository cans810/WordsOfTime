public static class WordValidator
{
    private static Dictionary<string, Dictionary<string, List<string>>> wordSetsWithSentences;

    public static List<string> GetWordsForEra(string era)
    {
        if (wordSetsWithSentences == null || !wordSetsWithSentences.ContainsKey(era))
        {
            Debug.LogError($"Era '{era}' not found in available eras");
            return new List<string>();
        }

        return new List<string>(wordSetsWithSentences[era].Keys);
    }

    public static string GetCurrentWord(string era)
    {
        var words = GetWordsForEra(era);
        int currentIndex = GameManager.Instance.GetCurrentWordIndex();
        
        if (currentIndex < words.Count)
        {
            return words[currentIndex];
        }
        
        return null;
    }

    public static string GetSentenceForWord(string word, string era)
    {
        // Only get sentence if this is the current word
        string currentWord = GetCurrentWord(era);
        if (currentWord != null && currentWord.Equals(word, StringComparison.OrdinalIgnoreCase))
        {
            if (wordSetsWithSentences.ContainsKey(era) && 
                wordSetsWithSentences[era].ContainsKey(word.ToUpper()))
            {
                var sentences = wordSetsWithSentences[era][word.ToUpper()];
                return sentences[Random.Range(0, sentences.Count)];
            }
        }
        
        return "Sentence not found.";
    }

    // Update WordGameManager's HandleCorrectWord method
    public void HandleCorrectWord()
    {
        UpdateScore(correctWordPoints);
        ShowMessage("Correct!", correctWordColor);
        
        GameManager.Instance.AdvanceToNextWord();
        
        // Check if there are more words in this era
        string nextWord = WordValidator.GetCurrentWord(GameManager.Instance.EraSelected);
        if (nextWord == null)
        {
            // Era completed
            SceneManager.LoadScene("MainMenuScene");
        }
        else
        {
            // Continue with next word
            SetupGame();
        }
    }
}