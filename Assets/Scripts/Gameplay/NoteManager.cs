using FMODUnity;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

[System.Serializable]
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
    public float headstart = 3f;

    [field: Header("Test Song")]
    [field: SerializeField] public EventReference testSongReference { get; private set; }
    [field: Header("Ready Go")]
    [field: SerializeField] public EventReference readyReference { get; private set; }

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

    private bool songEnded;

    protected virtual void Awake()
    {
        instance = this;
    }

    protected virtual void Start()
    {
        activeNotes = new List<Note>();

        if (GameManager.instance.IsSongSelected()) {
            var song = GameManager.instance.GetSelectedSong();
            if (song.HasValue)
                LoadSelectedSong(song.Value);
        }
        else {
            SongDataResource loaded = Resources.Load<SongDataResource>("SongData");
            songData = loaded.data;
            Metronome.instance.SetSong(testSongReference);
        }

        SpawnDifficultyNotes(GameManager.instance.GetSelectedDifficulty());
        List<NoteData> notesData = GetDifficultyNoteData(GameManager.instance.GetSelectedDifficulty());
        
        float delay = headstart;
        if (notesData.Count > 0 && notesData[0].time <= headstart) {
            delay = headstart - notesData[0].time;
        }
        Metronome.instance.PlaySongHeadstart(delay);
        RuntimeManager.PlayOneShot(readyReference);
        Metronome.instance.smoothTimelineOn = true;
        GameManager.instance.SetPlaying(true);

        foreach (var note in activeNotes) {
            note.SetDisplayMode(true);
        }

    }

    private void Update()
    {
        UpdateNotesPosition();
    }

    protected void UpdateNotesPosition()
    {
        foreach (Note note in activeNotes) {
            note.UpdatePosition();
        }
    }

    protected virtual void LoadSelectedSong(SongMetadata metadata)
    {
        if (metadata.songID == -1) {
            songData = SaveData.LoadCustomSong(metadata.localPath);
            Metronome.instance.SetBPMFlags(songData.BPMFlags);
            string songPath = SaveData.GetAudioFilePath(songData.metadata.audioFileName);
            if (File.Exists(songPath)) {
                Metronome.instance.SetCustomSong(songPath);
            }
        } else {

            songData = ResourceLoader.LoadSong(metadata.songID);
            Metronome.instance.SetBPMFlags(songData.BPMFlags);
            EventReference reference = ResourceLoader.LoadEventReference(metadata.songID);
            Metronome.instance.SetSong(reference);
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

    public bool IsPastLastNote()
    {
        List<NoteData> notes = GetDifficultyNoteData(difficulty);
        return Metronome.instance.GetTimelinePosition() >= notes[notes.Count - 1].time;
    }

    public float GetAccuracy(int perfects, int greats, int misses)
    {
        List<NoteData> notes = GetDifficultyNoteData(difficulty);
        float maxPossibleGrade = 0;
        foreach (NoteData note in notes) {
            switch (note.type) {
                case NoteType.Slider:
                    maxPossibleGrade += 2;
                    break;
                case NoteType.Saw:
                case NoteType.Laser:
                    break;
                default:
                    maxPossibleGrade += 1;
                    break;
            }
        }


        if (perfects + greats + misses != maxPossibleGrade) Debug.LogWarning($"Incoherence found between hits and max possible hits: {perfects + greats + misses}, {maxPossibleGrade}");
        return (perfects + greats * (2 / 3)) / maxPossibleGrade * 100;

    }

    // ---------------------------------------------------------------------------------------------------------------------------------------------
    // AUDIO TO POSITION
    // ---------------------------------------------------------------------------------------------------------------------------------------------
    public int GetTimeFromPosition(float horizontalPos)
    {
        return Mathf.RoundToInt(Metronome.instance.GetTimelinePosition() + (horizontalPos / noteSpeed * 1000f));
    }

    public float GetPositionFromTime(int time)
    {
        return (time - Metronome.instance.GetTimelinePosition()) / 1000f * noteSpeed;
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
