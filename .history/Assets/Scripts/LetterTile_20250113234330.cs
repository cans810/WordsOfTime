using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;  // Add this

// Add IPointerClickHandler instead of using Button
public class LetterTile : MonoBehaviour, IPointerClickHandler
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
        isSelected = selected;
        backgroundImage.color = selected ? Color.yellow : Color.white;
    }

    // Replace OnClick with this
    public void OnPointerClick(PointerEventData eventData)
    {
        OnTileSelected?.Invoke(this);
    }
}