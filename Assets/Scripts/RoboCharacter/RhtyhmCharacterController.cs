using FMODUnity;
using UnityEditor.Experimental.GraphView;
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
    public int perfectWindow = 40;
    public int perfectSlashWindow = 65;
    public int greatWindow = 120;
    public int slashWindow = 150;
    private const int SWITCH_GRACE_TIME = 80; // ms for allowing hit in previous lane after a switch
    public float multihitTimeInterval = 1.5f;
    public float sliderScoreInterval = 0.1f;

    // SCORE
    public int comboMultiplierInterval = 9;
    public int maxComboMultiplier = 6;
    private int score;
    private int combo;
    private int maxCombo;
    private int perfects;
    private int greats;
    private int misses;
    // HEALTH
    public int maxHealth = 120;
    private int health;
    // HIT
    private bool hitBuffered = false;
    private float hitBufferEndTime = 0;
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
    private float flickBufferEndTime = 0;

    private NoteManager noteManager;
    private Metronome metronome;
    private CharacterPoseSwapper swapper;

    void Start()
    {
        score = 0;
        currentLane = 1;
        noteManager = NoteManager.instance;
        metronome = Metronome.instance;
        swapper = GetComponent<CharacterPoseSwapper>();
    }

    void Update()
    {
        if (!GameManager.instance.IsPlaying()) return;
        int currentTime = metronome.GetTimelinePosition();
        HandleInput(currentTime);
        UpdateCharacterPosition();
    }

    private void HandleInput(int currentTime)
    {
        // Lane switching
        if (Input.GetKeyDown(KeyCode.Space))
        {
            currentLane = currentLane == 0 ? 1 : 0;
            if (currentLane == 1) swapper.SetBasePose(BaseAnimationState.Running);
            else
            {
                if (!isMultihitting) swapper.TriggerJump();
                swapper.SetBasePose(BaseAnimationState.Surfing);
            }

            if (hitBuffered)
            {
                TryHitNotes(currentTime, currentLane, InteractionType.Hit);
                hitBuffered = false;
            }
            else if (flickBuffered)
            {
                TryHitNotes(currentTime, currentLane, InteractionType.Flick);
                flickBuffered = false;
            }
        }

        // Hit detection
        if (Input.GetMouseButtonDown(0)) {
            swapper.TriggerHit();
            bool hitSuccess = TryHitNotes(currentTime, currentLane, InteractionType.Hit);
            if (!hitSuccess) {
                hitBuffered = true;
                hitBufferEndTime = currentTime + SWITCH_GRACE_TIME;
            }
        }

        // Flick detection
        float mouseSpeedX = Input.GetAxis("Mouse X") / Time.deltaTime;
        if (!isFlicking && Mathf.Abs(mouseSpeedX) >= flickThreshold) {
            swapper.TriggerSlash(mouseSpeedX < 0);
            isFlicking = true;
            bool flickSuccess = TryHitNotes(currentTime, currentLane, InteractionType.Flick);
            if (!flickSuccess) {
                flickBuffered = true;
                flickBufferEndTime = currentTime + SWITCH_GRACE_TIME;
            }
        }
        else if (isFlicking && Mathf.Abs(mouseSpeedX) < flickThreshold * 0.5f) {
            isFlicking = false;
        }

        // Expire old buffers
        if (hitBuffered && currentTime > hitBufferEndTime) hitBuffered = false;
        if (flickBuffered && currentTime > flickBufferEndTime) flickBuffered = false;

        

        HandleMissedNotes(currentTime);
        HandleHoldNote(currentTime);
        HandleMultihitNote(currentTime);
    }

    private void HandleHoldNote(int currentTime)
    {
        if (!isHolding || heldNote == null) return;

        int distanceHeld = Mathf.Abs(currentTime - heldNote.data.time);
        int maxScore = heldNote.data.duration;

        // Determine how much of the slider was held
        int previousConsumed = heldNote.GetConsumedDistance();
        int newConsumed = Mathf.Clamp(distanceHeld, 0, maxScore);

        // Determine if we've crossed a 10ms threshold
        int previousScoreUnits = previousConsumed / 20;
        int currentScoreUnits = newConsumed / 20;

        if (currentScoreUnits > previousScoreUnits) {
            int scoreGain = (currentScoreUnits - previousScoreUnits) * 5;
            score += scoreGain;
        }

        // Update consumed distance
        heldNote.SetConsumedDistance(newConsumed);

        // Completion
        if (newConsumed >= maxScore) {
            CompleteHoldNote("Perfect!", heldNote);
            isHolding = false;
            heldNote = null;
            if (currentLane == 1) swapper.SetBasePose(BaseAnimationState.Running);
            else swapper.SetBasePose(BaseAnimationState.Surfing);
            return;
        }

        // Breaking condition
        if (currentLane != heldNote.data.lane || !Input.GetMouseButton(0) || Input.GetMouseButtonUp(0)) {
            if (newConsumed >= maxScore - greatWindow) {
                CompleteHoldNote("Perfect!", heldNote);
            }
            else {
                heldNote.SetMissed(true);
                heldNote.missed = true;
                misses++;
            }
            isHolding = false;
            heldNote = null;
            if (currentLane == 1) swapper.SetBasePose(BaseAnimationState.Running);
            else swapper.SetBasePose(BaseAnimationState.Surfing);
            return;
        }
    }

    private void CompleteHoldNote(string rating, SliderNote note)
    {
        SpawnPopup(rating, note.data.lane);
        perfects++;
        combo++;
        note.gameObject.SetActive(false);
    }

    private void HandleMultihitNote(int currentTime)
    {
        if (!isMultihitting || activeMultihitNote == null) return;

        int distanceHeld = Mathf.Abs(currentTime - activeMultihitNote.data.time);

        if (distanceHeld >= activeMultihitNote.data.duration) {
            EndMultihit();
            SpawnPopup("Perfect!", 0);
            perfects++;
            return;
        } else if (multihitTimer > multihitTimeInterval) {
            EndMultihit();
            TakeDamage(activeMultihitNote);
            misses++;
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
                AddScore(50, false);
                return true;
            }
            return false;
        }

        foreach (Note note in noteManager.activeNotes) {
            if (!note.gameObject.activeSelf) continue;
            if (note.missed) continue;

            // Skip shooter notes that are being attacked
            if (note is ShooterNote shooterNote &&
                shooterNote.IsBeingAttacked())
                continue;

            if (IsObstacle(note.data.type)) continue;

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
                    AddScore(50);
                    currentLane = 1;
                    return true;
                }
                continue;
            }

            if (note.data.lane != inputLane) continue;

            // For the correct interaction type (slash or normal hit)
            if (RequiresSlash(note.data.type) == requiresSlash) {
                if (Mathf.Abs(delta) <= perfectWindowSize) {
                    SpawnPopup("Perfect!", note.data.lane);
                    perfects++;
                    AddScore(150);
                }
                else {
                    SpawnPopup("Great", note.data.lane);
                    greats++;
                    AddScore(100);
                }

                if (note is ShooterNote shooter) {
                    if (interaction == InteractionType.Flick) {
                        shooter.ReturnBullet();
                    }
                }
                else if (note is SliderNote slider && interaction == InteractionType.Hit) {
                    heldNote = slider;
                    isHolding = true;
                    swapper.SetBasePose(BaseAnimationState.Holding);
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
            if (note.data.time > currentTime) {
                break;
            }

            if (note == heldNote || note == activeMultihitNote) 
                continue;

            if (!note.gameObject.activeSelf)
                continue;

            int enemyHitDuration = 200;
            switch (note.data.type) {
                case NoteType.Saw:
                case NoteType.Laser:
                    if (currentTime - note.data.time > slashWindow && currentTime - note.data.time < slashWindow + enemyHitDuration) {
                        if (note.data.lane == currentLane) {
                            TakeDamage(note);
                            combo = 0;
                        }
                    }
                    break;
                case NoteType.Warn_Slash:
                    if (currentTime - note.data.time > slashWindow && currentTime - note.data.time < slashWindow + enemyHitDuration) {
                        
                        if (note.data.lane == currentLane) TakeDamage(note);
                        MissNote(note);
                    } 
                    break;
                case NoteType.Slash:
                    if (currentTime - note.data.time > slashWindow && currentTime - note.data.time < slashWindow + enemyHitDuration) {
                        if (note.data.lane == currentLane) TakeDamage(note);
                        MissNote(note);
                    }
                    break;
                case NoteType.Multiple_Slash:
                    if (currentTime - note.data.time > slashWindow && currentTime - note.data.time < slashWindow + enemyHitDuration) {
                        TakeDamage(note);
                        MissNote(note);
                    }
                    break;
                case NoteType.Warn_Hit:
                case NoteType.Hit:
                    if (currentTime - note.data.time > greatWindow && currentTime - note.data.time < greatWindow + enemyHitDuration) {
                        if (note.data.lane == currentLane) TakeDamage(note);
                        MissNote(note);
                    }
                    break;
                case NoteType.Multiple_Hit:
                    if (currentTime - note.data.time > greatWindow && currentTime - note.data.time < greatWindow + enemyHitDuration) {
                        TakeDamage(note);
                        MissNote(note);
                    }
                    break;
                case NoteType.Slider:
                    if (currentTime - note.data.time > greatWindow && currentTime - note.data.time < greatWindow + enemyHitDuration) {
                        MissNote(note);
                    }
                    break;
            }
        }
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

    public bool IsObstacle(NoteType type)
    {
        return type == NoteType.Saw ||
            type == NoteType.Laser;
    }

    public int GetScore()
    {
        return score;
    }

    public (int, int, int) GetHitStats()
    {
        return (perfects, greats, misses);
    }

    public int GetCombo()
    {
        return combo;
    }

    public int GetMaxCombo() { 
        return maxCombo;
    }

    public int GetHealth()
    {
        return health;
    }

    public int GetComboMultiplier()
    {
        int scoreMultiplier = (combo / comboMultiplierInterval) + 1;
        if (scoreMultiplier > maxComboMultiplier)
            scoreMultiplier = maxComboMultiplier;
        return scoreMultiplier;
    }

    private void AddScore(int addedScore, bool triggerCombo = true)
    {
        if (triggerCombo) {
            combo++;
            if (combo > maxCombo) maxCombo = combo;
        }

        int scoreMultiplier = GetComboMultiplier();
        score += scoreMultiplier * addedScore;
    }

    private void TakeDamage(Note note)
    {
        if (note.hitPlayer) return;
        health -= note.damage;
        note.hitPlayer = true;
    }

    private void MissNote(Note note)
    {
        if (note.missed) return;
        misses++;
        combo = 0;
        note.missed = true;

        if (note is SliderNote slider) {
            slider.SetMissed(true);
            misses++;
        }

        if (note is ShooterNote shooter) {
            shooter.Leave();
        }
        Debug.Log(misses);
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
        swapper.SetBasePose(BaseAnimationState.Running);

        hitBuffered = false;
        hitBufferEndTime = 0;
        heldNote = null;
        isHolding = false;
        activeMultihitNote = null;
        isMultihitting = false;
        multihitTimer = 0;
        isFlicking = false;
        flickBuffered = false;
        flickBufferEndTime = 0;
        health = maxHealth;
        combo = 0;
        maxCombo = 0;
        perfects = 0;
        greats = 0;
        misses = 0;
    }
}
