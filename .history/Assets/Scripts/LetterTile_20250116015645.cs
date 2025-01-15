using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public class LetterTile : MonoBehaviour, IPointerEnterHandler, IPointerDownHandler, IPointerUpHandler
{
    private Image backgroundImage;
    [SerializeField] private Color defaultColor = Color.white;
    [SerializeField] private Color selectedColor = Color.blue;
    private Color solvedColor;
    private bool isSolved = false;

    private void Awake()
    {
        backgroundImage = GetComponent<Image>();
    }

    public void SetSelected(bool selected)
    {
        if (!isSolved) // Only change color if not solved
        {
            backgroundImage.color = selected ? selectedColor : defaultColor;
        }
    }

    public void SetSolvedColor(Color color)
    {
        isSolved = true;
        solvedColor = color;
        backgroundImage.color = solvedColor;
    }

    public void ResetState()
    {
        isSolved = false;
        backgroundImage.color = defaultColor;
    }

    // In your SetLetter method, add:
    public void SetLetter(char letter, Vector2Int position)
    {
        Letter = letter;
        GridPosition = position;
        // Reset the state when setting a new letter
        ResetState();
        // Update the display
        if (textComponent != null)
        {
            textComponent.text = letter.ToString();
        }
    }
}