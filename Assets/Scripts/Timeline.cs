using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Timeline : MonoBehaviour
{
    [SerializeField] private GameObject linePrefab;
    [SerializeField] private GameObject dotPrefab;
    [SerializeField] private float beatTiling = 10f;

    private float editorBeat = 0;
    private List<GameObject> lines = new List<GameObject>();
    private List<GameObject> notes = new List<GameObject>();
    private float timelineWidth;
    private float beatSpacing;
    private List<float> testBeats = new List<float>() { 1f, 3.5f, 7f, 10.5f };
    private Material material;

    private void OnValidate()
    {
        if (beatTiling < 0.1f)
            beatTiling = 0.1f;
    }

    private void Start()
    {
        timelineWidth = GetComponent<RectTransform>().rect.width;
        Image img = GetComponent<Image>();
        material = new Material(img.material);
        img.material = material;

        material.SetFloat("_Tiling", beatTiling);
        material.SetFloat("_Offset", -Mathf.Abs(editorBeat - Mathf.Floor(editorBeat)));

        SpawnNotes();
    }

    private void Update()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0) {
            editorBeat += scroll;
            material.SetFloat("_Offset", -Mathf.Abs(editorBeat - Mathf.Floor(editorBeat)));
            UpdateNotes();
        }

        // Example: Adjust beat spacing dynamically (replace this with actual logic)
        if (Input.GetKeyDown(KeyCode.UpArrow)) {
            beatTiling -= 2f;
            material.SetFloat("_Tiling", beatTiling);
            UpdateNotes();

        }
        else if (Input.GetKeyDown(KeyCode.DownArrow)) {
            beatTiling += 2f;
            material.SetFloat("_Tiling", beatTiling);
            UpdateNotes();
        }
    }

    /*
    private void RedrawLines()
    {
        foreach (Transform l in transform.GetChild(0)) {
            Destroy(l.gameObject);
        }
        lines.Clear();

        timelineWidth = GetComponent<RectTransform>().rect.width;
        beatSpacing = beatPercentageSpacing / 100 * timelineWidth;
        int initialLineCount = Mathf.CeilToInt(timelineWidth / beatSpacing) + 2; // +2 buffer
        for (int i = 0; i < initialLineCount; i++) {
            GameObject line = Instantiate(linePrefab, transform.GetChild(0));
            line.SetActive(false);
            lines.Add(line);
        }
        UpdateLines();
    }*/

    /*
    private void UpdateLines()
    {
        float halfRange = timelineWidth / 2 / beatSpacing;
        int startBeat = Mathf.CeilToInt(editorBeat - halfRange);
        int endBeat = Mathf.FloorToInt(editorBeat + halfRange);
        int requiredLines = endBeat - startBeat + 1;

        // Expand the pool if needed
        while (lines.Count < requiredLines) {
            GameObject newLine = Instantiate(linePrefab, transform.GetChild(0));
            newLine.SetActive(false);
            lines.Add(newLine);
        }

        // Position active lines
        int index = 0;
        for (int beat = startBeat; beat <= endBeat; beat++) {
            if (index >= lines.Count) break;

            float xPosition = (beat - editorBeat) * beatSpacing;
            GameObject line = lines[index];
            RectTransform lineRect = line.GetComponent<RectTransform>();
            lineRect.anchoredPosition = new Vector2(xPosition, 0);
            line.SetActive(true);
            index++;
        }

        // Deactivate extra lines
        for (int i = index; i < lines.Count; i++) {
            lines[i].SetActive(false);
        }

        UpdateNotes();
    }*/

    private void SpawnNotes()
    {
        foreach (float beat in testBeats) {
            GameObject newNote = Instantiate(dotPrefab, transform);
            newNote.SetActive(false);
            notes.Add(newNote);
        }
        UpdateNotes();
    }

    private void UpdateNotes()
    {
        beatSpacing = 1 / beatTiling * timelineWidth;
        float halfRange = timelineWidth / 2 / beatSpacing;
        float startBeat = editorBeat - halfRange;
        float endBeat = editorBeat + halfRange;

        for (int n = 0; n < notes.Count; n++) {
            float beat = testBeats[n];
            float xPosition = (beat - editorBeat) * beatSpacing;
            GameObject note = notes[n];
            RectTransform noteRect = note.GetComponent<RectTransform>();
            noteRect.anchoredPosition = new Vector2(xPosition, 0);
            
            if (beat > startBeat && beat < endBeat) {
                note.SetActive(true);
            } else {
                note.SetActive(false);
            }
        }
    }
}
