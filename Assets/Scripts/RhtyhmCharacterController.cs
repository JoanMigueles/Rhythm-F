using FMODUnity;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

public class RhtyhmCharacterController : MonoBehaviour
{
    public int lane;

    [field: Header("Hit")]
    [field: SerializeField] public EventReference hitReference { get; private set; }

    public GameObject popupPrefab;
    public int PERFECT_WINDOW = 35;
    public int PERFECT_SLASH_WINDOW = 65;
    public int GREAT_WINDOW = 100;
    public int SLASH_WINDOW = 150;
    private const int SWITCH_GRACE_TIME = 100; // ms for allowing hit in previous lane after a switch
    public float multihitTimeInterval = 1.5f;

    // HIT
    private bool hitBuffered = false;
    // HOLD
    private SliderNote holdedNote;
    private bool holding;
    // MULTIHIT
    private MultihitNote multihitNote;
    private bool multihitting;
    private float multihitTimer;
    // FLICK
    public float flickThreshold = 1000; // Pixels per second
    private bool isFlicking = false;
    private bool flickBuffered = false;

    private NoteManager nm;

    void Start()
    {
        lane = 1;
        nm = NoteManager.instance;
    }


    void Update()
    {
        if (!GameManager.instance.IsPlaying()) return;
        int currentTime = Metronome.instance.GetTimelinePosition();

        // --- Handle lane switch ---
        if (Input.GetKeyDown(KeyCode.Space)) {
            lane = lane == 0 ? 1 : 0;
        }

        // --- Handle click buffer ---
        if (Input.GetMouseButtonDown(0)) {
            hitBuffered = true;
        }

        // --- Handle flick buffer ---
        float mouseSpeedX = Input.GetAxis("Mouse X") / Time.deltaTime;
        if (!isFlicking && Mathf.Abs(mouseSpeedX) >= flickThreshold) {
            // Start of a flick
            isFlicking = true;
            flickBuffered = true;
        }
        else if (isFlicking && Mathf.Abs(mouseSpeedX) < flickThreshold - flickThreshold/2) {
            // Flick has ended
            isFlicking = false;
        }


        if (nm.activeNotes != null && nm.activeNotes.Count > 0) {
            HandleMissedNotes(currentTime);

            // Try resolving buffered click
            if (hitBuffered) {
                bool hit = TryHitNotes(currentTime, lane);

                // Mark buffer as consumed either on success or if outside the hit window
                if (hit || !InNoteWindow(currentTime)) {
                    Debug.Log("Air attack");
                    hitBuffered = false;
                }
            }
            if (flickBuffered) {
                bool slash = TrySlashNotes(currentTime, lane);

                // Mark buffer as consumed either on success or if outside the hit window
                if (slash || !InSlashWindow(currentTime)) {
                    flickBuffered = false;
                }
            }
        }

        if (holding) {
            int distanceHolded = Mathf.Abs(currentTime - holdedNote.data.time);
            if (distanceHolded >= holdedNote.data.duration) {
                SpawnPopup("Perfect!", holdedNote.data.lane);
                holdedNote.gameObject.SetActive(false);
                RuntimeManager.PlayOneShot(hitReference);
                holding = false;
                holdedNote = null;
                return;
            } else if (lane != holdedNote.data.lane || !Input.GetMouseButton(0) || Input.GetMouseButtonUp(0)){
                if (distanceHolded >= holdedNote.data.duration - GREAT_WINDOW) {
                    SpawnPopup("Perfect!", holdedNote.data.lane);
                    holdedNote.gameObject.SetActive(false);
                    RuntimeManager.PlayOneShot(hitReference);
                } else {
                    holdedNote.SetMissed();
                }
                holding = false;
                holdedNote = null;
                return;
            }

            holdedNote.SetConsumedDistance(distanceHolded);
        }

        if (multihitting) {
            int distanceHolded = Mathf.Abs(currentTime - multihitNote.data.time);
            if (distanceHolded >= multihitNote.data.duration) {
                multihitting = false;
                multihitNote.SetHitting(false);
                multihitNote.gameObject.SetActive(false);
                multihitNote = null;
                transform.position = new Vector3(transform.position.x, lane == 0 ? 1.5f : -1.5f, 0f);
                return;
                // End multihit
            }
            if (multihitTimer > multihitTimeInterval) {
                multihitting = false;
                multihitNote.SetHitting(false);
                multihitNote = null;
                transform.position = new Vector3(transform.position.x, lane == 0 ? 1.5f : -1.5f, 0f);
                return;
                // End multihit with damage taken
            }
            multihitTimer += Time.deltaTime;
            transform.position = new Vector3(transform.position.x, 0f, 0f);
        } else {
            transform.position = new Vector3(transform.position.x, lane == 0 ? 1.5f : -1.5f, 0f);
        }
    }

    bool TryHitNotes(int currentTime, int inputLane)
    {
        if (multihitting && multihitNote != null) {
            if (!RequiresSlash(multihitNote.data.type)) {
                multihitTimer = 0;
                multihitNote.Pulsate();
            }
            RuntimeManager.PlayOneShot(hitReference);
            return true;
        }

        foreach (var note in nm.activeNotes) {
            if (!note.gameObject.activeSelf)
                continue;

            int delta = note.data.time - currentTime;
            if (Mathf.Abs(delta) > GREAT_WINDOW)
                continue;

            if (note is MultihitNote multihit) {
                if (!RequiresSlash(note.data.type)) {
                    multihitNote = multihit;
                    multihitting = true;
                    multihit.SetHitting(true);
                }
                RuntimeManager.PlayOneShot(hitReference);
                return true;
            }

            if (note.data.lane != inputLane)
                continue;

            int absDelta = Mathf.Abs(delta);
            if (!RequiresSlash(note.data.type)) {
                if (absDelta <= PERFECT_WINDOW) {
                    SpawnPopup("Perfect!", note.data.lane);
                }
                else {
                    SpawnPopup("Great", note.data.lane);
                }
            }

            if (note is SliderNote slider) {
                holdedNote = slider;
                holding = true;
            } else {
                note.gameObject.SetActive(false);
            }

            RuntimeManager.PlayOneShot(hitReference);
            return true;
        }
        return false;
    }

    bool TrySlashNotes(int currentTime, int inputLane)
    {
        if (multihitting && multihitNote != null) {
            if (RequiresSlash(multihitNote.data.type)) {
                multihitTimer = 0;
                multihitNote.Pulsate();
            }
            RuntimeManager.PlayOneShot(hitReference);
            return true;
        }

        foreach (var note in nm.activeNotes) {
            if (!note.gameObject.activeSelf)
                continue;

            if (note is ShooterNote shooterNote && shooterNote.IsBeingAttacked()) continue;

            int delta = note.data.time - currentTime;
            if (Mathf.Abs(delta) > SLASH_WINDOW)
                continue;

            if (note is MultihitNote multihit) {
                if (RequiresSlash(note.data.type)) {
                    multihitNote = multihit;
                    multihitting = true;
                    multihit.SetHitting(true);
                }
                RuntimeManager.PlayOneShot(hitReference);
                return true;
            }

            if (note.data.lane != inputLane)
                continue;

            int absDelta = Mathf.Abs(delta);
            if (RequiresSlash(note.data.type)) {
                if (absDelta <= PERFECT_SLASH_WINDOW) {
                    SpawnPopup("Perfect!", note.data.lane);
                }
                else {
                    SpawnPopup("Great", note.data.lane);
                }
                if (note is ShooterNote shooter) {
                    shooter.ReturnBullet();
                } else {
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
        foreach (var note in nm.activeNotes) {
            if (!note.gameObject.activeSelf)
                continue;

            if (currentTime - note.data.time > SLASH_WINDOW) {
                if (note is ShooterNote shooter && !shooter.IsLeaving()) shooter.Leave();
                //note.gameObject.SetActive(false);
            }
            else if (note.data.time > currentTime) {
                break;
            }
        }
    }

    bool InNoteWindow(int currentTime)
    {
        foreach (var note in nm.activeNotes) {
            if (!note.gameObject.activeSelf)
                continue;

            if (Mathf.Abs(note.data.time - currentTime) <= GREAT_WINDOW)
                return true; // Still within window for some note

            if (note.data.time > currentTime + GREAT_WINDOW)
                break; // Future notes — no need to wait

        }
        return false;
    }

    bool InSlashWindow(int currentTime)
    {
        foreach (var note in nm.activeNotes) {
            if (!note.gameObject.activeSelf)
                continue;

            if (Mathf.Abs(note.data.time - currentTime) <= GREAT_WINDOW && RequiresSlash(note.data.type))
                return true; // Still within window for some note

            if (note.data.time > currentTime + GREAT_WINDOW)
                break; // Future notes — no need to wait

        }
        return false;
    }

    public void SpawnPopup(string result, int lane)
    {
        float yPos = lane == 0 ? 2f : -1f;
        GameObject popup = Instantiate(popupPrefab, new Vector3(0.2f, yPos, 0), Quaternion.identity);
        popup.GetComponent<PopupText>().Show(result);
    }

    public bool RequiresSlash(NoteType type)
    {
        if (type == NoteType.Slash || type == NoteType.Warn_Slash || type == NoteType.Multiple_Slash) {
            return true;
        } else {
            return false;
        }
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
        holding = false;
        holdedNote = null;
        multihitting = false;
        multihitNote = null;
        multihitTimer = 0;
    }
}
