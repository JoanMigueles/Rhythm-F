using DG.Tweening;
using UnityEngine;

public class MultihitNote : DurationNote
{
    private bool beingHit = false;
    private int consumedAmount;

    public override void Move(int distance, bool laneSwap)
    {
        transform.position = new Vector3(NoteManager.instance.GetPositionFromTime(data.time + distance), 0f, 0f);
    }

    public override void UpdatePosition()
    {
        float xPos = beingHit ? 0f : NoteManager.instance.GetPositionFromTime(data.time + consumedAmount);
        transform.position = new Vector3(xPos, 0f, 0f);
        if (durationHandle != null) {
            durationHandle.SetDuration(data.duration - consumedAmount);
        }
        CheckForSound();
        CheckForAppearance();
    }

    public void SetConsumedDistance(int distance)
    {
        consumedAmount = distance;
    }

    public void SetHitting(bool hitting)
    {
        beingHit = hitting;
    }

    public void Pulsate()
    {
        transform.DOKill();
        transform.localScale = Vector3.one * 1.10f;

        transform.DOScale(Vector3.one, 0.15f)
                 .SetEase(Ease.InQuad);
    }

    public override void SetDisplayMode(bool gameplay)
    {
        base.SetDisplayMode(gameplay);
        durationHandle.gameObject.SetActive(gameplay);
        if (!gameplay) {
            consumedAmount = 0;
            beingHit = false;
        }
    }
}
