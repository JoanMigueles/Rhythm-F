using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class CenteredSnapScroll : MonoBehaviour, IBeginDragHandler, IEndDragHandler, IDragHandler
{
    [SerializeField] private RectTransform viewport;
    [SerializeField] private RectTransform content;
    //[SerializeField] private float snapSpeed = 10f;
    //[SerializeField] private float scaleFactor = 0.5f;
    //[SerializeField] private float minScale = 0.7f;
    [SerializeField] private float decelerationRate = 0.95f; // How quickly momentum slows down
    [SerializeField] private float maxMomentum = 2000f; // Maximum speed
    [SerializeField] private Color selectedColor;
    [SerializeField] private Color color;

    private RectTransform[] items;
    private bool isDragging;
    private Vector2 cursorPos;
    private Vector2 lastCursorPos;
    private float offsetY;

    private float momentum;
    private bool hasMomentum;

    private void Update()
    {
        if (!isDragging && hasMomentum) {
            // Apply momentum
            content.anchoredPosition += Vector2.up * momentum * Time.deltaTime;

            // Gradually reduce momentum
            momentum *= decelerationRate;

            // If momentum is very small, stop it and snap
            if (Mathf.Abs(momentum) < 50f) {
                momentum = 0f;
                hasMomentum = false;
            }
        }
        else {
            SnapToNearestItem();
        }
    }

    private void SnapToNearestItem()
    {
        if (content == null || viewport == null) return;

        float nearestDistance = float.MaxValue;
        float viewportCenter = viewport.rect.height / 2f;

        RectTransform nearestItem = null;
        Vector2 contentPos = content.anchoredPosition;

        foreach (RectTransform child in items) {
            if (child == null) continue;

            // Calculate child's center position in viewport space
            float childCenter = -child.anchoredPosition.y - contentPos.y;

            // Distance from viewport center
            float distance = Mathf.Abs(childCenter - viewportCenter);

            if (distance < nearestDistance) {
                nearestDistance = distance;
                nearestItem = child;
            }
        }

        if (nearestItem == null) return;

        SongPanel panel = nearestItem.GetComponent<SongPanel>();
        if (panel != null) {
            LevelListUI.instance.SetHoveredSong(panel.GetSongMetadata());
        }

        // Update visual feedback
        foreach (RectTransform child in items) {
            if (child == null) continue;

            Image img = child.GetComponent<Image>();
            if (img != null) {
                img.color = (child == nearestItem) ? selectedColor : color;
            }
        }

        if (!isDragging && !hasMomentum) {
            // Calculate target position to center the nearest item
            float targetY = -nearestItem.anchoredPosition.y - (viewport.rect.height) / 2f;

            // Apply with smoothing
            content.anchoredPosition = Vector2.Lerp(
                content.anchoredPosition,
                new Vector2(content.anchoredPosition.x, targetY),
                Time.deltaTime * 10f
            );
        }
    }

    public void OnBeginDrag(PointerEventData data)
    {
        isDragging = true;
        hasMomentum = false;
        momentum = 0f;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(viewport, data.position, null, out cursorPos);
        lastCursorPos = cursorPos;
        offsetY = cursorPos.y - content.anchoredPosition.y;
    }

    public void OnDrag(PointerEventData eventData)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(viewport, eventData.position, null, out cursorPos);

        // Calculate velocity based on cursor movement
        float deltaY = cursorPos.y - lastCursorPos.y;
        momentum = deltaY / Time.deltaTime;
        momentum = Mathf.Clamp(momentum, -maxMomentum, maxMomentum);

        content.anchoredPosition = new Vector2(content.anchoredPosition.x, cursorPos.y - offsetY);
        lastCursorPos = cursorPos;
    }

    public void OnEndDrag(PointerEventData data)
    {
        isDragging = false;
        hasMomentum = true;
    }

    public void SetItems()
    {
        // Get all child items (skip content's own RectTransform)
        items = new RectTransform[content.childCount];
        for (int i = 0; i < content.childCount; i++) {
            items[i] = content.GetChild(i) as RectTransform;
        }
    }
}
