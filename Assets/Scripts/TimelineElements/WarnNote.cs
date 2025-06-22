using DG.Tweening;
using FMODUnity;
using UnityEngine;

public class WarnNote : DurationNote
{

    [field: Header("Warn")]
    [field: SerializeField] public EventReference warnReference { get; private set; }

    public GameObject ghostLine;
    public TrailRenderer trail;
    public int attackDuration = 60;
    protected Vector3 frozenHandlePosition;
    protected bool warning = false;
    protected bool attacked = false;
    protected Tween attackTween;
    protected Tween warnTween;

    public override void UpdatePosition()
    {
        if (!gameObject.activeSelf) return;

        CheckForSound();
        float yPos = data.lane == 0 ? 1.5f : -1.5f;
        transform.position = new Vector3(NoteManager.instance.GetPositionFromTime(data.time), yPos, 0f);
        if (durationHandle == null) return;

        // Editor behavior
        if (!GameManager.instance.IsPlaying()) {
            durationHandle.transform.rotation = Quaternion.identity;
            durationHandle.SetDuration(data.duration);
            return;
        }

        // Game behavior
        if (Metronome.instance.GetTimelinePosition() > data.time - (Metronome.instance.beatSecondInterval * 1000) && !warning && !attacked) {
            Warn();
        }
        if (Metronome.instance.GetTimelinePosition() > data.time - attackDuration && warning && !attacked) {
            Attack();
        }

        if (warning) {
            durationHandle.transform.position = frozenHandlePosition;
        } else if (!attacked) {
            durationHandle.SetDuration(data.duration);
        }
    }

    protected void Warn()
    {
        warning = true;
        frozenHandlePosition = durationHandle.transform.position;
        RuntimeManager.PlayOneShot(warnReference);
        warnTween = durationHandle.transform.DORotate(new Vector3(0, 0, 20), 0.05f)
            .SetEase(Ease.OutCubic)
            .OnComplete(() => {
                warnTween = null;
            });
    }

    protected virtual void Attack()
    {
        if (durationHandle == null) return;

        warning = false;
        attacked = true;
        durationHandle.transform.rotation = Quaternion.identity;
        float durationSeconds = attackDuration / 1000f;

        trail.emitting = true;
        float startX = durationHandle.transform.position.x;
        float speed = startX / durationSeconds; // Units per second
        float totalDistance = startX + 10;
        float totalDuration = totalDistance / speed;

        attackTween = durationHandle.transform.DOMoveX(-10, totalDuration)
                .SetEase(Ease.Linear)
                .OnComplete(() => {
                    attackTween = null;
                    trail.emitting = false;
                });
    }

    protected virtual void HideGhost(bool hide)
    {
        GetComponent<SpriteRenderer>().enabled = !hide;
        ghostLine.GetComponent<SpriteRenderer>().enabled = !hide;
    }

    public override void SetDisplayMode(bool gameplay)
    {
        HideGhost(gameplay);
        base.SetDisplayMode(gameplay);

        if (!gameplay) {
            durationHandle.transform.DOKill();
            warning = false;
            attacked = false;
        }
    }
}
