using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LetterTile : MonoBehaviour
{
    public static event System.Action<LetterTile> OnTileSelected;

    public char Letter { get; private set; }
    public Vector2Int GridPosition { get; private set; }

    public bool isSelected = false;

    [SerializeField] private TextMeshProUGUI letterText;
    [SerializeField] private Image backgroundImage;

    public void SetLetter(char letter, Vector2Int gridPosition)
    {
        Letter = letter;
        GridPosition = gridPosition;
        letterText.text = letter.ToString();
    }

    public void SetSelected(bool selected)
    {
        if (isSelected == selected) return;

        isSelected = selected;
        backgroundImage.color = selected ? Color.yellow : Color.white;

        if (isSelected)
        {
            // Trigger the event when the tile is selected
            OnTileSelected?.Invoke(this);
        }
    }

    // This method will be invoked when the button is clicked
    public void OnClick()
{
    OnTileSelected?.Invoke(this);  // Just invoke the event, let GridManager handle the state
}
}
