using UnityEngine;

[RequireComponent(typeof(Renderer))]
public class ParallaxScroller : MonoBehaviour
{
    [SerializeField] private float worldScrollSpeed = 1f; // Units per second

    private Renderer quadRenderer;
    private float effectiveTilingX;
    private MaterialPropertyBlock propertyBlock;
    private float currentOffset;
    private Vector2 initialOffset;
    private Vector2 textureScale;

    void Start()
    {
        quadRenderer = GetComponent<Renderer>();
        propertyBlock = new MaterialPropertyBlock();

        // Use material instance to avoid sharedMaterial side effects
        initialOffset = quadRenderer.material.mainTextureOffset;
        textureScale = quadRenderer.material.mainTextureScale;

        float worldSizeX = quadRenderer.bounds.size.x;
        effectiveTilingX = textureScale.x / worldSizeX;
    }

    void LateUpdate()
    {
        currentOffset += Time.deltaTime * worldScrollSpeed * effectiveTilingX;
        currentOffset %= 1f; // Loop between 0-1

        propertyBlock.SetVector("_BaseMap_ST",
            new Vector4(
                textureScale.x,
                textureScale.y,
                currentOffset + initialOffset.x,
                initialOffset.y
            ));
        quadRenderer.SetPropertyBlock(propertyBlock);
    }
}
