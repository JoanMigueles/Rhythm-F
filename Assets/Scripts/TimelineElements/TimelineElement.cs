using UnityEngine;

public class TimelineElement : MonoBehaviour
{
    public bool isSelected;
    public SpriteRenderer mainSprite;

    public virtual void SetSelected(bool selected)
    {
        isSelected = selected;
        if (mainSprite != null) {
            Color currentColor = mainSprite.color;
            mainSprite.color = selected ? new Color(0f, 1f, 0f, currentColor.a) : new Color(1f, 1f, 1f, currentColor.a);
        }
    }

    public virtual void Move(int distance, bool laneSwap)
    {
        
    }

    public virtual void UpdatePosition()
    {

    }
}
