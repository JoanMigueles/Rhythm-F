using UnityEngine;
using UnityEngine.UI;

public class ButtonModeHighlight : MonoBehaviour
{
    [SerializeField] private bool highlightOnStart;
    [SerializeField] private Color highlightColor = new Color(153, 60, 180, 255);
    private Color originalColor;
    private Image image;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        image = GetComponent<Image>();
        originalColor = image.color;
        if (highlightOnStart)
            EditorUI.instance.SetHighlightedButton(this);
    }

    public void Highlight(bool on)
    {
        Color highlight = new Color(highlightColor.r, highlightColor.g, highlightColor.b, originalColor.a);
        image.color = on ? highlight : originalColor;
    }
}
