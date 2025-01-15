using System.Collections.Generic;

[System.Serializable]
public class WordEntry
{
    public string word;
    public string sentence;
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

