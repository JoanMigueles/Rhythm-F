using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;

public enum EditMode
{
    Object,
    Select,
    BPMMarker
}

public class EditorManager : NoteManager
{
    public bool beatSnapping = true;
    public int noteSubdivisionSnapping = 1;
    public EditMode editMode = EditMode.Select;
    [SerializeField] private GameObject songDataPanel;

    // SELECTED NOTE TYPE TO PLACE
    [SerializeField] private GameObject markerPrefab;
    [SerializeField] private GameObject notePreview;
    [SerializeField] private GameObject markerPreview;
    private SliderNote sliderPreview;
    private NoteType currentNoteType;
    private bool isSlider;
    private bool isBig;

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

    // NAVIGATION
    private int navigationStartTime; // The cursor time position when the user started navigating the timeline
    private float navigationStartPos; // The cursor time position when the user started navigating the timeline
    private bool isNavigating;

    // COPYING AND PASTING NOTES
    private List<NoteData> clipboardNotesData;
    private float clipboardBeatPivot; // The beat on the screen that serves as a referece pivot when copying and pasting groups in different beats

    // NOTE LISTS
    private List<BPMMarker> activeMarkers;
    private List<TimelineElement> selectedElements;

    // COMMAND HISTORY
    private CommandHistory history;

    protected override void Awake()
    {
        base.Awake();
        history = new CommandHistory();
    }

    protected override void Start()
    {
        clipboardNotesData = new List<NoteData>();
        selectedElements = new List<TimelineElement>();
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
        SpawnDifficultyNotes(Difficulty.Normal);
        foreach (BPMFlag flag in songData.BPMFlags) {
            SpawnMarker(flag);
        }
    }

    private void Update()
    {
        HandleTestingInput();
        if (EditorUI.instance.isHidden)
        {
            UpdateNotesPosition();
            UpdateMarkersPosition();
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

        if (Input.GetKeyDown(KeyCode.Z) && Input.GetKey(KeyCode.LeftControl) && !Input.GetKey(KeyCode.LeftAlt) && !IsDragging()) {
            UndoAction();
        }

        if (Input.GetKeyDown(KeyCode.Z) && Input.GetKey(KeyCode.LeftControl) && Input.GetKey(KeyCode.LeftAlt) && !IsDragging()) {
            RedoAction();
        }

        UpdateNotesPosition();
        UpdateMarkersPosition();
        if (isMoving) {
            UpdateMovingSelectionPosition();
        }
    }

    public void CreateSong()
    {
        songData = new SongData();
    }

    protected override void LoadSelectedSong(string songFilePath)
    {
        base.LoadSelectedSong(songFilePath);
        EditorUI.instance.ApplyWaveformTexture();
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
        Metronome.instance.ReleaseCustomPlayer();
        SaveData.CreateAudioFile(songData, filePath);

        // Load
        Metronome.instance.SetCustomSong(SaveData.GetAudioFilePath(songData.metadata.audioFileName));
        EditorUI.instance.DisplaySongData(songData.metadata);
        EditorUI.instance.ApplyWaveformTexture();

        yield break;
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
        if (EditorUI.instance.isPanelOpened) return;
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
            if (element == null)  return;
            HandleElementClick(element, worldPos.x);
        }
        else {
            if (!Input.GetKey(KeyCode.LeftShift)) ClearSelection();
            isSelecting = true;
        }
    }

    private void HandleElementClick(TimelineElement element, float clickPosition)
    {
        if (element is BPMMarker marker) {
            Select(marker);
            StartMoving(clickPosition);
            return;
        }

        if (!Input.GetKey(KeyCode.LeftShift)) {
            if (!element.isSelected) {
                ClearSelection();
                Select(element);
            }
            StartMoving(clickPosition);
        }
        else {
            if (!element.isSelected)
                Select(element);
            else
                Deselect(element);

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
            if (!moved) {
                TrySelectSingleUnderCursor(worldPos);
            }
            else {
                (int moveDist, bool changedLane) = CalculateMovedDistance(worldPos.x, worldPos.y);
                MoveSelection(moveDist, changedLane);
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

            Collider2D[] colliders = Physics2D.OverlapAreaAll(min, max);
            foreach (Collider2D hit in colliders) {
                TimelineElement element = hit.GetComponent<TimelineElement>();
                if (element != null && !element.isSelected && element is not BPMMarker)
                    Select(element);
            }
        }

        selectionBoxVisual.gameObject.SetActive(false);
        isSelecting = false;
    }

    private void TrySelectSingleUnderCursor(Vector3 worldPos)
    {
        RaycastHit2D hit = Physics2D.Raycast(worldPos, Vector2.zero);
        if (hit.collider != null && hit.collider is BoxCollider2D) {
            TimelineElement element = hit.collider.GetComponent<TimelineElement>();
            if (element != null) {
                ClearSelection();
                Select(element);
            }
        }
    }

    private void HandleKeyboardShortcuts()
    {
        if (Input.GetKeyDown(KeyCode.Delete)) {
            DeleteSelection();
        }

        if ((Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl) ||
             Input.GetKey(KeyCode.LeftCommand) || Input.GetKey(KeyCode.RightCommand)) &&
            Input.GetKeyDown(KeyCode.C)) {
            CopySelection();
        }

        if ((Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl) ||
             Input.GetKey(KeyCode.LeftCommand) || Input.GetKey(KeyCode.RightCommand)) &&
            Input.GetKeyDown(KeyCode.V)) {
            PasteNoteSelection();
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
            
            HandleCreateDown(worldPosition);
            HandleCreateDrag(worldPosition);
            HandleCreateUp(worldPosition);

            UpdateNotePreview(worldPosition);
        }
    }

    private void HandleCreateDown(Vector3 worldPos)
    {
        if (!Input.GetMouseButtonDown(0)) return;

        int time = GetSnappedTime(worldPos.x);
        int lane = worldPos.y > 0 ? 0 : 1;
        int timeThreshold = 2; // 2ms
        // Check if a note already exists at this time and lane
        bool noteExists = activeNotes.Exists(n =>
            n.data.time >= time - timeThreshold && n.data.time <= time + timeThreshold && n.data.lane == lane);

        if (noteExists) return;

        if (isBig) lane = 0;
        NoteData newNoteData = new NoteData(time, lane, currentNoteType);
        if (isSlider) {
            CreateSliderPreview(newNoteData);
        } else {
            CreateNote(newNoteData);
        }

        cursorStartTime = GetTimeFromPosition(worldPos.x);
    }

    private void HandleCreateDrag(Vector3 worldPos)
    {
        if (!Input.GetMouseButton(0) || sliderPreview == null) return;

        (int moveDist, bool changedLane) = CalculateMovedDistance(worldPos.x, worldPos.y);
        sliderPreview.UpdatePosition();
        if (sliderPreview.durationHandle != null) {
            sliderPreview.durationHandle.Move(moveDist, changedLane);
        }
    }

    private void HandleCreateUp(Vector3 worldPos)
    {
        if (!Input.GetMouseButtonUp(0) || sliderPreview == null) return;

        (int moveDist, bool changedLane) = CalculateMovedDistance(worldPos.x, worldPos.y);
        sliderPreview.data.duration = moveDist;
        if (sliderPreview.durationHandle != null) {
            if (moveDist < 0) moveDist = 0;
            sliderPreview.data.duration = moveDist;
        }
        sliderPreview.UpdatePosition();
        CreateNote(sliderPreview.data);
        Destroy(sliderPreview.gameObject);
    }

    public void UpdateNotePreview(Vector3 worldPos)
    {
        float horizontalPosition = (worldPos.x > 0) ? worldPos.x : 0;
        float verticalPosition = worldPos.y;

        float yPos;
        if (isBig) yPos = 0;
        else {
            yPos = verticalPosition > 0 ? 1.5f : -1.5f;
        }

        float xPos = GetSnappedHorizontalPosition(horizontalPosition);
        notePreview.transform.position = new Vector3(xPos, yPos, 0f);
    }

    public void CreateSliderPreview(NoteData noteData)
    {
        GameObject prefab = GetNoteTypePrefab(noteData.type);
        sliderPreview = Instantiate(prefab, transform).GetComponent<SliderNote>();
        sliderPreview.data = noteData;
        sliderPreview.UpdatePosition();
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
        Note newNote = SpawnNote(noteData);
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

    public void EditNote(NoteData noteData, NoteData newData)
    {
        Note editNote = activeNotes.Find(note => note.data.Equals(noteData));

        if (editNote != null) {
            editNote.data = newData;
            activeNotes = activeNotes.OrderBy(note => note.data.time).ThenBy(note => note.data.lane).ToList();
        }
        else {
            Debug.LogWarning("Cant seem to find note with matching data");
        }
    }

    public void EditMarker(BPMFlag flag, BPMFlag newFlag)
    {
        BPMMarker editMarker = activeMarkers.Find(marker => marker.flag.Equals(flag));

        if (editMarker != null) {
            editMarker.flag = newFlag;
            editMarker.UpdateDisplay(newFlag.BPM);
            activeMarkers = activeMarkers.OrderBy(marker => marker.flag.offset).ToList();
        }
        else {
            Debug.LogWarning("Marker to edit not found in active markers list");
        }

        UpdateMetronomeFlags();
    }

    public void DeleteNote(NoteData noteData)
    {
        Note deleteNote = activeNotes.Find(note => note.data.Equals(noteData));

        if (deleteNote != null) {
            // Remove from the list
            activeNotes.Remove(deleteNote);
            selectedElements.Remove(deleteNote);
            if (deleteNote is SliderNote slider && slider.durationHandle != null) {
                selectedElements.Remove(slider.durationHandle);
            }
            Destroy(deleteNote.gameObject);
        }
        else {
            Debug.LogWarning("Note to delete not found in active notes list");
        }
    }

    public void DeleteMarker(BPMFlag flag)
    {
        BPMMarker deleteMarker = activeMarkers.Find(marker => marker.flag.Equals(flag));

        if (deleteMarker != null) {
            // Remove from the list
            activeMarkers.Remove(deleteMarker);
            selectedElements.Remove(deleteMarker);
            Destroy(deleteMarker.gameObject);
        }
        else {
            Debug.LogWarning("Marker to delete not found in active markers list");
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
        if (selectedElements.Count == 0) return;
        if (selectedElements[0] is BPMMarker marker) {
            BPMFlag newFlag = new BPMFlag(marker.flag);
            newFlag.BPM = bpm;
            EditMarkerCommand editCommand = new EditMarkerCommand(marker.flag, newFlag);
            history.AddCommand(editCommand);
            Debug.Log("edited marker");
        }
    }

    public void DeleteSelection()
    {
        if (selectedElements.Count == 0) return;
        if (selectedElements[0] is BPMMarker selectedMarker) {
            DeleteMarkerCommand deleteMarkerCommand = new DeleteMarkerCommand(selectedMarker.flag);
            history.AddCommand(deleteMarkerCommand);
            Debug.Log("deleted marker");
            return;
        }

        List<NoteData> selectedNotesData = new List<NoteData>();
        foreach (TimelineElement element in selectedElements) {
            if (element is Note note) selectedNotesData.Add(note.data);
        }
        if (selectedNotesData.Count == 0) return;

        DeleteNotesCommand deleteCommand = new DeleteNotesCommand(selectedNotesData);
        history.AddCommand(deleteCommand);
        Debug.Log("deleted notes");
    }

    public void PasteNoteSelection()
    {
        if (clipboardNotesData.Count == 0) return;

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
        if (selectedElements.Count == 0) return;

        if (selectedElements[0] is BPMMarker marker) {
            BPMFlag newFlag = new BPMFlag(marker.flag);
            newFlag.offset += distance;
            EditMarkerCommand moveMarkerCommand = new EditMarkerCommand(marker.flag, newFlag);
            history.AddCommand(moveMarkerCommand);
            Debug.Log("moved marker");
            return;
        }

        List<NoteData> selectedNotesData = new List<NoteData>();
        List<NoteData> newNotesData = new List<NoteData>();
        foreach (TimelineElement element in selectedElements) {
            if (element is Note note) {
                selectedNotesData.Add(note.data);
                NoteData newNote = new NoteData(note.data);
                newNote.time += distance;
                if (laneSwap && note is not BigSliderNote) newNote.lane = newNote.lane == 0 ? 1 : 0;
                newNotesData.Add(newNote);
            } else if (element is NoteHandle handle) {
                if (handle.note.isSelected) continue;
                NoteData newNote = new NoteData(handle.note.data);
                newNote.duration += distance;
                if (newNote.duration < 0) newNote.duration = 0;
                if (newNote.duration == handle.note.data.duration) continue;
                selectedNotesData.Add(handle.note.data);
                newNotesData.Add(newNote);
            }
        }

        if (selectedNotesData.Count == 0) return;

        EditNotesCommand moveNotesCommand = new EditNotesCommand(selectedNotesData, newNotesData);
        history.AddCommand(moveNotesCommand);
        Debug.Log("moved notes");
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
    public override void SpawnDifficultyNotes(int diff)
    {
        SaveDifficultyNoteData(difficulty);
        ClearActiveNotes();
        base.SpawnDifficultyNotes(diff);
    }

    private void UpdateMarkersPosition()
    {
        foreach (BPMMarker marker in activeMarkers) {
            marker.UpdatePosition();
        }
    }

    private void UpdateMovingSelectionPosition()
    {
        // Convert mouse screen position to world position
        Vector3 worldPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        float horizontalPosition = worldPosition.x;
        float verticalPosition = worldPosition.y;
        (int moveDistance, bool changedLane) = CalculateMovedDistance(horizontalPosition, verticalPosition);
        moved = changedLane || moveDistance != 0;

        foreach (TimelineElement element in selectedElements) {
            element.Move(moveDistance, changedLane);
        }
    }

    public void Select(TimelineElement element)
    {
        if (element is BPMMarker) ClearSelection();
        selectedElements.Add(element);
        element.SetSelected(true);
    }

    private void Deselect(TimelineElement element)
    {
        selectedElements.Remove(element);
        element.SetSelected(false);
    }

    public void CopySelection()
    {
        if (selectedElements.Count == 0) return;

        clipboardNotesData = new List<NoteData>();

        foreach (TimelineElement element in selectedElements) {
            if (element is Note note) clipboardNotesData.Add(note.data);
        }

        if (clipboardNotesData.Count == 0) return;

        clipboardBeatPivot = GetClosestBeatSnappingFromTime(Metronome.instance.GetTimelinePosition());
        Debug.Log("Copy pivot: " + clipboardBeatPivot);
    }

    public void ClearSelection()
    {
        foreach (TimelineElement element in selectedElements) {
            element.SetSelected(false);
        }
        selectedElements.Clear();
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
        foreach (TimelineElement element in selectedElements) {
            if (element is Note note) {
                if (note is BigSliderNote unique) return false;
                if (previousLane != note.data.lane) {
                    if (previousLane == -1) {
                        previousLane = note.data.lane;
                    }
                    else {
                        return false;
                    }
                }
            }
            else if (element is NoteHandle handle) {
                if (!handle.note.isSelected) return false;
            }
        }

        return true;
    }

    public (int, bool) CalculateMovedDistance(float horizontalPosition, float verticalPosition)
    {
        int moveDistance;
        bool isMarker = false;
        if (selectedElements.Count > 0) {
            isMarker = selectedElements[0] is BPMMarker;
        }

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
            isBig = GetNoteTypePrefab(currentNoteType).GetComponent<BigSliderNote>() != null;
            isSlider = GetNoteTypePrefab(currentNoteType).GetComponent<SliderNote>() != null;
            notePreview.GetComponent<SpriteRenderer>().sprite = GetNoteTypePrefab(noteType).GetComponent<SpriteRenderer>().sprite;
        }
    }

    // ---------------------------------------------------------------------------------------------------------------------------------------------
    // AUDIO TO POSITION
    // ---------------------------------------------------------------------------------------------------------------------------------------------
    public float GetClosestBeatSnappingFromPosition(float horizontalPosition)
    {
        return Mathf.Round(GetBeatFromPosition(horizontalPosition) * noteSubdivisionSnapping) / noteSubdivisionSnapping;
    }

    public float GetClosestBeatSnappingFromTime(int time)
    {
        return Mathf.Round(Metronome.instance.GetBeatFromTime(time) * noteSubdivisionSnapping) / noteSubdivisionSnapping;
    }

    public int GetSnappedTime(float horizontalPosition)
    {
        if (Input.GetKey(KeyCode.LeftControl) || !beatSnapping) {
            return GetTimeFromPosition(horizontalPosition);
        } 
        return Metronome.instance.GetTimeFromBeat(GetClosestBeatSnappingFromPosition(horizontalPosition));
    }

    public float GetSnappedHorizontalPosition(float horizontalPosition)
    {
        if (Input.GetKey(KeyCode.LeftControl) || !beatSnapping) {
            return horizontalPosition;
        }
        return GetPositionFromBeat(GetClosestBeatSnappingFromPosition(horizontalPosition));
    }

}
