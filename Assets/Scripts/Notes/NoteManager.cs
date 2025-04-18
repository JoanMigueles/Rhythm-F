using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

public class NoteManager : MonoBehaviour
{
    public static NoteManager instance { get; private set; }
    public float noteSpeed = 5f;

    [SerializeField] private GameObject hitPrefab;
    [SerializeField] private GameObject sliderPrefab;
    [SerializeField] private GameObject warnHitPrefab;
    public GameObject notePreview;

    private List<NoteData> notesData;
    private List<Note> activeNotes;

    private void Start()
    {
        notesData = new List<NoteData>();
        activeNotes = new List<Note>();
        SpawnNotes();
    }

    private void Awake()
    {
        if (instance == null) {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else {
            Destroy(gameObject);
        }
    }

    private void SpawnNotes()
    {
        foreach (NoteData noteData in notesData) {
            Note newNote = Instantiate(hitPrefab, transform).GetComponent<Note>();
            newNote.data = noteData;
            UpdateNotePosition(newNote);
            activeNotes.Add(newNote);
        }
    }

    private void Update()
    {
        /*
        (float start, float end) beatWindow = GetTimelineBeatWindow();

        for (int n = 0; n < notes.Count; n++) {
            float beat = testNoteBeats[n];
            float beatSpacing = 1 / beatTiling * timelineWidth;
            float xPosition = (beat - editorBeat) * beatSpacing;
            GameObject note = notes[n];
            RectTransform noteRect = note.GetComponent<RectTransform>();
            noteRect.anchoredPosition = new Vector2(xPosition, 0);

            if (beat > beatWindow.start && beat < beatWindow.end) {
                note.SetActive(true);
            }
            else {
                note.SetActive(false);
            }
        }*/

        // Convert mouse screen position to world position
        Vector3 worldPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        // Get just the horizontal (x) position
        float horizontalPosition = worldPosition.x;

        if (Input.GetKey(KeyCode.LeftControl)) {
            notePreview.transform.position = new Vector3(horizontalPosition, -1.5f, 0f);
        }
        else {
            notePreview.transform.position = new Vector3(GetPositionFromBeat(Mathf.RoundToInt(GetBeatFromPosition(horizontalPosition))), -1.5f, 0);
        }

        if (Input.GetMouseButtonDown(0)) {
            NoteData newNoteData = new NoteData(GetTimeFromPosition(notePreview.transform.position.x));
            Note newNote = Instantiate(hitPrefab, transform).GetComponent<Note>();
            newNote.data = newNoteData;
            activeNotes.Add(newNote);
        }

        foreach (Note note in activeNotes) {
            UpdateNotePosition(note);
        }
    }

    private void UpdateNotePosition(Note note)
    {
        note.transform.position = new Vector3(GetPositionFromTime(note.data.time), 1.5f, 0f);
    }

    public void SetSpeed(float speed)
    {
        noteSpeed = speed;
    }

    public int GetTimeFromPosition(float horizontalPos) 
    {
        return Metronome.instance.GetTimelinePosition() + Mathf.RoundToInt(horizontalPos / noteSpeed * 1000f);
    }

    public float GetBeatFromPosition(float horizontalPosition)
    {
        return Metronome.instance.GetBeatFromTime(GetTimeFromPosition(horizontalPosition));
    }

    public float GetPositionFromTime(int time)
    {
        return (time - Metronome.instance.GetTimelinePosition()) / 1000f * noteSpeed;
    }

    public float GetPositionFromBeat(float beat)
    {
        return GetPositionFromTime(Metronome.instance.GetTimeFromBeat(beat));
    }
}
