using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class Pulsator : MonoBehaviour
{
    [Header("Pulsation Settings")]
    [SerializeField] private float pulseScale = 1.05f;
    [SerializeField] private float pulseDuration = 0.25f;
    [SerializeField] private Ease pulseEase = Ease.InQuad;

    private Vector3 originalScale;

    private void Start()
    {
        originalScale = transform.localScale;
    }

    public void Pulse()
    {
        transform.DOKill();
        transform.localScale = Vector3.one * pulseScale;

        transform.DOScale(Vector3.one, pulseDuration)
                 .SetEase(pulseEase);
    }

    private void OnDestroy()
    {
        PulsatorManager.instance.RemovePulsator(this);
        DOTween.Kill(transform);
    }
}