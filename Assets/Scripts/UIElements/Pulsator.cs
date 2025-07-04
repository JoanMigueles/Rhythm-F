using UnityEngine;
using DG.Tweening;
public class Pulsator : MonoBehaviour
{
    [SerializeField] private float pulseScale = 1.05f;
    [SerializeField] private float pulseDuration = 0.25f;
    [SerializeField] private Ease pulseEase = Ease.InQuad;

    private Vector3 originalScale;
    private Tween pulseTween;

    private void Start()
    {
        originalScale = transform.localScale;
        PulsatorManager.instance.AddPulsator(this);
    }

    public void Pulse()
    {
        if (!enabled) return;
        // Kill the previous pulse tween if it's still active
        if (pulseTween != null && pulseTween.IsActive())
            pulseTween.Kill();

        transform.localScale = originalScale * pulseScale;

        pulseTween = transform.DOScale(originalScale, pulseDuration)
                 .SetEase(pulseEase);
    }

    private void OnDestroy()
    {
        PulsatorManager.instance.RemovePulsator(this);
        DOTween.Kill(transform);
    }
}