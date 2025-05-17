using UnityEngine;
using static UnityEngine.Rendering.VirtualTexturing.Debugging;

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
        switch (type) {
            case HandleType.Duration:
                horizontalPosition = EditorManager.instance.GetDistanceFromTime(note.data.duration + distance);
                if (horizontalPosition < 0f) horizontalPosition = 0f;
                break;
            case HandleType.Anticipation:
                horizontalPosition = EditorManager.instance.GetDistanceFromTime(note.data.anticipation + distance);
                if (horizontalPosition > 0f) horizontalPosition = 0f;
                break;
        }
        transform.localPosition = new Vector3(horizontalPosition, 0f, 0f);
    }
}
