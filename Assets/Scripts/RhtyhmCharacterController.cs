using FMODUnity;
using UnityEngine;

// Enum to define interaction types
public enum InteractionType
{
    Hit,
    Flick
}

public class RhtyhmCharacterController : MonoBehaviour
{
    public int currentLane;

    [field: Header("Hit")]
    [field: SerializeField] public EventReference hitReference { get; private set; }

    public GameObject popupPrefab;
    public int perfectWindow = 50;
    public int perfectSlashWindow = 65;
    public int greatWindow = 120;
    public int slashWindow = 150;
    private const int SWITCH_GRACE_TIME = 100; // ms for allowing hit in previous lane after a switch
    public float multihitTimeInterval = 1.5f;
    public float sliderScoreInterval = 0.1f;
    private float sliderScoreTimer;
    private int score;

    // HIT
    private bool hitBuffered = false;
    private int hitBufferTime = -1;
    // HOLD
    private SliderNote heldNote;
    private bool isHolding;
    // MULTIHIT
    private MultihitNote activeMultihitNote;
    private bool isMultihitting;
    private float multihitTimer;
    // FLICK
    public float flickThreshold = 1000; // Pixels per second
    private bool isFlicking = false;
    private bool flickBuffered = false;

    private NoteManager noteManager;
    private Metronome metronome;

    void Start()
    {
        score = 0;
        currentLane = 1;
        noteManager = NoteManager.instance;
        metronome = Metronome.instance;
    }


    void Update()
    {
        if (!GameManager.instance.IsPlaying()) return;
        int currentTime = metronome.GetTimelinePosition();
        HandleInput();
        HandleNoteInteractions(currentTime);
        UpdateCharacterPosition();
    }

    private void HandleInput()
    {
        // Lane switching
        if (Input.GetKeyDown(KeyCode.Space)) {
            currentLane = currentLane == 0 ? 1 : 0;
        }

        // Hit buffer
        if (Input.GetMouseButtonDown(0)) {
            hitBuffered = true;
        }

        // Flick detection
        float mouseSpeedX = Input.GetAxis("Mouse X") / Time.deltaTime;
        if (!isFlicking && Mathf.Abs(mouseSpeedX) >= flickThreshold) {
            isFlicking = true;
            flickBuffered = true;
        }
        else if (isFlicking && Mathf.Abs(mouseSpeedX) < flickThreshold * 0.5f) {
            isFlicking = false;
        }
    }

    private void HandleNoteInteractions(int currentTime)
    {
        if (noteManager.activeNotes == null || noteManager.activeNotes.Count == 0) return;

        HandleMissedNotes(currentTime);

        // Try resolving buffered inputs
        if (hitBuffered) {
            bool hitSuccess = TryHitNotes(currentTime, currentLane, InteractionType.Hit);
            if (hitSuccess || !IsInNoteWindow(currentTime, InteractionType.Hit)) {
                hitBuffered = false;
            }
        }

        if (flickBuffered) {
            bool slashSuccess = TryHitNotes(currentTime, currentLane, InteractionType.Flick);
            if (slashSuccess || !IsInNoteWindow(currentTime, InteractionType.Flick)) {
                flickBuffered = false;
            }
        }

        HandleHoldNote(currentTime);
        HandleMultihitNote(currentTime);
    }

    private void HandleHoldNote(int currentTime)
    {
        if (!isHolding || heldNote == null) return;

        int distanceHeld = Mathf.Abs(currentTime - heldNote.data.time);

        sliderScoreTimer += Time.deltaTime;
        if (sliderScoreTimer >= sliderScoreInterval) {
            sliderScoreTimer = 0;
            score += 10;
        }

        if (distanceHeld >= heldNote.data.duration) {
            CompleteHoldNote("Perfect!", heldNote);
            return;
        }

        if (currentLane != heldNote.data.lane || !Input.GetMouseButton(0) || Input.GetMouseButtonUp(0)) {
            if (distanceHeld >= heldNote.data.duration - greatWindow) {
                CompleteHoldNote("Perfect!", heldNote);
            }
            else {
                sliderScoreTimer = 0;
                heldNote.SetMissed();
            }
            isHolding = false;
            heldNote = null;
            return;
        }

        heldNote.SetConsumedDistance(distanceHeld);
    }

    private void CompleteHoldNote(string rating, SliderNote note)
    {
        sliderScoreTimer = 0;
        SpawnPopup(rating, note.data.lane);
        note.gameObject.SetActive(false);
        isHolding = false;
        heldNote = null;
    }

    private void HandleMultihitNote(int currentTime)
    {
        if (!isMultihitting || activeMultihitNote == null) return;

        int distanceHeld = Mathf.Abs(currentTime - activeMultihitNote.data.time);

        if (distanceHeld >= activeMultihitNote.data.duration || multihitTimer > multihitTimeInterval) {
            EndMultihit();
            return;
        }

        multihitTimer += Time.deltaTime;
    }

    private void EndMultihit()
    {
        isMultihitting = false;
        if (activeMultihitNote != null) {
            activeMultihitNote.SetHitting(false);
            activeMultihitNote.gameObject.SetActive(false);
            activeMultihitNote = null;
        }
    }

    private void UpdateCharacterPosition()
    {
        float yPosition = isMultihitting ? 0f : (currentLane == 0 ? 1.5f : -1.5f);
        transform.position = new Vector3(transform.position.x, yPosition, 0f);
    }

    bool TryHitNotes(int currentTime, int inputLane, InteractionType interaction)
    {
        // Determine parameters based on interaction type
        bool requiresSlash = interaction == InteractionType.Flick;
        int windowSize = requiresSlash ? slashWindow : greatWindow;
        int perfectWindowSize = requiresSlash ? perfectSlashWindow : perfectWindow;

        // Handle multihit notes first
        if (isMultihitting && activeMultihitNote != null) {
            if (RequiresSlash(activeMultihitNote.data.type) == requiresSlash) {
                activeMultihitNote.Pulsate();
                multihitTimer = 0;
                RuntimeManager.PlayOneShot(hitReference);
                score += 50;
                return true;
            }
            return false;
        }

        foreach (Note note in noteManager.activeNotes) {
            if (!note.gameObject.activeSelf) continue;

            // Skip shooter notes that are being attacked
            if (note is ShooterNote shooterNote &&
                shooterNote.IsBeingAttacked())
                continue;

            int delta = note.data.time - currentTime;
            if (Mathf.Abs(delta) > windowSize) continue;

            // Handle multihit notes
            if (note is MultihitNote multihit) {
                if (RequiresSlash(multihit.data.type) == requiresSlash) {
                    activeMultihitNote = multihit;
                    isMultihitting = true;
                    multihit.SetHitting(true);
                    multihitTimer = 0;
                    RuntimeManager.PlayOneShot(hitReference);
                    score += 50;
                    return true;
                }
                continue;
            }

            if (note.data.lane != inputLane) continue;

            // For the correct interaction type (slash or normal hit)
            if (RequiresSlash(note.data.type) == requiresSlash) {
                if (Mathf.Abs(delta) <= perfectWindowSize) {
                    SpawnPopup("Perfect!", note.data.lane);
                    score += 150;
                } else {
                    SpawnPopup("Great", note.data.lane);
                    score += 100;
                }

                if (note is ShooterNote shooter) {
                    if (interaction == InteractionType.Flick) {
                        shooter.ReturnBullet();
                    }
                }
                else if (note is SliderNote slider && interaction == InteractionType.Hit) {
                    heldNote = slider;
                    isHolding = true;
                }
                else {
                    note.gameObject.SetActive(false);
                }

                RuntimeManager.PlayOneShot(hitReference);
                return true;
            }
        }
        return false;
    }

    void HandleMissedNotes(int currentTime)
    {
        foreach (var note in noteManager.activeNotes) {
            if (!note.gameObject.activeSelf)
                continue;

            if (currentTime - note.data.time > slashWindow) {
                if (note is ShooterNote shooter && !shooter.IsLeaving()) shooter.Leave();
                //note.gameObject.SetActive(false);
            }
            else if (note.data.time > currentTime) {
                break;
            }
        }
    }

    private bool IsInNoteWindow(int currentTime, InteractionType interaction)
    {
        int windowSize = interaction == InteractionType.Flick ? slashWindow : greatWindow;

        foreach (var note in noteManager.activeNotes) {
            if (!note.gameObject.activeSelf) continue;

            // Check if we've passed all relevant notes
            if (note.data.time > currentTime + windowSize)
                break;

            // Check if note is within window
            bool inWindow = Mathf.Abs(note.data.time - currentTime) <= windowSize;
            return true;
        }
        return false;
    }

    public void SpawnPopup(string result, int lane)
    {
        Debug.Log("Spawn");
        float yPos = lane == 0 ? 2f : -1f;
        GameObject popup = Instantiate(popupPrefab, new Vector3(0.2f, yPos, 0), Quaternion.identity);
        popup.GetComponent<PopupText>().Show(result);
    }

    public bool RequiresSlash(NoteType type)
    {
        return type == NoteType.Slash ||
               type == NoteType.Warn_Slash ||
               type == NoteType.Multiple_Slash;
    }

    public int GetScore()
    {
        return score;
    }

    private void OnDisable()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void OnEnable()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        isHolding = false;
        heldNote = null;
        isMultihitting = false;
        activeMultihitNote = null;
        multihitTimer = 0;
    }
}
