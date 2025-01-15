using System.Collections.Generic;

[System.Serializable]
public class WordSet
{
    public string era;
    public List<string> words;
}

[System.Serializable]
public class WordSetList
{
    public List<WordSet> sets;
}
