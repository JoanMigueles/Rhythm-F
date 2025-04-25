using UnityEngine;
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

public struct NoteData
{
    public int time { get; set; }
    public int lane { get; set; }
    public NoteType type { get; private set; }
    public int duration { get; set; }
    public int anticipation { get; set; }

    public NoteData(int time, int lane)
    {
        this.time = time;
        this.lane = lane;
        type = NoteType.Hit;
        duration = 0;
        anticipation = 0;
    }

    public NoteData(int time, int lane, NoteType type)
    {
        this.time = time;
        this.lane = lane;
        this.type = type;
        duration = 0;
        anticipation = 0;
    }

    public NoteData(int time, int lane, NoteType type, int duration)
    {
        this.time = time;
        this.lane = lane;
        this.type = type;
        this.duration = duration;
        anticipation = 0;
    }

    public NoteData(int time, int lane, NoteType type, int duration, int anticipation)
    {
        this.time = time;
        this.lane = lane;
        this.type = type;
        this.duration = duration;
        this.anticipation = anticipation;
    }

    public NoteData(NoteData data)
    {
        this.time = data.time;
        this.lane = data.lane;
        this.type = data.type;
        this.duration = data.duration;
        this.anticipation = data.anticipation;
    }
}

public class Note : MonoBehaviour
{
    public uint noteIndex;
    public NoteData data;
}
