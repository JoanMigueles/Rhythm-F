using TMPro;
using UnityEngine;

public class EditorUIManager : MonoBehaviour
{
    public TMP_Text timer;
    public TMP_Text beat;
    public TMP_Text cursorBeat;
    public Timeline timeline;
    private RectTransform canvasRectTransform;

    private void Awake()
    {
        canvasRectTransform = GetComponent<RectTransform>();
    }
    // Update is called once per frame
    void Update()
    {
        timer.text = FormatTimeMS(Metronome.instance.GetTimelinePosition());
        beat.text = Metronome.instance.GetTimelineBeatPosition().ToString("F2");
        /*
        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRectTransform, Input.mousePosition, null, out localPoint
        );*/

        /*
        // Obtener los límites del rectángulo del canvas
        (float minX, float maxX) = (-canvasRectTransform.rect.width / 2f, canvasRectTransform.rect.width / 2f);
        float normalizedX = Mathf.InverseLerp(minX, maxX, localPoint.x);

        (float startBeat, float endBeat) = timeline.GetTimelineBeatWindow();
        float beatX = Mathf.Lerp(startBeat, endBeat, normalizedX);
        cursorBeat.text = beatX.ToString("F2");*/

        // Convert mouse screen position to world position
        Vector3 worldPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        // Get just the horizontal (x) position
        float horizontalPosition = worldPosition.x;
        cursorBeat.text = horizontalPosition.ToString("F2");

    }

    public string FormatTimeMS(int milliseconds)
    {
        int totalCentiseconds = milliseconds / 10;
        int minutes = totalCentiseconds / 6000;
        int seconds = (totalCentiseconds % 6000) / 100;
        int centiseconds = totalCentiseconds % 100;

        return string.Format("{0:00}:{1:00}:{2:00}", minutes, seconds, centiseconds);
    }
}
