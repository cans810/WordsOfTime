using System.Collections.Generic;
using UnityEngine;

public class GridManager : MonoBehaviour
{
    // Add this method to find a word in the grid
    public List<Vector2Int> FindWordInGrid(string word, LetterTile[,] grid)
    {
        if (string.IsNullOrEmpty(word) || grid == null) return null;

        int gridSize = grid.GetLength(0);
        bool[,] visited = new bool[gridSize, gridSize];
        List<Vector2Int> result = new List<Vector2Int>();

        for (int x = 0; x < gridSize; x++)
        {
            for (int y = 0; y < gridSize; y++)
            {
                if (DFS(x, y, word.ToUpper(), 0, visited, result))
                {
                    return result;
                }
            }
        }

        return null;
    }

    private bool DFS(int x, int y, string word, int index, bool[,] visited, List<Vector2Int> path)
    {
        if (index == word.Length) return true;

        if (x < 0 || x >= gridSize || y < 0 || y >= gridSize || 
            visited[x, y] || grid[x, y] == null || 
            grid[x, y].GetLetter() != word[index])
        {
            return false;
        }

        visited[x, y] = true;
        path.Add(new Vector2Int(x, y));

        // Check all 8 directions
 