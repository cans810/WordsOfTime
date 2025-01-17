[System.Serializable]
public class WordGridData
{
    public string word;
    public List<char> grid;
}

[System.Serializable]
public class WordGridList
{
    public List<WordGridData> grids;
}