// Structure to store pre-generated grid data
using System.Collections.Generic;
using UnityEngine;

public class PreGeneratedGrid
    {
        public char[,] letters;  // The grid of letters
        public List<Vector2Int> wordPositions;  // Positions of the target word's letters
        public Dictionary<char, List<Vector2Int>> letterPositions;  // All positions for each letter
        
        public PreGeneratedGrid(int size)
        {
            letters = new char[size, size];
            wordPositions = new List<Vector2Int>();
            letterPositions = new Dictionary<char, List<Vector2Int>>();
        }

        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
                LoadWordSets();
                PreGenerateAllGrids();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void PreGenerateAllGrids()
        {
            preGeneratedGrids = new Dictionary<string, Dictionary<string, PreGeneratedGrid>>();
            
            foreach (var era in wordSetsWithSentences.Keys)
            {
                preGeneratedGrids[era] = new Dictionary<string, PreGeneratedGrid>();
                foreach (var word in wordSetsWithSentences[era].Keys)
                {
                    preGeneratedGrids[era][word] = GenerateGridForWord(word);
                }
            }
            
            Debug.Log($"Pre-generated grids for {preGeneratedGrids.Count} eras");
        }

        private PreGeneratedGrid GenerateGridForWord(string word)
        {
            const int GRID_SIZE = 5;
            PreGeneratedGrid grid = new PreGeneratedGrid(GRID_SIZE);
            
            // Place the word's letters in adjacent positions
            List<Vector2Int> validStartPositions = new List<Vector2Int>();
            for (int x = 0; x < GRID_SIZE; x++)
            {
                for (int y = 0; y < GRID_SIZE; y++)
                {
                    validStartPositions.Add(new Vector2Int(x, y));
                }
            }
            
            // Shuffle start positions for randomness
            for (int i = validStartPositions.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                var temp = validStartPositions[i];
                validStartPositions[i] = validStartPositions[j];
                validStartPositions[j] = temp;
            }

            foreach (var startPos in validStartPositions)
            {
                if (TryPlaceWord(word, startPos, grid, GRID_SIZE))
                {
                    FillRemainingSpaces(grid, GRID_SIZE);
                    return grid;
                }
            }

            Debug.LogError($"Failed to generate grid for word: {word}");
            return null;
        }

        private bool TryPlaceWord(string word, Vector2Int startPos, PreGeneratedGrid grid, int gridSize)
        {
            List<Vector2Int> positions = new List<Vector2Int>();
            Vector2Int currentPos = startPos;

            for (int i = 0; i < word.Length; i++)
            {
                if (!IsValidPosition(currentPos, gridSize) || grid.letters[currentPos.x, currentPos.y] != '\0')
                {
                    return false;
                }

                grid.letters[currentPos.x, currentPos.y] = word[i];
                positions.Add(currentPos);

                if (!grid.letterPositions.ContainsKey(word[i]))
                {
                    grid.letterPositions[word[i]] = new List<Vector2Int>();
                }
                grid.letterPositions[word[i]].Add(currentPos);

                if (i < word.Length - 1)
                {
                    var nextPos = GetNextValidPosition(currentPos, grid, gridSize);
                    if (!nextPos.HasValue) return false;
                    currentPos = nextPos.Value;
                }
            }

            grid.wordPositions = positions;
            return true;
        }

        private Vector2Int? GetNextValidPosition(Vector2Int current, PreGeneratedGrid grid, int gridSize)
        {
            Vector2Int[] directions = new[]
            {
                new Vector2Int(1, 0),
                new Vector2Int(0, 1),
                new Vector2Int(-1, 0),
                new Vector2Int(0, -1)
            };

            List<Vector2Int> validPositions = new List<Vector2Int>();

            foreach (var dir in directions)
            {
                Vector2Int newPos = current + dir;
                if (IsValidPosition(newPos, gridSize) && grid.letters[newPos.x, newPos.y] == '\0')
                {
                    validPositions.Add(newPos);
                }
            }

            if (validPositions.Count == 0) return null;
            return validPositions[Random.Range(0, validPositions.Count)];
        }

        private void FillRemainingSpaces(PreGeneratedGrid grid, int gridSize)
        {
            for (int x = 0; x < gridSize; x++)
            {
                for (int y = 0; y < gridSize; y++)
                {
                    if (grid.letters[x, y] == '\0')
                    {
                        char randomLetter = (char)Random.Range('A', 'Z' + 1);
                        grid.letters[x, y] = randomLetter;
                        
                        if (!grid.letterPositions.ContainsKey(randomLetter))
                        {
                            grid.letterPositions[randomLetter] = new List<Vector2Int>();
                        }
                        grid.letterPositions[randomLetter].Add(new Vector2Int(x, y));
                    }
                }
            }
        }

        private bool IsValidPosition(Vector2Int pos, int gridSize)
        {
            return pos.x >= 0 && pos.x < gridSize && pos.y >= 0 && pos.y < gridSize;
        }

        public PreGeneratedGrid GetPreGeneratedGrid(string era, string word)
        {
            if (preGeneratedGrids.TryGetValue(era, out var eraGrids))
            {
                if (eraGrids.TryGetValue(word.ToUpper(), out var grid))
                {
                    return grid;
                }
            }
            return null;
        }
    }