// Add this method to clear solved words display
private void ClearSolvedWordsDisplay()
{
    if (GridManager.Instance != null)
    {
        GridManager.Instance.ClearSolvedWords();
    }
    
    // Also clear any other related state if needed
    solvedWords.Clear();
    solvedWordPositions.Clear();
    solvedBaseWordsPerEra.Clear();
} 