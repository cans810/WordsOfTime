using System.Collections.Generic;

[System.Serializable]
public class WordEntry
{
    public string word;
    public string[] sentences;  // This is an array of sentences
}

[System.Serializable]
public class WordSet
{
    public string era;
    public WordEntry[] words;  // Array of words with sentences
}

[System.Serializable]
public class WordSetList
{
    public List<WordSet> sets;
}

