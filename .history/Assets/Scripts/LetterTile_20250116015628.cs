using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public class LetterTile : MonoBehaviour, IPointerEnterHandler, IPointerDownHandler, IPointerUpHandler
{
    private TextMeshProUGUI letterText;
    private Image backgroundImage;
    public char Letter { get; private set; }
    private Vector2Int gridPosition;
    
    [SerializeField] private Color defaultColor = Color.white;
    [SerializeField] private Color selectedColor = Color.yellow;
    
    private void Awake()
    {
        letterText = GetComponentInChildren<TextMeshProUGUI>();
        backgroundImage = GetComponent<Image>();
    }

    public void SetLetter(char letter, Vector2Int position)
    {
        Letter = letter;
        gridPosition = position;
        letterText.text = letter.ToString();
    }

    public void SetSelected(bool selected)
    {
        backgroundImage.color = selected ? selectedColor : defaultColor;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        GridManager.Instance.StartWordSelection(this);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (GridManager.Instance.IsSelecting)
        {
            GridManager.Instance.AddToSelection(this);
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        GridManager.Instance.EndWordSelection();
    }

    public Vector2Int GetGridPosition()
    {
        return gridPosition;
    }
}