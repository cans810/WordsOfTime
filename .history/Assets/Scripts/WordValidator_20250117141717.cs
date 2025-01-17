public static class WordValidator
{
    private static Dictionary<string, Dictionary<string, List<string>>> wordSetsWithSentences;

    static WordValidator()
    {
        LoadWordSets();
    }

    private static void LoadWordSets()
    {
        string filePath = Application.dataPath + "/words.json";
        if (!System.IO.File.Exists(filePath)) return;

        try
        {
            string json = System.IO.File.ReadAllText(filePath);
            WordSetList wordSetList = JsonUtility.FromJson<WordSetList>(json);

            if (wordSetList?.sets != null)
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
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error parsing JSON file: {e.Message}");
        }
    }

    public static string GetSentenceForWord(string word, string era)
    {
        if (wordSetsWithSentences == null || !wordSetsWithSentences.ContainsKey(era)) 
            return null;

        string upperWord = word.ToUpper();
        if (!wordSetsWithSentences[era].ContainsKey(upperWord)) 
            return null;

        var sentences = wordSetsWithSentences[era][upperWord];
        return sentences.Count > 0 ? sentences[Random.Range(0, sentences.Count)] : null;
    }

    public static List<string> GetWordsForEra(string era)
    {
        if (wordSetsWithSentences == null || !wordSetsWithSentences.ContainsKey(era))
            return new List<string>();

        return new List<string>(wordSetsWithSentences[era].Keys);
    }

    public static bool IsValidWord(string word, string era)
    {
        return wordSetsWithSentences != null && 
               wordSetsWithSentences.ContainsKey(era) &&
               wordSetsWithSentences[era].ContainsKey(word.ToUpper());
    }
}