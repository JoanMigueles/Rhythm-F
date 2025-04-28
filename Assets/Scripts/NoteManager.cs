using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UIElements;
using VFolders.Libs;

public enum EditMode
{
    Object,
    Select
}
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
    public bool beatSnapping = true;
    public int noteSubdivisionSnapping = 1;
    public EditMode editMode = EditMode.Select;
    public Difficulty difficulty = Difficulty.Normal;

    // NOTE PREFABS
    [SerializeField] private GameObject hitPrefab;
    [SerializeField] private GameObject sliderPrefab;
    [SerializeField] private GameObject warnHitPrefab;
    [SerializeField] private GameObject notePreview;

    // SELECTION BOX
    [SerializeField] private RectTransform selectionBoxVisual; 
    [SerializeField] private float dragThreshold = 0.1f; // Minimum drag distance to trigger box selection
    private Vector2 dragStartPos; // Starting position of the selection box
    private bool isSelecting;

    // MOVING NOTES
    private int cursorStartTime; // The cursor time position when the user started moving selected notes
    private bool isMoving;
    private bool singleLane;
    private bool moved;

    // COPYING AND PASTING NOTES
    private List<NoteData> clipboardNotesData;
    private float clipboardBeatPivot; // The beat on the screen that serves as a referece pivot when copying and pasting groups in different beats

    // NOTE LISTS
    private SongData songData;
    private List<Note> selectedNotes;
    private List<Note> activeNotes;

    // COMMAND HISTORY
    private CommandHistory history;


    private void Awake()
    {
        instance = this;
        history = new CommandHistory();
    }

    private void Start()
    {
        clipboardNotesData = new List<NoteData>();
        selectedNotes = new List<Note>();
        activeNotes = new List<Note>();
        SpawnDifficultyNotes(Difficulty.Normal);
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
        
        switch (editMode) {
            case EditMode.Select:
                HandleSelectionInput();
                break;
            case EditMode.Object:
                HandleObjectInput();
                break;
            default:
                break;
        }

        if (Input.GetKeyDown(KeyCode.Z) && !IsDragging()) {
            UndoAction();
        }

        if (Input.GetKeyDown(KeyCode.X) && !IsDragging()) {
            history.RedoCommand();
        }

        foreach (Note note in activeNotes) {
            UpdateNotePosition(note);
        }
        if (isMoving) {
            UpdateMovingSelectionPosition();
        }
    }


    // -----------------------------------------------------------------------------------------------------------------------------
    // SELECT MODE
    // -----------------------------------------------------------------------------------------------------------------------------
    private void HandleSelectionInput()
    {
        // Convert mouse screen position to world position
        Vector3 worldPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        float horizontalPosition = worldPosition.x;
        float verticalPosition = worldPosition.y;

        if (!EventSystem.current.IsPointerOverGameObject() || isSelecting) {
            // -------------------- BEGIN DRAG --------------------
            if (Input.GetMouseButtonDown(0)) {
                // Check if clicked on a Note, if not, create a selection box when dragging
                RaycastHit2D hit = Physics2D.Raycast(worldPosition, Vector2.zero);
                if (hit.collider != null && hit.collider is BoxCollider2D) {
                    Note selectedNote = hit.collider.gameObject.GetComponent<Note>();
                    if (!Input.GetKey(KeyCode.LeftShift)) {
                        // Pressing on a note selects and behaves as drag movement
                        if (!selectedNotes.Contains(selectedNote)) {
                            // Reset selection when clicking on a non selected Note (otherwise it moves keeping the selection)
                            ClearSelection();
                            SelectNote(selectedNote);
                        }
                        isMoving = true;
                        singleLane = IsSingleLaneSelection();
                        moved = false;
                        cursorStartTime = GetTimeFromPosition(horizontalPosition);
                    } else {
                        // Pressing shift and click on a note toggles the selection/deselection of a note and behaves as a drag window instead of movement
                        if (!selectedNotes.Contains(selectedNote)) {
                            SelectNote(selectedNote);
                        } else {
                            DeselectNote(selectedNote);
                        }
                        isSelecting = true;
                    }
                    
                } else {
                    // Pressing on empty space creates a drag selection window
                    if (!Input.GetKey(KeyCode.LeftShift)) {
                        ClearSelection();
                    }
                    isSelecting = true;
                }
                dragStartPos = worldPosition;
            }

            // -------------------- DRAGGING --------------------
            if (IsDragging() && Input.GetMouseButton(0)) {
                if (isSelecting) {
                    float dragDistance = Vector2.Distance(dragStartPos, worldPosition);
                    // Only show selection box if dragged beyond threshold
                    if (dragDistance > dragThreshold || selectionBoxVisual.gameObject.activeSelf) {
                        if (!selectionBoxVisual.gameObject.activeSelf)
                            selectionBoxVisual.gameObject.SetActive(true);

                        Vector2 center = (dragStartPos + (Vector2)worldPosition) / 2f;
                        Vector2 size = new Vector2(
                            Mathf.Abs(dragStartPos.x - horizontalPosition),
                            Mathf.Abs(dragStartPos.y - verticalPosition)
                        );

                        selectionBoxVisual.position = center;
                        selectionBoxVisual.sizeDelta = size;
                    }
                }
            }

            // -------------------- END DRAG --------------------
            if (IsDragging() && Input.GetMouseButtonUp(0)) {
                if (isSelecting) {
                    float dragDistance = Vector2.Distance(dragStartPos, worldPosition);

                    // If dragged far enough, do box selection
                    if (dragDistance > dragThreshold) {
                        Vector2 min = new Vector2(
                            Mathf.Min(dragStartPos.x, horizontalPosition),
                            Mathf.Min(dragStartPos.y, verticalPosition)
                        );
                        Vector2 max = new Vector2(
                            Mathf.Max(dragStartPos.x, horizontalPosition),
                            Mathf.Max(dragStartPos.y, verticalPosition)
                        );

                        Collider2D[] hits = Physics2D.OverlapAreaAll(min, max);
                        foreach (Collider2D hit in hits) {
                            Note selectedNote = hit.GetComponent<Note>();
                            if (selectedNote != null && !selectedNotes.Contains(selectedNote)) {
                                SelectNote(selectedNote);
                            }
                        }
                    }

                    selectionBoxVisual.gameObject.SetActive(false);
                    isSelecting = false;
                }
                if (isMoving) {
                    // If it didnt move the selection (not dragging it previously), select the note under the cursor no matter if is selected or not
                    // This allows the user to select a single note in a group of selected notes that the user didnt want to move.
                    if (!moved) {
                        RaycastHit2D hit = Physics2D.Raycast(worldPosition, Vector2.zero);
                        if (hit.collider != null && hit.collider is BoxCollider2D) {
                            Note selectedNote = hit.collider.gameObject.GetComponent<Note>();
                            if (selectedNote != null) {
                                ClearSelection();
                                SelectNote(selectedNote);
                            }
                        }
                    } else {
                        (int moveDistance, bool changedLane) = CalculateMovedDistance(horizontalPosition, verticalPosition);
                        MoveSelection(moveDistance, changedLane);
                    }
                    isMoving = false;
                    moved = false;
                }
            }
        }

        // ----- DELETE SELECTION -----
        if (Input.GetKeyDown(KeyCode.Delete)) {
            if (selectedNotes.Count > 0)
                DeleteSelection();
        }

        // ----- COPY SELECTION -----
        if ((Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl) ||
             Input.GetKey(KeyCode.LeftCommand) || Input.GetKey(KeyCode.RightCommand)) &&
             Input.GetKeyDown(KeyCode.C)) {
            if (selectedNotes.Count > 0)
                CopySelection();
        }

        // ----- PASTE SELECTION -----
        if ((Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl) ||
             Input.GetKey(KeyCode.LeftCommand) || Input.GetKey(KeyCode.RightCommand)) &&
             Input.GetKeyDown(KeyCode.V)) {
            if (clipboardNotesData.Count > 0)
                PasteSelection();
        }
    }

    // -----------------------------------------------------------------------------------------------------------------------------
    // OBJECT MODE
    // -----------------------------------------------------------------------------------------------------------------------------

    private void HandleObjectInput()
    {
        if (!EventSystem.current.IsPointerOverGameObject()) {
            // Convert mouse screen position to world position
            Vector3 worldPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            float horizontalPosition = (worldPosition.x > 0) ? worldPosition.x : 0;
            float verticalPosition = worldPosition.y;

            if (Input.GetKey(KeyCode.LeftControl) || !beatSnapping) {
                float yPos = verticalPosition > 0 ? 1.5f : -1.5f;
                notePreview.transform.position = new Vector3(horizontalPosition, yPos, 0f);
            }
            else {
                float xPos = GetPositionFromBeat(GetClosestBeatSnappingFromPosition(horizontalPosition));
                float yPos = verticalPosition > 0 ? 1.5f : -1.5f;
                notePreview.transform.position = new Vector3(xPos, yPos, 0);
            }

            if (Input.GetMouseButtonDown(0)) {
                int lane = verticalPosition > 0 ? 0 : 1;
                int time = GetTimeFromPosition(notePreview.transform.position.x);
                int timeThreshold = 2; // 2ms

                
                // Check if a note already exists at this time and lane
                bool noteExists = activeNotes.Exists(n =>
                    n.data.time >= time - timeThreshold && n.data.time <= time + timeThreshold && n.data.lane == lane);
                
                if (!noteExists) {
                    NoteData newNoteData = new NoteData(time, lane);
                    CreateNote(newNoteData);
                }
            }
        }
    }

    // ---------------------------------------------------------------------------------------------------------------------------------------------
    // NOTE MANAGING (EXECUTED FROM COMMANDS)
    // ---------------------------------------------------------------------------------------------------------------------------------------------
    public void SpawnNote(NoteData noteData, bool select)
    {
        Note newNote = Instantiate(hitPrefab, transform).GetComponent<Note>();
        newNote.gameObject.GetInstanceID();
        newNote.data = noteData;
        activeNotes.Add(newNote);
        if (select)
            SelectNote(newNote);
    }

    public void DeleteNote(NoteData noteData)
    {
        Note deleteNote = activeNotes.Find(note => note.data.Equals(noteData));

        if (deleteNote != null) {
            // Remove from the list
            activeNotes.Remove(deleteNote);
            selectedNotes.Remove(deleteNote);
            Destroy(deleteNote.gameObject);
        }
        else {
            Debug.LogWarning("Note to delete not found in active notes list");
        }
    }
    public void MoveNote(NoteData noteData, int distance, bool laneSwap)
    {
        Note moveNote = activeNotes.Find(note => note.data.Equals(noteData));

        if (moveNote != null) {
            NoteData movedData = new NoteData(noteData);
            movedData.time = noteData.time + distance;
            if (laneSwap) {
                movedData.lane = movedData.lane == 0 ? 1 : 0;
            }
            moveNote.data = movedData;
        }
        else {
            Debug.LogWarning("Cant seem to find note with matching data");
        }
    }


    // ---------------------------------------------------------------------------------------------------------------------------------------------
    // COMMANDS: ARE STORED IN A HISTORY, ALLOWING US TO UNDO AND REDO THESE ACTIONS
    // ---------------------------------------------------------------------------------------------------------------------------------------------
    private void CreateNote(NoteData note)
    {
        CreateNotesCommand createCommand = new CreateNotesCommand(note);
        history.AddCommand(createCommand);
    }
    
    public void DeleteSelection()
    {
        List<NoteData> selectedNotesData = new List<NoteData>();
        foreach (Note note in selectedNotes) {
            selectedNotesData.Add(note.data);
        }
        DeleteNotesCommand deleteCommand = new DeleteNotesCommand(selectedNotesData);
        history.AddCommand(deleteCommand);
    }

    public void PasteSelection()
    {
        ClearSelection();
        List<NoteData> newNotes = new List<NoteData>();
        foreach (NoteData noteData in clipboardNotesData) {
            float pastePivot = GetClosestBeatSnappingFromTime(Metronome.instance.GetTimelinePosition());
            int pivotTimDiff = Metronome.instance.GetTimeFromBeatInterval(pastePivot - clipboardBeatPivot);
            int newTime = noteData.time + pivotTimDiff;

            NoteData newNoteData = new NoteData(noteData);
            newNoteData.time = newTime;
            newNotes.Add(newNoteData);
        }

        CreateNotesCommand createNotesCommand = new CreateNotesCommand(newNotes, true);
        history.AddCommand(createNotesCommand);
    }

    public void MoveSelection(int distance, bool laneSwap)
    {
        List<NoteData> selectedNotesData = new List<NoteData>();
        foreach(Note note in selectedNotes) {
            selectedNotesData.Add(note.data);
        }
        MoveNotesCommand moveNotesCommand = new MoveNotesCommand(selectedNotesData, distance, laneSwap);
        history.AddCommand(moveNotesCommand);
    }

    public void UndoAction()
    {
        history.UndoCommand();
    }

    public void RedoAction()
    {
        history.RedoCommand();
    }

    // ---------------------------------------------------------------------------------------------------------------------------------------------
    // NOT COMMANDS: THESE ACTIONS DONT NEED TO BE UNDONE
    // ---------------------------------------------------------------------------------------------------------------------------------------------
    
    private void SpawnDifficultyNotes(Difficulty difficulty)
    {
        ClearActiveNotes();
        switch (difficulty) {
            case Difficulty.Easy:
                SpawnActiveNotes(songData.easyNotes);
                break;
            case Difficulty.Normal:
                SpawnActiveNotes(songData.normalNotes);
                break;
            case Difficulty.Hard:
                SpawnActiveNotes(songData.hardNotes);
                break;
            case Difficulty.Rumble:
                SpawnActiveNotes(songData.rumbleNotes);
                break;
        }
        
    }

    private void UpdateNotePosition(Note note)
    {
        float yPos = note.data.lane == 0 ? 1.5f : -1.5f;
        note.transform.position = new Vector3(GetPositionFromTime(note.data.time), yPos, 0f);
    }

    private void UpdateMovingSelectionPosition()
    {
        // Convert mouse screen position to world position
        Vector3 worldPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        float horizontalPosition = worldPosition.x;
        float verticalPosition = worldPosition.y;
        (int moveDistance, bool changedLane) = CalculateMovedDistance(horizontalPosition, verticalPosition);
        moved = changedLane || moveDistance != 0;

        foreach (Note selectedNote in selectedNotes) {
            float yPos;
            if (changedLane) {
                yPos = selectedNote.data.lane == 0 ? -1.5f : 1.5f;
            }
            else {
                yPos = selectedNote.data.lane == 0 ? 1.5f : -1.5f;
            }
            selectedNote.transform.position = new Vector3(GetPositionFromTime(selectedNote.data.time + moveDistance), yPos, 0f);
        }
    }

    private void SelectNote(Note note)
    {
        selectedNotes.Add(note);
        note.GetComponent<SpriteRenderer>().color = Color.green;
    }

    private void DeselectNote(Note note)
    {
        selectedNotes.Remove(note);
        note.GetComponent<SpriteRenderer>().color = Color.white;
    }

    public void CopySelection()
    {
        clipboardNotesData = new List<NoteData>();
        foreach (Note note in selectedNotes) {
            clipboardNotesData.Add(note.data);
        }
        clipboardBeatPivot = GetClosestBeatSnappingFromTime(Metronome.instance.GetTimelinePosition());
        Debug.Log("Copy pivot: " + clipboardBeatPivot);
    }

    public void ClearSelection()
    {
        foreach (Note note in selectedNotes) {
            note.GetComponent<SpriteRenderer>().color = Color.white;
        }
        selectedNotes.Clear();
    }

    public void ClearActiveNotes()
    {
        foreach (Note note in activeNotes) {
            note.gameObject.Destroy();
        }
        activeNotes.Clear();
    }

    public void SpawnActiveNotes(List<NoteData> notesData)
    {
        foreach (NoteData noteData in notesData) {
            Note newNote = Instantiate(hitPrefab, transform).GetComponent<Note>();
            newNote.data = noteData;
            UpdateNotePosition(newNote);
            activeNotes.Add(newNote);
        }
    }

    public bool IsDragging()
    {
        return isMoving || isSelecting;
    }

    public bool IsSingleLaneSelection()
    {
        // Determine if is a single lane selection
        int previousLane = -1;
        foreach (Note note in selectedNotes) {
            if (previousLane != note.data.lane) {
                if (previousLane == -1) {
                    previousLane = note.data.lane;
                }
                else {
                    return false;
                }
            }
        }
        return true;
    }

    public (int, bool) CalculateMovedDistance(float horizontalPosition, float verticalPosition)
    {
        int moveDistance;
        if (Input.GetKey(KeyCode.LeftControl) || !beatSnapping) {
            moveDistance = GetTimeFromPosition(horizontalPosition) - cursorStartTime;
        }
        else {
            moveDistance = Metronome.instance.GetTimeFromBeat(GetClosestBeatSnappingFromPosition(horizontalPosition))
                - Metronome.instance.GetTimeFromBeat(GetClosestBeatSnappingFromTime(cursorStartTime));
        }
        bool changedLane = singleLane && (verticalPosition > 0 != dragStartPos.y > 0);
        return (moveDistance, changedLane);
    }

    // ---------------------------------------------------------------------------------------------------------------------------------------------
    // EDITOR PARAMETER SETTERS
    // ---------------------------------------------------------------------------------------------------------------------------------------------
    public void SetSpeed(float speed)
    {
        noteSpeed = speed;
    }

    public void SetEditMode(string modeName)
    {
        if (System.Enum.TryParse(modeName, out EditMode mode)) {
            editMode = mode;

            if (mode != EditMode.Select) {
                selectionBoxVisual.gameObject.SetActive(false);
                ClearSelection();
            }

            notePreview.gameObject.SetActive(mode == EditMode.Object);
        }
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

    public float GetBeatFromPosition(float horizontalPosition)
    {
        // Add to current timeline beat position
        return Metronome.instance.GetTimelineBeatPosition() + horizontalPosition / noteSpeed / Metronome.instance.beatSecondInterval;
    }

    public float GetPositionFromBeat(float beat)
    {
        return GetPositionFromTime(Metronome.instance.GetTimeFromBeat(beat));
    }

    public float GetClosestBeatSnappingFromPosition(float horizontalPosition)
    {
        return Mathf.Round(GetBeatFromPosition(horizontalPosition) * noteSubdivisionSnapping) / noteSubdivisionSnapping;
    }

    public float GetClosestBeatSnappingFromTime(int time)
    {
        return Mathf.Round(Metronome.instance.GetBeatFromTime(time) * noteSubdivisionSnapping) / noteSubdivisionSnapping;
    }
}
