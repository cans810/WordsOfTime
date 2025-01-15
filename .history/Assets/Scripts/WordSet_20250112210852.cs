using System;
using System.Collections.Generic;

[Serializable]
public class WordEntry
{
    public string word;         // The word itself
    public List<string> sentences;  // List of sentences associated with the word
}

[Serializable]
public class WordSet
{
    public string era;          // The era associated with the word set
    public List<WordEntry> words;  // List of words in the set
}

[Serializable]
public class WordSetList
{
    public List<WordSet> sets;  // List of word sets
}
