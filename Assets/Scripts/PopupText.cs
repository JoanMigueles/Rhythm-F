using UnityEngine;
using TMPro; // Or remove if using TextMesh
using DG.Tweening;

public class PopupText : MonoBehaviour
{
    public TMP_Text textMesh; // Or TextMesh if not using TMP
    public float moveUpDistance = 1f;
    public float duration = 0.6f;

    public void Show(string result)
    {
        textMesh.text = result;

        // Move up and fade
        transform.DOMoveY(transform.position.y + moveUpDistance, duration).SetEase(Ease.OutCubic);
        textMesh.DOFade(0, duration).OnComplete(() => Destroy(gameObject));
    }
}