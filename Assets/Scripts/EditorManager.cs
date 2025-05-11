using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;

public enum EditMode
{
    Object,
    Select,
    BPMMarker
}
public enum Difficulty
{
    Easy,
    Normal,
    Hard,
    Rumble
}

public class EditorManager : MonoBehaviour
{
    public static EditorManager instance { get; private set; }
    public float noteSpeed = 5f;
    public bool beatSnapping = true;
    public int noteSubdivisionSnapping = 1;
    public EditMode editMode = EditMode.Select;
    public Difficulty difficulty = Difficulty.Normal;
    [SerializeField] private GameObject songDataPanel;

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
    [SerializeField] private GameObject markerPrefab;
    private NoteType currentNoteType;

    [SerializeField] private GameObject notePreview;
    [SerializeField] private GameObject markerPreview;

    // SELECTION BOX
    [SerializeField] private RectTransform selectionBoxVisual; 
    [SerializeField] private float dragThreshold = 0.1f; // Minimum drag distance to trigger box selection
    private Vector2 dragStartPos; // Starting position of the selection box
    private bool isSelecting;

    // MOVING NOTES
    private int cursorStartTime; // The cursor time position when the user started moving selected notes
    private bool isMoving;
    private bool isMarker;
    private bool singleLane;
    private bool moved;

    // NAVIGATION
    private int navigationStartTime; // The cursor time position when the user started navigating the timeline
    private float navigationStartPos; // The cursor time position when the user started navigating the timeline
    private bool isNavigating;

    // COPYING AND PASTING NOTES
    private List<NoteData> clipboardNotesData;
    private float clipboardBeatPivot; // The beat on the screen that serves as a referece pivot when copying and pasting groups in different beats

    // NOTE LISTS
    private SongData songData;
    private List<NoteHandle> selectedHandles;
    private List<Note> selectedNotes;
    private List<Note> activeNotes;
    private BPMMarker selectedMarker;
    private List<BPMMarker> activeMarkers;

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
        activeMarkers = new List<BPMMarker>();

        if (GameManager.instance.IsSongSelected()) {
            LoadSelectedSong(GameManager.instance.GetSelectedSong());
        }
        if (songData == null) {
            CreateSong();
            EditorUI.instance.OpenPanel(songDataPanel);
        }

        EditorUI.instance.DisplaySongData(songData.metadata);

        //  SPAWN INITAL ELEMENTS
        List<NoteData> notesData = GetDifficultyNoteData(Difficulty.Normal);
        foreach (NoteData noteData in notesData) {
            SpawnNote(noteData, false);
        }
        foreach (BPMFlag flag in songData.BPMFlags) {
            SpawnMarker(flag);
        }
    }

    private void Update()
    {
        HandleTestingInput();
        if (EditorUI.instance.isHidden)
        {
            foreach (Note note in activeNotes) {
                UpdateNotePosition(note);
            }
            foreach (BPMMarker marker in activeMarkers) {
                UpdateMarkerPosition(marker);
            }
            return;
        }

        HandleNavigationInput();

        switch (editMode) {
            case EditMode.Select:
                HandleSelectionInput();
                break;
            case EditMode.Object:
                HandleObjectInput();
                break;
            case EditMode.BPMMarker:
                HandleBPMInput();
                break;
            default:
                break;
        }


        if (Input.GetKeyDown(KeyCode.Z) && !IsDragging()) {
            UndoAction();
        }

        if (Input.GetKeyDown(KeyCode.X) && !IsDragging()) {
            RedoAction();
        }


        foreach (Note note in activeNotes) {
            UpdateNotePosition(note);
        }
        foreach (BPMMarker marker in activeMarkers) {
            UpdateMarkerPosition(marker);
        }
        if (isMoving) {
            UpdateMovingSelectionPosition();
        }
    }

    public void CreateSong()
    {
        songData = new SongData();
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

    public void SetSongName(string name)
    {
        songData.metadata.songName = name;
        EditorUI.instance.DisplaySongData(songData.metadata);
    }

    public void SetSongArtist(string artist)
    {
        songData.metadata.artist = artist;
        EditorUI.instance.DisplaySongData(songData.metadata);
    }

    public IEnumerator SaveAndLoadCustomAudioFile(string filePath)
    {
        // Save
        Metronome.instance.ReleaseCustomSong();
        SaveData.CreateAudioFile(songData, filePath);

        // Load
        Metronome.instance.SetCustomSong(SaveData.GetAudioFilePath(songData.metadata.audioFileName));
        EditorUI.instance.DisplaySongData(songData.metadata);
        EditorUI.instance.ApplyWaveformTexture();

        yield break;
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

    public void SaveDifficultyNoteData(Difficulty difficulty)
    {
        List<NoteData> notesData = new List<NoteData>();
        foreach (Note note in activeNotes) {
            notesData.Add(note.data);
        }

        switch (difficulty) {
            case Difficulty.Easy:
                songData.easyNotes = notesData;
                break;
            case Difficulty.Normal:
                songData.normalNotes = notesData;
                break;
            case Difficulty.Hard:
                songData.hardNotes = notesData;
                break;
            case Difficulty.Rumble:
                songData.rumbleNotes = notesData;
                break;
        }
    }

    public void SaveBPMFlags()
    {
        List<BPMFlag> flags = new List<BPMFlag>();

        foreach (BPMMarker marker in activeMarkers) {
            flags.Add(marker.flag);
        }

        songData.BPMFlags = flags;
    }

    // ---------------------------------------------------------------------------------------------------------------------------------------------
    // TESTING
    // ---------------------------------------------------------------------------------------------------------------------------------------------
    private void HandleTestingInput()
    {
        if ((EditorUI.instance.isHidden && Input.GetKeyDown(KeyCode.Escape)) || Input.GetKeyDown(KeyCode.T)) {
            EditorUI.instance.ToggleEditorPanels();
            GameManager.instance.SetNotes(activeNotes);
        }
    }

    public void ReactivateNotes()
    {
        foreach (Note note in activeNotes) {
            note.gameObject.SetActive(true);
        }
    }

    // ---------------------------------------------------------------------------------------------------------------------------------------------
    // NAVIGATION
    // ---------------------------------------------------------------------------------------------------------------------------------------------
    private void HandleNavigationInput()
    {
        if (EditorUI.instance.isPanelOpened) return;

        float scroll = Input.GetAxis("Mouse ScrollWheel");

        if (scroll != 0f && !isNavigating) {
            Metronome.instance.SetTimelinePosition((int)(Metronome.instance.GetTimelinePosition() + scroll * 1000));
        }

        if (Input.GetKeyDown(KeyCode.Space)) {
            if (Metronome.instance.IsPaused()) {
                Metronome.instance.PlaySong();
            }
            else {
                Metronome.instance.PauseSong();
            }
        }

        if (EventSystem.current.IsPointerOverGameObject() && !isNavigating)
            return;

        Vector3 worldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);

        if (Input.GetMouseButtonDown(1) || Input.GetMouseButtonDown(2)) {
            isNavigating = true;
            navigationStartPos = worldPos.x;
            navigationStartTime = Metronome.instance.GetTimelinePosition();
            Metronome.instance.PauseSong();
        }

        if ((Input.GetMouseButton(1) || Input.GetMouseButton(2)) && isNavigating) {
            float currentWorldX = worldPos.x;
            float worldDelta = currentWorldX - navigationStartPos;

            // Convert world delta to time delta — you define how many units of world = how many milliseconds
            int timeDelta = (int)(worldDelta * 1000 / noteSpeed);
            Metronome.instance.SetTimelinePosition(navigationStartTime - timeDelta);
        }

        if (Input.GetMouseButtonUp(1) || Input.GetMouseButtonUp(2)) {
            isNavigating = false;
        }
    }

    // ---------------------------------------------------------------------------------------------------------------------------------------------
    // SELECT MODE
    // ---------------------------------------------------------------------------------------------------------------------------------------------
    private void HandleSelectionInput()
    {
        if (EventSystem.current.IsPointerOverGameObject() && !isSelecting)
            return;

        Vector3 worldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);

        HandleMouseDown(worldPos);
        HandleMouseDrag(worldPos);
        HandleMouseUp(worldPos);
        HandleKeyboardShortcuts();
    }

    private void HandleMouseDown(Vector3 worldPos)
    {
        if (!Input.GetMouseButtonDown(0)) return;

        RaycastHit2D hit = Physics2D.Raycast(worldPos, Vector2.zero);
        dragStartPos = worldPos;

        if (hit.collider != null) {
            TimelineElement element = hit.collider.GetComponent<TimelineElement>();
            if (element == null) {
                Debug.Log("Clicked on non timeline element");
                return;
            }

            if (element is Note) {
                HandleNoteClick(element as Note, worldPos.x);
                isMarker = false;
            }
            else if (element is NoteHandle) {

            }
            else if (element is BPMMarker) {
                Select(element as BPMMarker);
                StartMoving(worldPos.x);
            }
        }
        else {
            if (!Input.GetKey(KeyCode.LeftShift)) ClearSelection();
            isSelecting = true;
            isMarker = false;
        }
    }

    private void HandleNoteClick(Note note, float clickPosition)
    {
        if (!Input.GetKey(KeyCode.LeftShift)) {
            if (!selectedNotes.Contains(note)) {
                ClearSelection();
                Select(note);
            }
            StartMoving(clickPosition);
        }
        else {
            if (!selectedNotes.Contains(note))
                Select(note);
            else
                Deselect(note);

            isSelecting = true;
        }
    }

    private void StartMoving(float startPosition)
    {
        isMoving = true;
        singleLane = IsSingleLaneSelection();
        moved = false;
        cursorStartTime = GetTimeFromPosition(startPosition);
    }

    private void HandleMouseDrag(Vector3 worldPos)
    {
        if (!IsDragging() || !Input.GetMouseButton(0) || !isSelecting)
            return;

        float dragDist = Vector2.Distance(dragStartPos, worldPos);

        if (dragDist > dragThreshold || selectionBoxVisual.gameObject.activeSelf) {
            if (!selectionBoxVisual.gameObject.activeSelf)
                selectionBoxVisual.gameObject.SetActive(true);

            Vector2 center = (dragStartPos + (Vector2)worldPos) / 2f;
            Vector2 size = new Vector2(Mathf.Abs(dragStartPos.x - worldPos.x), Mathf.Abs(dragStartPos.y - worldPos.y));

            selectionBoxVisual.position = center;
            selectionBoxVisual.sizeDelta = size;
        }
    }

    private void HandleMouseUp(Vector3 worldPos)
    {
        if (!IsDragging() || !Input.GetMouseButtonUp(0))
            return;

        if (isSelecting) {
            PerformBoxSelection(worldPos);
        }

        if (isMoving) {
            if (!moved && !isMarker) TrySelectSingleUnderCursor(worldPos);
            else {
                (int moveDist, bool changedLane) = CalculateMovedDistance(worldPos.x, worldPos.y);
                if (isMarker) 
                {
                    MoveSelectedMarker(moveDist);
                } else {
                    MoveSelection(moveDist, changedLane);
                }
            }

            isMoving = false;
            moved = false;
        }
    }

    private void PerformBoxSelection(Vector3 worldPos)
    {
        float dragDist = Vector2.Distance(dragStartPos, worldPos);

        if (dragDist > dragThreshold) {
            Vector2 min = new Vector2(Mathf.Min(dragStartPos.x, worldPos.x), Mathf.Min(dragStartPos.y, worldPos.y));
            Vector2 max = new Vector2(Mathf.Max(dragStartPos.x, worldPos.x), Mathf.Max(dragStartPos.y, worldPos.y));

            foreach (var hit in Physics2D.OverlapAreaAll(min, max)) {
                Note note = hit.GetComponent<Note>();
                if (note != null && !selectedNotes.Contains(note))
                    Select(note);
            }
        }

        selectionBoxVisual.gameObject.SetActive(false);
        isSelecting = false;
    }

    private void TrySelectSingleUnderCursor(Vector3 worldPos)
    {
        RaycastHit2D hit = Physics2D.Raycast(worldPos, Vector2.zero);
        if (hit.collider != null && hit.collider is BoxCollider2D) {
            Note note = hit.collider.GetComponent<Note>();
            if (note != null) {
                ClearSelection();
                Select(note);
            }
        }
    }

    private void HandleKeyboardShortcuts()
    {
        if (Input.GetKeyDown(KeyCode.Delete)) {
            if (isMarker) {
                Debug.Log("attempting to delete marker");
                DeleteSelectedMarker();
            } else {
                Debug.Log("attempting to delete selection");
                DeleteSelection();
            }
        }
            

        if ((Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl) ||
             Input.GetKey(KeyCode.LeftCommand) || Input.GetKey(KeyCode.RightCommand)) &&
            Input.GetKeyDown(KeyCode.C)) {
            CopySelection();
        }

        if ((Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl) ||
             Input.GetKey(KeyCode.LeftCommand) || Input.GetKey(KeyCode.RightCommand)) &&
            Input.GetKeyDown(KeyCode.V)) {
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
                    NoteData newNoteData = new NoteData(time, lane, currentNoteType);
                    CreateNote(newNoteData);
                }
            }
        }
    }

    // -----------------------------------------------------------------------------------------------------------------------------
    // BPM MARKER MODE
    // -----------------------------------------------------------------------------------------------------------------------------
    private void HandleBPMInput()
    {
        if (!EventSystem.current.IsPointerOverGameObject()) {
            // Convert mouse screen position to world position
            Vector3 worldPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            float horizontalPosition = (worldPosition.x > 0) ? worldPosition.x : 0;
            float verticalPosition = worldPosition.y;

            if (Input.GetKey(KeyCode.LeftShift) && beatSnapping) {
                float xPos = GetPositionFromBeat(GetClosestBeatSnappingFromPosition(horizontalPosition));
                markerPreview.transform.position = new Vector3(xPos, 3.3f, 0);
            }
            else {
                markerPreview.transform.position = new Vector3(horizontalPosition, 3.3f, 0f);
            }

            if (Input.GetMouseButtonDown(0)) {
                int time = GetTimeFromPosition(markerPreview.transform.position.x);
                int timeThreshold = 2; // 2ms

                // Check if a marker already exists at this time and lane
                bool markerExists = activeMarkers.Exists(n =>
                    n.flag.offset >= time - timeThreshold && n.flag.offset <= time + timeThreshold);

                if (!markerExists) {
                    BPMFlag bpmFlag = new BPMFlag(time);
                    CreateMarker(bpmFlag);
                }
            }
        }
    }

    // ---------------------------------------------------------------------------------------------------------------------------------------------
    // NOTE MANAGING (EXECUTED FROM COMMANDS)
    // ---------------------------------------------------------------------------------------------------------------------------------------------
    public void SpawnNote(NoteData noteData, bool select)
    {
        GameObject prefab = GetNoteTypePrefab(noteData.type);
        Note newNote = Instantiate(prefab, transform).GetComponent<Note>();
        newNote.data = noteData;
        activeNotes.Add(newNote);
        activeNotes = activeNotes.OrderBy(note => note.data.time).ThenBy(note => note.data.lane).ToList();
        if (select)
            Select(newNote);
    }

    public void SpawnMarker(BPMFlag flag)
    {
        BPMMarker newMarker = Instantiate(markerPrefab, transform).GetComponent<BPMMarker>();
        newMarker.flag = flag;
        newMarker.UpdateDisplay(flag.BPM);
        activeMarkers.Add(newMarker);
        activeMarkers = activeMarkers.OrderBy(marker => marker.flag.offset).ToList();

        UpdateMetronomeFlags();
    }

    public void EditMarker(BPMFlag flag, float BPM)
    {
        BPMMarker editMarker = FindMarkerBinary(flag);

        if (editMarker != null) {
            editMarker.flag.BPM = BPM;
            editMarker.UpdateDisplay(BPM);
        }
        else {
            Debug.LogWarning("Marker to edit not found in active markers list");
        }

        UpdateMetronomeFlags();
    }

    public void DeleteNote(NoteData noteData)
    {
        Note deleteNote = FindNoteBinary(noteData);

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

    public void DeleteMarker(BPMFlag flag)
    {
        BPMMarker deleteMarker = FindMarkerBinary(flag);

        if (deleteMarker != null) {
            // Remove from the list
            activeMarkers.Remove(deleteMarker);
            selectedMarker = null;
            Destroy(deleteMarker.gameObject);
        }
        else {
            Debug.LogWarning("Marker to delete not found in active markers list");
        }

        UpdateMetronomeFlags();
    }

    public void MoveNote(NoteData noteData, int distance, bool laneSwap)
    {
        Note moveNote = FindNoteBinary(noteData);

        if (moveNote != null) {
            NoteData movedData = new NoteData(noteData);
            movedData.time = noteData.time + distance;
            if (laneSwap) {
                movedData.lane = movedData.lane == 0 ? 1 : 0;
            }
            moveNote.data = movedData;
            activeNotes = activeNotes.OrderBy(note => note.data.time).ThenBy(note => note.data.lane).ToList();
        }
        else {
            Debug.LogWarning("Cant seem to find note with matching data");
        }
    }

    public void MoveMarker(BPMFlag flag, int distance)
    {
        BPMMarker moveMarker = FindMarkerBinary(flag);

        if (moveMarker != null) {
            BPMFlag movedFlag = new BPMFlag(flag);
            movedFlag.offset = movedFlag.offset + distance;
            moveMarker.flag = movedFlag;
            activeMarkers = activeMarkers.OrderBy(marker => marker.flag.offset).ToList();
        }
        else {
            Debug.LogWarning("Cant seem to find marker with matching data");
        }

        UpdateMetronomeFlags();
    }


    // ---------------------------------------------------------------------------------------------------------------------------------------------
    // COMMANDS: ARE STORED IN A HISTORY, ALLOWING US TO UNDO AND REDO THESE ACTIONS
    // ---------------------------------------------------------------------------------------------------------------------------------------------
    private void CreateNote(NoteData note)
    {
        CreateNotesCommand createCommand = new CreateNotesCommand(note);
        history.AddCommand(createCommand);
        Debug.Log("created note");
    }

    private void CreateMarker(BPMFlag flag)
    {
        CreateMarkerCommand createCommand = new CreateMarkerCommand(flag);
        history.AddCommand(createCommand);
        Debug.Log("created marker");
    }

    public void EditSelectedMarker(float bpm)
    {
        if (selectedMarker == null) return;
        EditMarkerCommand editCommand = new EditMarkerCommand(selectedMarker.flag, bpm);
        history.AddCommand(editCommand);
        Debug.Log("edited marker");

    }

    public void DeleteSelection()
    {
        if (selectedNotes.Count == 0) return;

        List<NoteData> selectedNotesData = new List<NoteData>();
        foreach (Note note in selectedNotes) {
            selectedNotesData.Add(note.data);
        }
        DeleteNotesCommand deleteCommand = new DeleteNotesCommand(selectedNotesData);
        history.AddCommand(deleteCommand);
        Debug.Log("deleted notes");
    }

    private void DeleteSelectedMarker()
    {
        if (selectedMarker != null) {
            DeleteMarkerCommand deleteCommand = new DeleteMarkerCommand(selectedMarker.flag);
            history.AddCommand(deleteCommand);
            Debug.Log("deleted marker");
        }
    }

    public void PasteSelection()
    {
        //if (clipboardNotesData.Count > 0) return;

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
        Debug.Log("crated notes");
    }

    public void MoveSelection(int distance, bool laneSwap)
    {
        List<NoteData> selectedNotesData = new List<NoteData>();
        foreach(Note note in selectedNotes) {
            selectedNotesData.Add(note.data);
        }
        MoveNotesCommand moveNotesCommand = new MoveNotesCommand(selectedNotesData, distance, laneSwap);
        history.AddCommand(moveNotesCommand);
        Debug.Log("moved notes");
    }

    public void MoveSelectedMarker(int distance)
    {
        MoveMarkerCommand moveCommand = new MoveMarkerCommand(selectedMarker.flag, distance);
        history.AddCommand(moveCommand);
        Debug.Log("moved marker");
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
    
    public void SpawnDifficultyNotes(int diff)
    {
        SaveDifficultyNoteData(difficulty);
        ClearActiveNotes();
        difficulty = (Difficulty)diff;

        List<NoteData> notesData = GetDifficultyNoteData((Difficulty)diff);
        foreach (NoteData noteData in notesData) {
            SpawnNote(noteData, false);
        }
    }

    private void UpdateNotePosition(Note note)
    {
        float yPos = note.data.lane == 0 ? 1.5f : -1.5f;
        note.transform.position = new Vector3(GetPositionFromTime(note.data.time), yPos, 0f);
    }

    private void UpdateMarkerPosition(BPMMarker marker)
    {
        marker.transform.position = new Vector3(GetPositionFromTime(marker.flag.offset), 3.3f, 0f);
    }

    private void UpdateMovingSelectionPosition()
    {
        // Convert mouse screen position to world position
        Vector3 worldPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        float horizontalPosition = worldPosition.x;
        float verticalPosition = worldPosition.y;
        (int moveDistance, bool changedLane) = CalculateMovedDistance(horizontalPosition, verticalPosition);
        moved = changedLane || moveDistance != 0;

        if (isMarker) {
            selectedMarker.transform.position = new Vector3(GetPositionFromTime(selectedMarker.flag.offset + moveDistance), 3.3f, 0f);
        } else {
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
    }

    public void Select(TimelineElement element)
    {
        switch(element) {
            case Note note:
                selectedNotes.Add(note);
                note.GetComponent<SpriteRenderer>().color = Color.green;
                break;
            case BPMMarker marker:
                ClearSelection();
                selectedMarker = marker;
                selectedMarker.Highlight(true);
                isMarker = true;
                break;
            case NoteHandle handle:
                selectedHandles.Add(handle);
                handle.GetComponent<SpriteRenderer>().color = Color.green;
                break;
        }
    }

    private void Deselect(TimelineElement element)
    {
        switch (element) {
            case Note note:
                selectedNotes.Remove(note);
                note.GetComponent<SpriteRenderer>().color = Color.white;
                break;
            case BPMMarker marker:
                if (selectedMarker != null) {
                    selectedMarker.Highlight(false);
                    selectedMarker = null;
                }
                break;
            case NoteHandle handle:
                selectedHandles.Add(handle);
                handle.GetComponent<SpriteRenderer>().color = Color.green;
                break;
        }
    }

    public void CopySelection()
    {
        if (selectedNotes.Count == 0) return;
        if (isMarker) return;

        clipboardNotesData = new List<NoteData>();
        foreach (Note note in selectedNotes) {
            clipboardNotesData.Add(note.data);
        }
        clipboardBeatPivot = GetClosestBeatSnappingFromTime(Metronome.instance.GetTimelinePosition());
        Debug.Log("Copy pivot: " + clipboardBeatPivot);
    }

    public void ClearSelection()
    {
        Deselect(selectedMarker);
        foreach (Note note in selectedNotes) {
            note.GetComponent<SpriteRenderer>().color = Color.white;
        }
        selectedNotes.Clear();
    }

    public void ClearActiveNotes()
    {
        history.Clear();
        foreach (Note note in activeNotes) {
            Destroy(note.gameObject);
        }
        activeNotes.Clear();
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
        if ((!isMarker && Input.GetKey(KeyCode.LeftControl)) || (isMarker && !Input.GetKey(KeyCode.LeftShift)) || !beatSnapping) {
            moveDistance = GetTimeFromPosition(horizontalPosition) - cursorStartTime;
        }
        else {
            moveDistance = Metronome.instance.GetTimeFromBeat(GetClosestBeatSnappingFromPosition(horizontalPosition))
                - Metronome.instance.GetTimeFromBeat(GetClosestBeatSnappingFromTime(cursorStartTime));
        }
        bool changedLane = singleLane && (verticalPosition > 0 != dragStartPos.y > 0);
        return (moveDistance, changedLane);
    }

    public void UpdateMetronomeFlags()
    {
        List<BPMFlag> flags = new List<BPMFlag>();

        foreach (BPMMarker marker in activeMarkers) {
            flags.Add(marker.flag);
        }

        Metronome.instance.SetBPMFlags(flags);
    }

    public void SaveChanges()
    {
        SaveBPMFlags();
        SaveDifficultyNoteData(difficulty);
        SaveData.SaveCustomSongData(songData);
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
            markerPreview.gameObject.SetActive(mode == EditMode.BPMMarker);
        }
    }

    public void SetNoteType(string type)
    {
        if (System.Enum.TryParse(type, out NoteType noteType)) {
            currentNoteType = noteType;
            notePreview.GetComponent<SpriteRenderer>().sprite = GetNoteTypePrefab(noteType).GetComponent<SpriteRenderer>().sprite;
        }
    }

    private GameObject GetNoteTypePrefab(NoteType type)
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

    // SORTERS
    private Note FindNoteBinary(NoteData noteData)
    {
        int low = 0;
        int high = activeNotes.Count - 1;

        while (low <= high) {
            int mid = (low + high) / 2;
            Note midNote = activeNotes[mid];

            int cmp = noteData.time.CompareTo(midNote.data.time);
            if (cmp == 0)
                cmp = noteData.lane.CompareTo(midNote.data.lane);

            if (cmp == 0)
                return midNote;
            else if (cmp < 0)
                high = mid - 1;
            else
                low = mid + 1;
        }

        return null; // Not found
    }

    private BPMMarker FindMarkerBinary(BPMFlag flag)
    {
        int low = 0;
        int high = activeMarkers.Count - 1;

        while (low <= high) {
            int mid = (low + high) / 2;
            BPMMarker midMarker = activeMarkers[mid];

            int cmp = flag.offset.CompareTo(midMarker.flag.offset);
            if (cmp == 0)
                return midMarker;
            else if (cmp < 0)
                high = mid - 1;
            else
                low = mid + 1;
        }

        return null; // Not found
    }
}
