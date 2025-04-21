using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

public class NoteManager : MonoBehaviour
{
    public static NoteManager instance { get; private set; }
    public float noteSpeed = 5f;
    public int noteSubdivisionSnapping = 1;

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
        float horizontalPosition = (worldPosition.x > 0) ? worldPosition.x : 0;
        float verticalPosition = worldPosition.y;

        if (Input.GetKey(KeyCode.LeftControl)) {
            float yPos = verticalPosition > 0 ? 1.5f : -1.5f;
            notePreview.transform.position = new Vector3(horizontalPosition, yPos, 0f);
        }
        else {
            float xPos = GetPositionFromBeat(Mathf.Round(GetBeatFromPosition(horizontalPosition) * noteSubdivisionSnapping) / noteSubdivisionSnapping);
            float yPos = verticalPosition > 0 ? 1.5f : -1.5f;
            notePreview.transform.position = new Vector3(xPos, yPos, 0);
        }

        if (Input.GetMouseButtonDown(0)) {
            int lane = verticalPosition > 0 ? 0 : 1;
            int time = GetTimeFromPosition(notePreview.transform.position.x);

            // Check if a note already exists at this time and lane
            bool noteExists = activeNotes.Exists(n =>
                n.data.time == time && n.data.lane == lane);

            if (!noteExists) {
                NoteData newNoteData = new NoteData(time, lane);
                Note newNote = Instantiate(hitPrefab, transform).GetComponent<Note>();
                newNote.data = newNoteData;
                Debug.Log(newNote.data.time);
                activeNotes.Add(newNote);
            }
        }

        foreach (Note note in activeNotes) {
            UpdateNotePosition(note);
        }
    }

    private void UpdateNotePosition(Note note)
    {
        float yPos = note.data.lane == 0 ? 1.5f : -1.5f;
        note.transform.position = new Vector3(GetPositionFromTime(note.data.time), yPos, 0f);
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
