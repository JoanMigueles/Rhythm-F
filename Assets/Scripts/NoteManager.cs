using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public enum Difficulty
{
    Easy,
    Normal,
    Hard,
    Rumble
}

public class NoteManager : MonoBehaviour
{
    public static NoteManager instance { get; private set; }
    public float noteSpeed = 5f;
    public Difficulty difficulty = Difficulty.Normal;

    // NOTE PREFABS
    [SerializeField] private GameObject hitPrefab;
    [SerializeField] private GameObject slashPrefab;
    [SerializeField] private GameObject sliderPrefab;
    [SerializeField] private GameObject warnHitPrefab;
    [SerializeField] private GameObject warnSlashPrefab;
    [SerializeField] private GameObject laserPrefab;
    [SerializeField] private GameObject grabThrowPrefab;
    [SerializeField] private GameObject sawPrefab;
    [SerializeField] private GameObject multipleHitPrefab;
    [SerializeField] private GameObject multipleSlashPrefab;

    protected SongData songData;
    public List<Note> activeNotes;

    private const int HEADSTART = 2000;
    private float headstartTimer;
    private bool songStarted = false;

    protected virtual void Awake()
    {
        instance = this;
    }

    protected virtual void Start()
    {
        activeNotes = new List<Note>();

        if (GameManager.instance.IsSongSelected()) {
            LoadSelectedSong(GameManager.instance.GetSelectedSong());
        }
        else {
            SongDataResource loaded = Resources.Load<SongDataResource>("SongData");
            songData = loaded.data;
            string songPath = SaveData.GetAudioFilePath(songData.metadata.audioFileName);
            if (File.Exists(songPath)) {
                Metronome.instance.SetCustomSong(songPath);
            }
        }

        SpawnDifficultyNotes(Difficulty.Normal);
        List<NoteData> notesData = GetDifficultyNoteData(Difficulty.Normal);
        if (notesData.Count > 0) {
            if (notesData[0].time > HEADSTART) {
                headstartTimer = 0;
            } else {
                headstartTimer = HEADSTART - notesData[0].time;
            }
        }
    }

    private void Update()
    {
        if (!songStarted) {
            headstartTimer -= Time.deltaTime * 1000f;

            if (headstartTimer <= 0f) {
                songStarted = true;
                headstartTimer = 0f;
                Metronome.instance.PlaySong();
            }
        }
        UpdateNotesPosition();
    }

    protected void UpdateNotesPosition()
    {
        foreach (Note note in activeNotes) {
            note.UpdatePosition();
        }
    }

    protected virtual void LoadSelectedSong(string songFilePath)
    {
        songData = SaveData.LoadCustomSong(songFilePath);
        string songPath = SaveData.GetAudioFilePath(songData.metadata.audioFileName);
        if (File.Exists(songPath)) {
            Metronome.instance.SetCustomSong(songPath);
        }
    }

    public List<NoteData> GetDifficultyNoteData(Difficulty difficulty)
    {
        switch (difficulty) {
            case Difficulty.Easy:
                return songData.easyNotes;
            case Difficulty.Normal:
                return songData.normalNotes;
            case Difficulty.Hard:
                return songData.hardNotes;
            case Difficulty.Rumble:
                return songData.rumbleNotes;
        }
        return null;
    }

    public virtual void SpawnDifficultyNotes(int diff)
    {
        difficulty = (Difficulty)diff;
        Debug.Log("Spawning difficulty: " +  difficulty.ToString());

        List<NoteData> notesData = GetDifficultyNoteData((Difficulty)diff);
        foreach (NoteData noteData in notesData) {
            SpawnNote(noteData);
        }
    }

    public virtual void SpawnDifficultyNotes(Difficulty diff)
    {
        List<NoteData> notesData = GetDifficultyNoteData(diff);
        foreach (NoteData noteData in notesData) {
            SpawnNote(noteData);
        }
    }

    public Note SpawnNote(NoteData noteData)
    {
        GameObject prefab = GetNoteTypePrefab(noteData.type);
        Note newNote = Instantiate(prefab, transform).GetComponent<Note>();
        newNote.data = noteData;
        activeNotes.Add(newNote);
        activeNotes = activeNotes.OrderBy(note => note.data.time).ThenBy(note => note.data.lane).ToList();
        return newNote;
    }

    protected GameObject GetNoteTypePrefab(NoteType type)
    {
        switch (type) {
            case NoteType.Hit: return hitPrefab;
            case NoteType.Slash: return slashPrefab;
            case NoteType.Slider: return sliderPrefab;
            case NoteType.Warn_Hit: return warnHitPrefab;
            case NoteType.Warn_Slash: return warnSlashPrefab;
            case NoteType.Laser: return laserPrefab;
            case NoteType.Grab_Throw: return grabThrowPrefab;
            case NoteType.Saw: return sawPrefab;
            case NoteType.Multiple_Hit: return multipleHitPrefab;
            case NoteType.Multiple_Slash: return multipleSlashPrefab;
            default: return hitPrefab;
        }
    }

    // ---------------------------------------------------------------------------------------------------------------------------------------------
    // AUDIO TO POSITION
    // ---------------------------------------------------------------------------------------------------------------------------------------------
    public int GetTimeFromPosition(float horizontalPos)
    {
        return Mathf.RoundToInt(Metronome.instance.GetTimelinePosition() - headstartTimer + (horizontalPos / noteSpeed * 1000f));
    }

    public float GetPositionFromTime(int time)
    {
        return (time - Metronome.instance.GetTimelinePosition() + headstartTimer) / 1000f * noteSpeed;
    }

    public float GetDistanceFromTime(int time)
    {
        return time / 1000f * noteSpeed;
    }

    public float GetBeatFromPosition(float horizontalPosition)
    {
        // Add to current timeline beat position
        return Metronome.instance.GetTimelineBeatPosition() + horizontalPosition / noteSpeed / Metronome.instance.beatSecondInterval;
    }

    public float GetPositionFromBeat(float beat)
    {
        return GetPositionFromTime(Metronome.instance.GetTimeFromBeat(beat));
    }
}
