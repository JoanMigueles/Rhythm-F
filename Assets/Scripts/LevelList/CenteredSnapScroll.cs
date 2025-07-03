using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;
using System;

public class CenteredSnapScroll : MonoBehaviour, IBeginDragHandler, IEndDragHandler, IDragHandler
{
    [SerializeField] private RectTransform viewport;
    [SerializeField] private RectTransform content;
    [SerializeField] private float decelerationRate = 0.95f; // How quickly momentum slows down
    [SerializeField] private float maxMomentum = 2000f; // Maximum speed

    private RectTransform[] items;
    private RectTransform hoveredItem;
    private bool isDragging;
    private Vector2 cursorPos;
    private Vector2 lastCursorPos;
    private float offsetY;

    private float momentum;
    private bool hasMomentum;

    private void Update()
    {
        if (!isDragging) {
            int hoveredIndex = Array.IndexOf(items, hoveredItem);
            int newIndex = 0;
            if (Input.GetKeyDown(KeyCode.UpArrow) || Input.mouseScrollDelta.y > 0f) {
                newIndex = hoveredIndex - 1;
                if (newIndex < 0)  newIndex = 0;
                SnapToItem(items[newIndex], true);
            }
            else if (Input.GetKeyDown(KeyCode.DownArrow) || Input.mouseScrollDelta.y < 0f) {
                newIndex = hoveredIndex + 1;
                if (newIndex >= items.Length) newIndex = items.Length - 1;
                SnapToItem(items[newIndex], true);
            }
        }

        if (!isDragging && hasMomentum) {
            // Apply momentum
            content.anchoredPosition += Vector2.up * momentum * Time.deltaTime;
            if (content.anchoredPosition.y < viewport.rect.height / 2) {
                momentum = momentum / (viewport.rect.height / 2 - content.anchoredPosition.y);
            } else if (content.anchoredPosition.y + content.rect.height > viewport.rect.height / 2) {
                momentum = momentum / (content.anchoredPosition.y + content.rect.height - viewport.rect.height / 2);
            }

            // Gradually reduce momentum
            momentum *= decelerationRate;

            // If momentum is very small, stop it and snap
            if (Mathf.Abs(momentum) < 50f) {
                momentum = 0f;
                hasMomentum = false;
            }
        }

        RectTransform nearestItem = CalculateNearesItem();
        if (nearestItem != null) {
            if (!DOTween.IsTweening(content))
                SnapToItem(nearestItem, true);
            if (nearestItem != hoveredItem)
                SetHoveredItem(nearestItem);
        }

    }

    private RectTransform CalculateNearesItem()
    {
        if (content == null || viewport == null) return null;

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

        return nearestItem;
    }

    public void SnapToItem(RectTransform item, bool smooth)
    {
        if (isDragging || hasMomentum) return;
        // Calculate target position to center the nearest item
        float targetY = -item.anchoredPosition.y - (viewport.rect.height) / 2f;

        // Kill any previous tweens to avoid conflicts
        content.DOKill();

        if (smooth) {
            content.DOAnchorPosY(targetY, 0.3f).SetEase(Ease.OutCubic);
        } else {
            content.anchoredPosition = new Vector2(content.anchoredPosition.x, targetY);
        }
    }

    private void SetHoveredItem(RectTransform item)
    {
        if (hoveredItem != null) {
            SongPanel previousPanel = hoveredItem.GetComponent<SongPanel>();
            if (previousPanel != null)
                previousPanel.SetHovered(false);
        }

        hoveredItem = item;
        SongPanel panel = hoveredItem.GetComponent<SongPanel>();

        if (panel != null) {
            panel.SetHovered(true);
            SongMetadata metadata = panel.GetSongMetadata();
            LevelListUI.instance.SetHoveredSong(panel.GetSongMetadata());
        }
    }

    public void OnBeginDrag(PointerEventData data)
    {
        content.DOKill();
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

        float newY = cursorPos.y - offsetY;

        content.anchoredPosition = new Vector2(content.anchoredPosition.x, newY);
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

        if (items.Length > 0) {
            SnapToItem(items[0], false);
        }
    }

    private void OnDestroy()
    {
        content.DOKill();
    }
}
