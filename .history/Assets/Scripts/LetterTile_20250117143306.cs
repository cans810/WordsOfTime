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

    [SerializeField] private Color solvedColor = Color.yellow;
    public bool isSolved = false;

    public string SolvedWord { get; private set; } = "";  // Add this

    public void SetSolvedColor()
    {
        backgroundImage.color = solvedColor; // Use the serialized solvedColor
        isSolved = true;
    }

    public void SetSelected(bool isSelected)
    {
        // Only change color if not solved
        if (!isSolved)
        {
            backgroundImage.color = isSelected ? selectedColor : defaultColor;
        }
    }

    public void ResetTile()
    {
        SetSelected(false);
        if (!isSolved) // Use the isSolved flag directly.
        {
            backgroundImage.color = defaultColor;
        }
    }

    // Modify SetLetter to reset the state
    public void SetLetter(char letter, Vector2Int position, string solvedWord = "")
    {
        Letter = letter;
        gridPosition = position;
        letterText.text = letter.ToString();
        
        // Only reset color if not solved
        if (!isSolved)
        {
            backgroundImage.color = defaultColor;
        }
        GetComponent<Image>().raycastTarget = true;
    }


    private void Awake()
    {
        letterText = GetComponentInChildren<TextMeshProUGUI>();
        backgroundImage = GetComponent<Image>();
    }


    public void ResetState()
    {
        isSolved = false;
        backgroundImage.color = defaultColor;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!isSolved) // Only allow selection if not part of a solved word
        {
            GridManager.Instance.StartWordSelection(this);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!isSolved && GridManager.Instance.IsSelecting)
        {
            GridManager.Instance.AddToSelection(this);
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!isSolved)
        {
            GridManager.Instance.EndWordSelection();
        }
    }

    public Vector2Int GetGridPosition()
    {
        return gridPosition;
    }
}