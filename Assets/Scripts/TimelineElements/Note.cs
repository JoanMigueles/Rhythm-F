using FMODUnity;
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
    public int damage = 20;
    public bool defeated = false;
    public bool hitPlayer = false;
    public bool missed = false;

    [field: Header("Hit")]
    [field: SerializeField] public EventReference hitReference { get; private set; }
    bool soundMade = false;

    public ParticleSystem killParticles;

    // Overrides the updating of positions while moving notes on the editor
    public override void Move(int distance, bool laneSwap)
    {
        float yPos;
        if (laneSwap)
            yPos = data.lane == 0 ? -1.5f : 1.5f;
        else
            yPos = data.lane == 0 ? 1.5f : -1.5f;
        transform.position = new Vector3(NoteManager.instance.GetPositionFromTime(data.time + distance), yPos, 0f);
    }

    // Updates position for this note (called from NoteManager)
    public override void UpdatePosition()
    {
        float yPos = data.lane == 0 ? 1.5f : -1.5f;
        transform.position = new Vector3(NoteManager.instance.GetPositionFromTime(data.time), yPos, 0f);
        CheckForSound();
        CheckForAppearance();
    }

    // Toggles the appearance of the note from the editor view to the playing/testing view mode (hiding ghosts, lines, etc...)
    public virtual void SetDisplayMode(bool gameplay)
    {
        if (gameplay) {
            // Notes on gameplay/testing
            gameObject.SetActive(false);
            if (Metronome.instance.GetTimelinePosition() > data.time) {
                defeated = true;
                hitPlayer = true;
                missed = true;
            }
            else {
                defeated = false;
                hitPlayer = false;
                missed = false;
            }
            CheckForAppearance();
        } else {
            // Notes on editor
            gameObject.SetActive(true);
            SetAnimated(false);
        }
    }

    // Sets the note as defeated while playing
    public virtual void Kill()
    {
        defeated = true;
        if (killParticles != null) {
            Instantiate(killParticles, transform.position, Quaternion.identity);
        }
        gameObject.SetActive(false);
    }

    // Sound cue for each note when playing the song on the editor
    public void CheckForSound()
    {
        if (Metronome.instance.IsPaused() || GameManager.instance.IsPlaying()) soundMade = false;
        else if (!soundMade && Metronome.instance.GetTimelinePosition() > data.time && Metronome.instance.GetTimelinePosition() < data.time + 100 && data.type != NoteType.Saw && data.type != NoteType.Laser) {
            RuntimeManager.PlayOneShot(hitReference);
            soundMade = true;
        }
    }

    // Checks the appearance of the note on screen when playing
    public virtual void CheckForAppearance()
    {
        if (!GameManager.instance.IsPlaying()) return;

        float screenLeft = Camera.main.ViewportToWorldPoint(new Vector3(0, 0, transform.position.z)).x - 5f;
        float screenRight = Camera.main.ViewportToWorldPoint(new Vector3(1, 0, transform.position.z)).x + 5f;

        if (transform.position.x <= screenLeft || transform.position.x >= screenRight) {
            if (gameObject.activeSelf) {
                gameObject.SetActive(false);
            }
        } else if (!gameObject.activeSelf && !defeated) {
            gameObject.SetActive(true);
            SetAnimated(true);
        }
    }

    // Set the appearance of the note to an idle animation (for gameplay) or not animated (for editor)
    public void SetAnimated(bool animated)
    {
        Float[] floatingSprites = GetComponentsInChildren<Float>();
        Spin[] spinningSprites = GetComponentsInChildren<Spin>();
        if (animated) {
            foreach (Float f in floatingSprites) {
                f.StartFloating();
            }
            foreach (Spin spin in spinningSprites) {
                spin.StartSpinning();
            }
        }
        else {
            foreach (Float f in floatingSprites) {
                f.StopFloating();
            }
            foreach (Spin spin in spinningSprites) {
                spin.StopSpinning();
            }
        }
    }
}

public class DurationNote : Note
{
    public NoteHandle durationHandle;

    public override void CheckForAppearance()
    {
        if (!GameManager.instance.IsPlaying()) return;

        float screenLeft = Camera.main.ViewportToWorldPoint(new Vector3(0, 0, transform.position.z)).x - 5f;
        float screenRight = Camera.main.ViewportToWorldPoint(new Vector3(1, 0, transform.position.z)).x + 5f;

        if (transform.position.x <= screenLeft - NoteManager.instance.GetDistanceFromTime(data.duration) || transform.position.x >= screenRight) {
            if (gameObject.activeSelf) {
                gameObject.SetActive(false);
            }
        }
        else if (!gameObject.activeSelf && !defeated) {
            gameObject.SetActive(true);
            SetAnimated(true);
        }
    }
}
