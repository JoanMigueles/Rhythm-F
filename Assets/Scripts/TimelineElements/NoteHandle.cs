using Unity.VisualScripting;
using UnityEngine;

public enum HandleType
{
    Duration,
    Anticipation
}

public class NoteHandle : TimelineElement
{
    public Note note;
    public HandleType type;

    public override void Move(int distance, bool laneSwap)
    {
        if (note.isSelected) return;
        float horizontalPosition = 0;
        horizontalPosition = NoteManager.instance.GetDistanceFromTime(note.data.duration + distance);
        if (horizontalPosition < 0f) horizontalPosition = 0f;
        transform.localPosition = new Vector3(horizontalPosition, 0f, 0f);
    }

    public void SetDuration(int duration)
    {
        float horizontalPosition = 0;
        horizontalPosition = NoteManager.instance.GetDistanceFromTime(duration);
        if (horizontalPosition < 0f) horizontalPosition = 0f;
        transform.localPosition = new Vector3(horizontalPosition, 0f, 0f);
    }
}
