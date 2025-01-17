// Your original Word classes:
[System.Serializable]
public class WordSetList
{
    public WordSet[] sets;
}

[System.Serializable]
public class WordSet
{
    public string era;
    public Word[] words;
}

[System.Serializable]
public class Word
{
    public string word;
    public string[] sentences;
}