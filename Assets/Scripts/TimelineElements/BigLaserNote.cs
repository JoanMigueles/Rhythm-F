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
    public Color laserColor;
    public float startingHeight;
    private bool appeared = false;
    private bool shot = false;

    public GameObject warnSpeechBubble;
    private Vector3 warnSpeechTargetScale = Vector3.one;

    private void Start()
    {
        warnSpeechTargetScale = warnSpeechBubble.transform.localScale;
    }

    public override void UpdatePosition()
    {
        float yPos = data.lane == 0 ? 1.5f : -1.5f;
        // Editor behavior
        if (!GameManager.instance.IsPlaying()) {
            transform.position = new Vector3(NoteManager.instance.GetPositionFromTime(data.time), yPos, 0f);
            return;
        }
        CheckForAppearance();
    }

    private void Appear()
    {
        gameObject.SetActive(true);
        transform.position = new Vector3(17f, data.lane == 0 ? 1.5f : -1.5f, 0f);
        transform.DOMoveX(11, appearingTime/1000f)
                .SetEase(Ease.OutCubic)
                .OnComplete(() => {
                    Warn();
                });
    }

    private void Warn()
    {
        ShowSpeechBubble();
        RuntimeManager.PlayOneShot(warnReference);
    }

    public void ShowSpeechBubble()
    {
        float rotationDegrees = 180f;
        float duration = 0.3f;

        warnSpeechBubble.transform.DOKill();

        warnSpeechBubble.transform.gameObject.SetActive(true); ;
        warnSpeechBubble.transform.localScale = Vector3.zero;
        warnSpeechBubble.transform.rotation = Quaternion.Euler(0f, 0f, -rotationDegrees);

        warnSpeechBubble.transform.DOScale(warnSpeechTargetScale, duration).SetEase(Ease.OutBack);
        warnSpeechBubble.transform.DORotate(Vector3.zero, duration).SetEase(Ease.OutBack);
    }

    private void ShootLaser()
    {
        if (laser != null) {
            warnSpeechBubble.transform.DOKill();
            warnSpeechBubble.SetActive(false);

            laser.SetActive(true);
            SpriteRenderer sr = laser.GetComponent<SpriteRenderer>();
            sr.DOKill();
            sr.color = Color.white;
            laser.transform.localScale = new Vector3(sr.transform.localScale.x, startingHeight, sr.transform.localScale.z);

            Sequence seq = DOTween.Sequence();
            seq.Join(laser.transform.DOScaleY(0f, (float)laserDuration / 1000));
            seq.Join(sr.DOFade(0f, (float)laserDuration / 1000));
            seq.Join(sr.DOColor(laserColor, (float)laserDuration / 1000));
            seq.SetEase(Ease.InQuad);
            seq.OnComplete(() => Leave());
        }
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

    public override void SetDisplayMode(bool gameplay)
    {
        if (gameplay) {
            if (Metronome.instance.GetTimelinePosition() > data.time) {
                appeared = true;
                shot = true;
                gameObject.SetActive(false);
            }
            transform.position = new Vector3(30f, 10f, 0f);
        } else {
            gameObject.SetActive(true);
            transform.DOKill();
            laser.GetComponent<SpriteRenderer>().DOKill();
            laser.transform.DOKill();
            laser.SetActive(false);
            warnSpeechBubble.transform.DOKill();
            warnSpeechBubble.SetActive(false);
            appeared = false;
            shot = false;
        }
    }

    public override void CheckForAppearance()
    {
        if (!GameManager.instance.IsPlaying()) return;

        // Game behavior
        if (Metronome.instance.GetTimelinePosition() > data.time - (Metronome.instance.GetBeatSecondInterval() * 1000) - appearingTime && !appeared) {
            appeared = true;
            Appear();
        }
        else if (Metronome.instance.GetTimelinePosition() > data.time && !shot) {
            shot = true;
            ShootLaser();
        }
    }
}
