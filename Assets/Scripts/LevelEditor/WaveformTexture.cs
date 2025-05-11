using UnityEngine;
using UnityEngine.Rendering;

[RequireComponent(typeof(SpriteRenderer))]
public class WaveformTexture : MonoBehaviour
{
    private SpriteRenderer sr;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
    }

    public void SetTexture(Texture2D tex)
    {
        if (tex == null) return;
        Sprite sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0f, 0.5f), 100f, 0u, SpriteMeshType.FullRect);
        sr.sprite = sprite;
    }

    public void SetSpriteWidth(float targetWidth)
    {
        if (sr == null || sr.sprite == null) {
            Debug.LogWarning("Missing SpriteRenderer or Sprite");
            return;
        }

        float spriteWidthInWorldUnits = sr.sprite.bounds.size.x;
        float scaleFactor = targetWidth / spriteWidthInWorldUnits;

        transform.localScale = new Vector3(scaleFactor, 1f, 1f);
    }
}