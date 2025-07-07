using UnityEngine;
using DG.Tweening;

public class Float : MonoBehaviour
{
    private Tween floatTween;
    public float amount = 0.3f;

    private void OnDisable()
    {
        StopFloating();
    }

    public void StartFloating()
    {
        if (floatTween != null && floatTween.IsActive())
            return;

        floatTween = transform.DOLocalMoveY(amount, 0.5f)
                            .SetRelative()
                            .SetLoops(-1, LoopType.Yoyo)
                            .SetEase(Ease.InOutSine);
    }

    public void StopFloating()
    {
        if (floatTween != null && floatTween.IsActive())
            floatTween.Kill();

        transform.localPosition = new Vector3(transform.localPosition.x, 0, transform.localPosition.z);
    }
}