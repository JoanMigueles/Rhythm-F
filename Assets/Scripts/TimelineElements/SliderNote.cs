using DG.Tweening;
using UnityEngine;

public class SliderNote : DurationNote
{
    int consumedAmount;

    public override void UpdatePosition()
    {
        float yPos = data.lane == 0 ? 1.5f : -1.5f;
        transform.position = new Vector3(NoteManager.instance.GetPositionFromTime(data.time + consumedAmount) , yPos, 0f);
        if (durationHandle != null) {
            durationHandle.SetDuration(data.duration - consumedAmount);
        }
    }

    public void SetConsumedDistance(int distance)
    {
        consumedAmount = distance;
    }

    public void SetMissed()
    {
        foreach (SpriteRenderer sr in GetComponentsInChildren<SpriteRenderer>()) {
            Color color = sr.color;
            sr.DOColor(new Color(color.r, color.g, color.b, 0.5f), 0.2f);
        }
    }

    public override void SetDisplayMode(bool gameplay)
    {
        base.SetDisplayMode(gameplay);

        if (!gameplay) {
            consumedAmount = 0;
            foreach (SpriteRenderer sr in GetComponentsInChildren<SpriteRenderer>()) {
                sr.color = new Color(sr.color.r, sr.color.g, sr.color.b, 1f);
            }
        }
    }
}
