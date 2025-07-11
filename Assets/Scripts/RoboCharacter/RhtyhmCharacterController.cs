using FMODUnity;
using NUnit.Framework;
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
    private int healingPerSecond = 5;
    private int health;
    private float healTimer;
    // HIT
    private bool hitBuffered = false;
    private float hitBufferEndTime = 0;
    // HOLD
    private SliderNote heldNote;
    // MULTIHIT
    private MultihitNote activeMultihitNote;
    private float multihitTimer;
    // FLICK
    public float flickThreshold = 1000; // Pixels per second
    private bool isFlicking = false;
    private bool flickBuffered = false;
    private float flickBufferEndTime = 0;

    private NoteManager noteManager;
    private Metronome metronome;
    private CharacterPoseSwapper swapper;

    private bool songEnded = false;
    private bool dead = false;

    private void Awake()
    {
        swapper = GetComponent<CharacterPoseSwapper>();
    }

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
        if (dead) return;

        if (Input.GetKeyDown(KeyCode.L) && GameplayUI.instance != null && DialogueMissionManager.instance == null) {
            Lose();
            NoteManager.instance.gameObject.SetActive(false);
        }

        int currentTime = metronome.GetTimelinePosition();
        
        // Regenrate health
        if (health >= maxHealth) {
            health = maxHealth;
            healTimer = 0;
        } else {
            healTimer += Time.deltaTime;
        }

        if (healTimer >= 1) {
            healTimer = 0;
            health += healingPerSecond;
        }

        // Check for level ending
        if (NoteManager.instance.IsPastLastNote() && !songEnded && GameplayUI.instance != null && DialogueMissionManager.instance == null) {
            songEnded = true;
            StartCoroutine(GameplayUI.instance.WinLevel());
        }

        HandleInput(currentTime);
        UpdateCharacterPosition();
    }

    private void HandleInput(int currentTime)
    {
        if (DialogueMissionManager.instance != null && DialogueMissionManager.instance.CanAdvanceDialogue()) return;
        // Lane switching
        if (Input.GetKeyDown(KeyCode.Space))
        {
            currentLane = currentLane == 0 ? 1 : 0;
            if (currentLane == 1) swapper.SetBasePose(BaseAnimationState.Running);
            else
            {
                if (activeMultihitNote == null) swapper.TriggerJump();
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
        if (heldNote == null) return;

        int distanceHeld = Mathf.Abs(currentTime - heldNote.data.time);
        int maxScore = heldNote.data.duration;

        // Determine how much of the slider was held
        int previousConsumed = heldNote.GetConsumedDistance();
        int newConsumed = Mathf.Clamp(distanceHeld, 0, maxScore);

        // Determine if we've crossed a 20ms threshold
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
            CompleteHoldNote();
            if (currentLane == 1) swapper.SetBasePose(BaseAnimationState.Running);
            else swapper.SetBasePose(BaseAnimationState.Surfing);
            return;
        }

        // Breaking condition
        if (currentLane != heldNote.data.lane || !Input.GetMouseButton(0) || Input.GetMouseButtonUp(0)) {
            if (newConsumed >= maxScore - greatWindow) {
                CompleteHoldNote();
            }
            else {
                heldNote.SetMissed(true);
                heldNote.missed = true;
                heldNote = null;
                misses++;
                combo = 0;
                if (DialogueMissionManager.instance != null)
                    DialogueMissionManager.instance.RegisterMissionAction(false);
            }
            if (currentLane == 1) swapper.SetBasePose(BaseAnimationState.Running);
            else swapper.SetBasePose(BaseAnimationState.Surfing);
            return;
        }
    }

    private void CompleteHoldNote()
    {
        SpawnPopup("Perfect!", heldNote.data.lane);
        perfects++;
        combo++;
        if (DialogueMissionManager.instance != null)
            DialogueMissionManager.instance.RegisterMissionAction(false);
        heldNote.Kill();
        heldNote = null;
    }

    private void HandleMultihitNote(int currentTime)
    {
        if (activeMultihitNote == null) return;

        int distanceHeld = Mathf.Abs(currentTime - activeMultihitNote.data.time);
        activeMultihitNote.SetConsumedDistance(distanceHeld);

        if (distanceHeld >= activeMultihitNote.data.duration) {
            SpawnPopup("Perfect!", 0);
            perfects++;
            activeMultihitNote.SetHitting(false);
            activeMultihitNote.Kill();
            activeMultihitNote = null;
            if (DialogueMissionManager.instance != null)
                DialogueMissionManager.instance.RegisterMissionAction(false);
            return;
        } else if (multihitTimer > multihitTimeInterval) {
            TakeDamage(activeMultihitNote);
            misses++;
            activeMultihitNote.SetHitting(false);
            activeMultihitNote = null;
            if (DialogueMissionManager.instance != null)
                DialogueMissionManager.instance.RegisterMissionAction(true);
            return;
        }

        multihitTimer += Time.deltaTime;
    }

    private void UpdateCharacterPosition()
    {
        float yPosition = activeMultihitNote != null ? 0f : (currentLane == 0 ? 1.5f : -1.5f);
        transform.position = new Vector3(transform.position.x, yPosition, 0f);
    }

    bool TryHitNotes(int currentTime, int inputLane, InteractionType interaction)
    {
        // Determine parameters based on interaction type
        bool requiresSlash = interaction == InteractionType.Flick;
        int windowSize = requiresSlash ? slashWindow : greatWindow;
        int perfectWindowSize = requiresSlash ? perfectSlashWindow : perfectWindow;

        // Handle multihit notes first
        if (activeMultihitNote != null) {
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
            if (note.missed || note.defeated) continue;
            if (!note.gameObject.activeSelf) continue;
            if (IsObstacle(note.data.type)) continue;
            int delta = note.data.time - currentTime;
            if (Mathf.Abs(delta) > windowSize) continue;

            // Handle multihit notes
            if (note is MultihitNote multihit) {
                if (RequiresSlash(multihit.data.type) == requiresSlash) {
                    activeMultihitNote = multihit;
                    multihit.SetHitting(true);
                    multihitTimer = 0;
                    RuntimeManager.PlayOneShot(hitReference);
                    AddScore(50);
                    return true;
                }
                continue;
            }

            if (note.data.lane != inputLane) continue;

            // Handle slider notes
            if (note is SliderNote slider && interaction == InteractionType.Hit) {
                SpawnPopup("Perfect!", note.data.lane);
                perfects++;
                AddScore(100);

                heldNote = slider;
                swapper.SetBasePose(BaseAnimationState.Holding);
                slider.SetHoldingGlowActive(true);
                RuntimeManager.PlayOneShot(hitReference);
                return true;
            }

            // Handle for the correct interaction type on single notes (slash or normal hit)
            if (RequiresSlash(note.data.type) == requiresSlash) {
                if (Mathf.Abs(delta) <= perfectWindowSize) {
                    SpawnPopup("Perfect!", note.data.lane);
                    perfects++;
                    AddScore(150);
                    if (DialogueMissionManager.instance != null)
                        DialogueMissionManager.instance.RegisterMissionAction(false);
                }
                else {
                    SpawnPopup("Great", note.data.lane);
                    greats++;
                    AddScore(100);
                    if (DialogueMissionManager.instance != null)
                        DialogueMissionManager.instance.RegisterMissionAction(false);
                }
                note.Kill();
                RuntimeManager.PlayOneShot(hitReference);
                return true;
            }
        }
        return false;
    }

    void HandleMissedNotes(int currentTime)
    {
        for (int i = 0; i < NoteManager.instance.activeNotes.Count; i++) {
            var note = NoteManager.instance.activeNotes[i];
            int enemyHitDuration = 60;
            if (note.data.time > currentTime + enemyHitDuration / 2) {
                break;
            }

            if (note == heldNote || note == activeMultihitNote) 
                continue;

            if (!note.gameObject.activeSelf || note.defeated)
                continue;

            switch (note.data.type) {
                case NoteType.Saw:
                case NoteType.Laser:
                    if (currentTime < note.data.time + enemyHitDuration / 2 && currentTime > note.data.time - enemyHitDuration / 2) {
                        if (note.data.lane == currentLane) {
                            TakeDamage(note);
                            if (DialogueMissionManager.instance != null)
                                DialogueMissionManager.instance.RegisterMissionAction(true);
                        }
                    } else if (currentTime > note.data.time + enemyHitDuration / 2 && !note.missed) {
                        note.missed = true;
                        if (DialogueMissionManager.instance != null)
                            DialogueMissionManager.instance.RegisterMissionAction(false);
                    }
                    break;
                case NoteType.Warn_Slash:
                case NoteType.Slash:
                case NoteType.Multiple_Slash:
                    if (currentTime - note.data.time > slashWindow && currentTime - note.data.time < slashWindow + enemyHitDuration) {
                        if (note.data.lane == currentLane || note.data.type == NoteType.Multiple_Slash) TakeDamage(note);
                        MissNote(note);
                    }
                    break;
                case NoteType.Warn_Hit:
                case NoteType.Hit:
                case NoteType.Multiple_Hit:
                    if (currentTime - note.data.time > greatWindow && currentTime - note.data.time < greatWindow + enemyHitDuration) {
                        if (note.data.lane == currentLane || note.data.type == NoteType.Multiple_Hit) TakeDamage(note);
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

    public int GetMaxCombo() 
    { 
        return maxCombo;
    }

    public int GetHealth()
    {
        return health;
    }

    public bool IsDead()
    {
        return dead;
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

        combo = 0;

        // Inmune while testing
        if (EditorUI.instance == null || DialogueMissionManager.instance == null) {
            health -= note.damage;
        }

        note.hitPlayer = true;
        swapper.TriggerDamage();

        if (health <= 0) {
            health = 0;
            Lose();
        }

    }

    private void Lose()
    {
        Debug.Log("LOST");
        swapper.SetBasePose(BaseAnimationState.Dead);
        dead = true;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        if (GameplayUI.instance != null) {
            StartCoroutine(GameplayUI.instance.LoseLevel());
        }
    }

    private void MissNote(Note note)
    {
        if (note.missed) return;
        misses++;
        combo = 0;
        note.missed = true;
        if (DialogueMissionManager.instance != null) 
            DialogueMissionManager.instance.RegisterMissionAction(true);

        if (note is SliderNote slider) {
            slider.SetMissed(true);
            misses++;
            
        }

        if (note is ShooterNote shooter) {
            shooter.Leave();
        }
    }

    // Hide the cursor and reset every state everytime the character is re-enabled
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
        activeMultihitNote = null;
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
        currentLane = 1;
    }
}
