using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LetterTile : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI letterText;
    [SerializeField] private Image backgroundImage;

    public char Letter { get; private set; }
    public Vector2Int GridPosition { get; private set; }

    private Color defaultColor = Color.white;
    private Color selectedColor = new Color(0.8f, 0.8f, 1f);
    private bool isSelected = false;

    public delegate void TileSelected(LetterTile tile);
    public static event TileSelected OnTileSelected;

    public void SetLetter(char letter, Vector2Int position)
    {
        Letter = letter;
        GridPosition = position;
        letterText.text = letter.ToString();
    }

    public void SetSelected(bool selected)
    {
        isSelected = selected;
        backgroundImage.color = selected ? selectedColor : defaultColor;
    }

    private void OnMouseDown()
    {
        if (!isSelected)
        {
            OnTileSelected?.Invoke(this);
        }
    }
}
