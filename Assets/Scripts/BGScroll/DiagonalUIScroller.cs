using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RawImage))]
public class DiagonalUIScroller : MonoBehaviour
{
    public Vector2 scrollSpeed = new Vector2(0.1f, 0.1f);

    private RawImage rawImage;
    private Vector2 uvOffset = Vector2.zero;
    private void Start()
    {
        rawImage = GetComponent<RawImage>();
    }

    void Update()
    {
        uvOffset += scrollSpeed * Time.deltaTime;
        rawImage.uvRect = new Rect(uvOffset, rawImage.uvRect.size);
    }
}
