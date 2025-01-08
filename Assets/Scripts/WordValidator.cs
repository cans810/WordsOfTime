using System.Collections.Generic;
using UnityEngine;

public static class WordValidator
{
    private static HashSet<string> validWords = new HashSet<string>
    {
        "APPLE", "BANANA", "CHERRY", "DATE", "ELDERBERRY" // Example words
    };

    public static bool IsValidWord(string word)
    {
        return validWords.Contains(word);
    }
}
