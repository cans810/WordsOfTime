using UnityEngine;
using UnityEngine.UI;

public class LetterTile : MonoBehaviour
{
    public char Letter { get; private set; }
    private Vector2Int gridPos;
    public bool isSolved = false;

    [SerializeField] private Image background;
    [SerializeField] private Text letterText;
    private Color defaultColor;

    private void Awake()
    {
        if (background != null) defaultColor = background.color;
    }

    public void SetLetter(char c, Vector2Int pos)
    {
        Letter = c;
        gridPos = pos;
        if (letterText != null) letterText.text = (c == '\0') ? "" : c.ToString();
    }

    public void ResetTile()
    {
        isSolved = false;
        SetSelected(false);
        if (background != null)
        {
            background.color = defaultColor;
        }
    }

    public void SetSelected(bool selected)
    {
        if (background != null)
        {
            background.color = selected ? Color.yellow : defaultColor;
        }
    }

    public void SetSolvedColor()
    {
        if (background != null)
        {
            background.color = Color.green;
        }
    }

    public Vector2Int GetGridPosition()
    {
        return gridPos;
    }
}
