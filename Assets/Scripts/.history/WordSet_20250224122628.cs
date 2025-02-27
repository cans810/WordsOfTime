[System.Serializable]
public class Translations
{
    public string en;
    public string tr;
}

[System.Serializable]
public class WordEntry
{
    public string word;
    public string[] sentences;
    public Translations translations;
    public string didYouKnow;
    public string difficulty;
}ssssssssssssssssssssssssssssssssss

[System.Serializable]
public class WordSet
{
    public string era;
    public WordEntry[] words;
}

[System.Serializable]
public class WordSetList
{
    public WordSet[] sets;
}