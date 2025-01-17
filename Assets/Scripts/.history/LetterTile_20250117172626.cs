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

    private Color originalColor;

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
        if (!isSolved)
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
        isSolved = false;  // Reset the solved state for new letters
        backgroundImage.color = defaultColor;  // Reset color to default
        GetComponent<Image>().raycastTarget = true;
    }

    private void Awake()
    {
        letterText = GetComponentInChildren<TextMeshProUGUI>();
        backgroundImage = GetComponent<Image>();
    }

    public void ResetState()
    {
        // Remove this method or modify it to not reset solved state
        if (!isSolved)
        {
            backgroundImage.color = defaultColor;
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!isSolved && GridManager.Instance.IsSelecting())  // Fixed: Added parentheses to call the method
        {
            GridManager.Instance.AddToSelection(this);
        }
        else if (!isSolved)
        {
            GridManager.Instance.StartWordSelection(this);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        // Check if the current word is already solved
        if (WordGameManager.Instance.IsWordSolved(WordGameManager.Instance.targetWord))
        {
            return; // Don't allow selection if word is already solved
        }
        
        if (!isSolved && GridManager.Instance.IsSelecting)
        {
            GridManager.Instance.AddToSelection(this);
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        // Check if the current word is already solved
        if (WordGameManager.Instance.IsWordSolved(WordGameManager.Instance.targetWord))
        {
            return; // Don't allow selection if word is already solved
        }
        
        if (!isSolved)
        {
            GridManager.Instance.EndWordSelection();
        }
    }

    public Vector2Int GetGridPosition()
    {
        return gridPosition;
    }

    public void SetHighlightColor(Color color)
    {
        if (!isSolved)
        {
            backgroundImage.color = color;
        }
    }

    public void ResetHighlight()
    {
        if (!isSolved)
        {
            backgroundImage.color = defaultColor;
        }
    }

    public Color GetCurrentColor()
    {
        // Return the background color instead of text color
        return backgroundImage.color;
    }

    public void PreserveLetterDisplay()
    {
        if (letterText != null)
        {
            // Ensure the letter stays visible and unchanged
            letterText.text = Letter.ToString();
            letterText.color = Color.black; // Or whatever your default text color is
        }
    }

    public char GetLetter()
    {
        return Letter;
    }
}