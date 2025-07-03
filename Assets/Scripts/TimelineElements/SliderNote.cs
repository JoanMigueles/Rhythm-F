using DG.Tweening;
using UnityEngine;

public class SliderNote : DurationNote
{
    public ParticleSystem glowParticles;
    private int consumedAmount;

    public override void UpdatePosition()
    {
        float yPos = data.lane == 0 ? 1.5f : -1.5f;
        transform.position = new Vector3(NoteManager.instance.GetPositionFromTime(data.time + consumedAmount) , yPos, 0f);
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

    public int GetConsumedDistance()
    {
        return consumedAmount;
    }

    public void SetHoldingGlowActive(bool glow)
    {
        glowParticles.gameObject.SetActive(glow);

        if (glow) glowParticles.Play();
        else glowParticles.Stop();
    }

    public void SetMissed(bool missed)
    {
        foreach (SpriteRenderer sr in GetComponentsInChildren<SpriteRenderer>()) {
            sr.DOKill(); // Cancela cualquier tween anterior en este SpriteRenderer

            Color color = sr.color;
            float targetAlpha = missed ? 0.5f : 1f;

            sr.DOColor(new Color(color.r, color.g, color.b, targetAlpha), 0.2f);
        }
    }

    public override void SetDisplayMode(bool gameplay)
    {
        base.SetDisplayMode(gameplay);

        if (gameplay) {
            // Notes on gameplay/testing
        } else {
            // Notes on editor
            consumedAmount = 0;
            SetHoldingGlowActive(false);
            SetMissed(false);
        }
    }
}
