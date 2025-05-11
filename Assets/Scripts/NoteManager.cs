using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;

public class NoteManager : MonoBehaviour
{
    public static NoteManager instance { get; private set; }
    public float noteSpeed = 5f;
    public Difficulty difficulty = Difficulty.Normal;

    // NOTE PREFABS
    [SerializeField] private GameObject hitPrefab;
    [SerializeField] private GameObject sliderPrefab;
    [SerializeField] private GameObject warnHitPrefab;

    // NOTE LISTS
    private SongData songData;
    private List<Note> activeNotes;

    private void Awake()
    {
        instance = this;
    }

    private void Start()
    {
        activeNotes = new List<Note>();
        if (GameManager.instance.IsSongSelected()) {
            LoadSelectedSong(GameManager.instance.GetSelectedSong());
        }
        if (songData == null) {
            // SET DEFAULT SONG
            songData = new SongData();
        }

        //  SPAWN INITAL ELEMENTS
        List<NoteData> notesData = GetDifficultyNoteData(Difficulty.Normal);
        foreach (NoteData noteData in notesData) {
            SpawnNote(noteData, false);
        }
    }

    private void Update()
    {
        foreach (Note note in activeNotes) {
            UpdateNotePosition(note);
        }
    }

    public void LoadSelectedSong(string songFilePath)
    {
        songData = SaveData.LoadCustomSong(songFilePath);
        string songPath = SaveData.GetAudioFilePath(songData.metadata.audioFileName);
        if (File.Exists(songPath)) {
            Metronome.instance.SetCustomSong(songPath);
            EditorUI.instance.ApplyWaveformTexture();
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

    public void SpawnNote(NoteData noteData, bool select)
    {
        Note newNote = Instantiate(hitPrefab, transform).GetComponent<Note>();
        newNote.data = noteData;
        activeNotes.Add(newNote);
    }

    private void UpdateNotePosition(Note note)
    {
        float yPos = note.data.lane == 0 ? 1.5f : -1.5f;
        note.transform.position = new Vector3(GetPositionFromTime(note.data.time), yPos, 0f);
    }

    // ---------------------------------------------------------------------------------------------------------------------------------------------
    // EDITOR PARAMETER SETTERS
    // ---------------------------------------------------------------------------------------------------------------------------------------------
    public void SetSpeed(float speed)
    {
        noteSpeed = speed;
    }

    // ---------------------------------------------------------------------------------------------------------------------------------------------
    // AUDIO TO POSITION
    // ---------------------------------------------------------------------------------------------------------------------------------------------
    public float GetPositionFromTime(int time)
    {
        return (time - Metronome.instance.GetTimelinePosition()) / 1000f * noteSpeed;
    }
}
