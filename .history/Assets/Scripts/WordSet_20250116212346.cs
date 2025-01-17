// Classes for JSON parsing
[System.Serializable]
public class WordSetList
{
    public WordSet[] sets;
}

[System.Serializable]
public class WordSet
{
    public string era;
    public WordEntry[] words;
}

[System.Serializable]
public class WordEntry
{
    public string word;
    public string[] sentences;
}