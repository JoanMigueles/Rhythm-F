using DG.Tweening;
using FMODUnity;
using System.Collections;
using UnityEngine;

public class BigLaserNote : Note
{
    [field: Header("Warn")]
    [field: SerializeField] public EventReference warnReference { get; private set; }

    public GameObject laser;
    public int appearingTime = 500;
    public int laserDuration = 400;
    private bool appeared = false;
    private bool shot = false;
    private bool leaving = false;
    public override void UpdatePosition()
    {
        if (!gameObject.activeSelf) return;
        float yPos = data.lane == 0 ? 1.5f : -1.5f;

        // Editor behavior
        if (!GameManager.instance.IsPlaying()) {
            transform.position = new Vector3(NoteManager.instance.GetPositionFromTime(data.time), yPos, 0f);
            return;
        }

        // Game behavior
        if (Metronome.instance.GetTimelinePosition() > data.time - (Metronome.instance.beatSecondInterval * 1000) - appearingTime && !appeared) {
            appeared = true;
            Appear();
        } else if (Metronome.instance.GetTimelinePosition() > data.time && !shot) {
            shot = true;
            ShootLaser();
        } else if (Metronome.instance.GetTimelinePosition() > data.time + laserDuration && !leaving) {
            leaving = true;
            Leave();
        }
    }

    private void Appear()
    {
        gameObject.GetComponent<SpriteRenderer>().enabled = true;
        transform.position = new Vector3(17f, data.lane == 0 ? 1.5f : -1.5f, 0f);
        transform.DOMoveX(11, appearingTime/1000f)
                .SetEase(Ease.OutCubic)
                .OnComplete(() => {
                    Warn();
                });
    }

    private void Warn()
    {
        RuntimeManager.PlayOneShot(warnReference);
    }

    public void Leave()
    {
        if (laser != null) {
            laser.SetActive(false);
        }
        transform.DOMoveY(8f, 0.5f)
            .SetEase(Ease.InBack)
            .OnComplete(() => {
                gameObject.SetActive(false);
            });
    }

    private void ShootLaser()
    {
        if (laser != null) {
            laser.SetActive(true);
        }
    }

    public override void SetDisplayMode(bool gameplay)
    {
        base.SetDisplayMode(gameplay);
        gameObject.GetComponent<SpriteRenderer>().enabled = !gameplay;

        if (!gameplay) {
            transform.DOKill();
            laser.SetActive(false);
            appeared = false;
            shot = false;
            leaving = false;
        }
    }
}
