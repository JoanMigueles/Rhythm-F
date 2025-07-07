using DG.Tweening;
using FMODUnity;
using UnityEngine;

public class WarnNote : DurationNote
{

    [field: Header("Warn")]
    [field: SerializeField] public EventReference warnReference { get; private set; }

    public GameObject ghost;
    public TrailRenderer trail;
    public int attackDuration = 60;

    protected Vector3 frozenHandlePosition;
    protected bool warning = false;
    protected bool attacked = false;

    public GameObject warnSpeechBubble;
    private Vector3 warnSpeechTargetScale = Vector3.one;

    private void Start()
    {
        warnSpeechTargetScale = warnSpeechBubble.transform.localScale;
    }

    public void ShowSpeechBubble()
    {
        float rotationDegrees = 180f;
        float duration = 0.3f;

        warnSpeechBubble.transform.DOKill();

        warnSpeechBubble.transform.gameObject.SetActive(true);;
        warnSpeechBubble.transform.localScale = Vector3.zero;
        warnSpeechBubble.transform.rotation = Quaternion.Euler(0f, 0f, -rotationDegrees);

        warnSpeechBubble.transform.DOScale(warnSpeechTargetScale, duration).SetEase(Ease.OutBack);
        warnSpeechBubble.transform.DORotate(Vector3.zero, duration).SetEase(Ease.OutBack);
    }

    public override void UpdatePosition()
    {
        base.UpdatePosition();

        if (durationHandle == null) return;

        // Editor behavior
        if (!GameManager.instance.IsPlaying()) {
            durationHandle.transform.rotation = Quaternion.identity;
            durationHandle.SetDuration(data.duration);
            return;
        }
    }

    protected void Warn()
    {
        warning = true;

        frozenHandlePosition = durationHandle.transform.position;
        RuntimeManager.PlayOneShot(warnReference);

        ShowSpeechBubble();

        durationHandle.transform.DOKill();
        durationHandle.transform.DORotate(new Vector3(0, 0, 20), 0.05f)
            .SetEase(Ease.OutCubic);
    }

    protected virtual void Attack()
    {
        if (durationHandle == null) return;

        warnSpeechBubble.transform.DOKill();
        warnSpeechBubble.SetActive(false);

        trail.emitting = true;
        warning = false;
        attacked = true;

        durationHandle.transform.rotation = Quaternion.identity;
        float durationSeconds = attackDuration / 1000f;
        float startX = durationHandle.transform.position.x;
        float speed = startX / durationSeconds; // Units per second
        float totalDistance = startX + 10;
        float totalDuration = totalDistance / speed;

        durationHandle.transform.DOKill();
        durationHandle.transform.DOMoveX(-10, totalDuration)
                .SetEase(Ease.Linear)
                .OnComplete(() => {
                    gameObject.SetActive(false);
                });
    }

    public override void SetDisplayMode(bool gameplay)
    {
        base.SetDisplayMode(gameplay);
        ghost.SetActive(!gameplay);

        if (!gameplay) {
            durationHandle.transform.DOKill();
            durationHandle.transform.rotation = Quaternion.identity;
            durationHandle.SetDuration(data.duration);

            warnSpeechBubble.transform.DOKill();
            warnSpeechBubble.SetActive(false);

            trail.emitting = false;
            warning = false;
            attacked = false;
        }
    }
    public override void CheckForAppearance()
    {
        if (!GameManager.instance.IsPlaying()) return;

        float screenLeft = Camera.main.ViewportToWorldPoint(new Vector3(0, 0, transform.position.z)).x - 5f;
        float screenRight = Camera.main.ViewportToWorldPoint(new Vector3(1, 0, transform.position.z)).x + 5f;
        if (transform.position.x <= screenLeft - NoteManager.instance.GetDistanceFromTime(data.duration) || transform.position.x >= screenRight) {
            if (gameObject.activeSelf) {
                gameObject.SetActive(false);
            }
        }
        else if (!gameObject.activeSelf && !defeated && !attacked) {
            gameObject.SetActive(true);
            SetAnimated(true);
        }

        if (gameObject.activeSelf) {
            if (Metronome.instance.GetTimelinePosition() > data.time - (Metronome.instance.GetBeatSecondInterval() * 1000) && !warning && !attacked) {
                Warn();
            }
            if (Metronome.instance.GetTimelinePosition() > data.time - attackDuration && warning && !attacked) {
                Attack();
            }

            if (warning) {
                durationHandle.transform.position = frozenHandlePosition;
            }
        }
    }

    private void OnDisable()
    {
        durationHandle.transform.DOKill();
    }
}
