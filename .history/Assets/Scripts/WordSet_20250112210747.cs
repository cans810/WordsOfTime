using System.Collections.Generic;

[System.Serializable]
public class WordEntry
{
    public string word;
    public List<string> sentences;  // Fixed: Change sentences to a List<string> as it's supposed to hold multiple sentences
}

[System.Serializable]
public class WordSet
{
    public string era;
    public List<WordEntry> words;
}

[System.Serializable]
public class WordSetList
{
    public List<WordSet> sets;
}

