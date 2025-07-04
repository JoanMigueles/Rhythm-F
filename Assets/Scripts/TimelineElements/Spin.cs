using UnityEngine;
using DG.Tweening;

public class Spin : MonoBehaviour
{
    public float rotationSpeed = 360f; // Degrees per second
    public bool clockwise = false;
    public bool startSpinning = false;

    private Tween rotationTween;

    private void OnDisable()
    {
        StopSpinning();
    }

    private void Start()
    {
        if (startSpinning) {
            StartSpinning();
        }
    }

    public void StartSpinning()
    {
        if (rotationTween != null && rotationTween.IsActive()) return;

        float endValue = clockwise ? -rotationSpeed : rotationSpeed;

        rotationTween = transform
            .DORotate(new Vector3(0, 0, endValue), 1f, RotateMode.FastBeyond360)
            .SetRelative()
            .SetLoops(-1, LoopType.Incremental)
            .SetEase(Ease.Linear);
    }

    public void StopSpinning()
    {
        if (rotationTween != null && rotationTween.IsActive()) {
            rotationTween.Kill();
            rotationTween = null;
        }

        transform.rotation = Quaternion.identity;
    }
}