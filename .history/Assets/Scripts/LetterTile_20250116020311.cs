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
    private bool isSolved = false;

    [SerializeField] private Color defaultColor = Color.white;
    [SerializeField] private Color selectedColor = Color.yellow;
    [SerializeField] private Color solvedColor = new Color(1f, 0.92f, 0.016f); // Bright yellow for solved words

    [SerializeField] private Color solvedColor = Color.yellow;
    private bool isSolved = false;

    public void SetSolvedColor()
    {
        isSolved = true;
        backgroundImage.color = solvedColor;
        // Optionally disable the collider/raycaster if you want to prevent any interaction
        GetComponent<Image>().raycastTarget = false;
    }

    // Modify SetLetter to reset the state
    public void SetLetter(char letter, Vector2Int position)
    {
        Letter = letter;
        gridPosition = position;
        letterText.text = letter.ToString();
        isSolved = false;
        backgroundImage.color = defaultColor;
        GetComponent<Image>().raycastTarget = true;
    }


    private void Awake()
    {
        letterText = GetComponentInChildren<TextMeshProUGUI>();
        backgroundImage = GetComponent<Image>();
    }
    
    public void SetSelected(bool selected)
    {
        if (!isSolved) // Only change color if not solved
        {
            backgroundImage.color = selected ? selectedColor : defaultColor;
        }
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