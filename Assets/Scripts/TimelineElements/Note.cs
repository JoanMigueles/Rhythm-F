using Unity.VisualScripting;
using UnityEngine;

[System.Serializable]
public enum NoteType
{
    Hit,
    Slash,
    Slider,
    Warn_Hit,
    Warn_Slash,
    Laser,
    Grab_Throw,
    Saw,
    Multiple_Hit,
    Multiple_Slash
}

[System.Serializable]
public struct NoteData
{
    public int time;
    public int lane;
    public NoteType type;
    public int duration;

    public NoteData(int time, int lane)
    {
        this.time = time;
        this.lane = lane;
        type = NoteType.Hit;
        duration = 0;
    }

    public NoteData(int time, int lane, NoteType type)
    {
        this.time = time;
        this.lane = lane;
        this.type = type;
        duration = 0;
    }

    public NoteData(int time, int lane, NoteType type, int duration)
    {
        this.time = time;
        this.lane = lane;
        this.type = type;
        this.duration = duration;
    }

    public NoteData(NoteData data)
    {
        this.time = data.time;
        this.lane = data.lane;
        this.type = data.type;
        this.duration = data.duration;
    }
}

public class Note : TimelineElement
{
    public NoteData data;

    public override void Move(int distance, bool laneSwap)
    {
        float yPos;
        if (laneSwap) {
            yPos = data.lane == 0 ? -1.5f : 1.5f;
        }
        else {
            yPos = data.lane == 0 ? 1.5f : -1.5f;
        }

        transform.position = new Vector3(NoteManager.instance.GetPositionFromTime(data.time + distance), yPos, 0f);
    }

    public override void UpdatePosition()
    {
        float yPos = data.lane == 0 ? 1.5f : -1.5f;
        transform.position = new Vector3(NoteManager.instance.GetPositionFromTime(data.time), yPos, 0f);
    }
}
