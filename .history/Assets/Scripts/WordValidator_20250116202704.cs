// WordValidator.cs
using System.Collections.Generic;

public static class WordValidator
{
    public static string GetSentenceForWord(string word, string era)
    {
        return GameManager.Instance.GetSentenceForWord(word, era);
    }

    public static List<string> GetWordsForEra(string era)
    {
        return GameManager.Instance.GetWordsForEra(era);
    }

    public static bool IsValidWord(string word, string era)
    {
        return GameManager.Instance.IsValidWord(word, era);
    }
}