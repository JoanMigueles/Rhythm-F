using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class Timeline : MonoBehaviour
{
    [SerializeField] private GameObject linePrefab;
    [SerializeField] private GameObject dotPrefab;
    [SerializeField] private float beatTiling = 10f;

    private float editorBeat = 0;
    private float timelineWidth;
    
    private List<float> testNoteBeats = new List<float>() { 1f, 3.5f, 7f, 10.5f };
    private List<GameObject> notes = new List<GameObject>();
    
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
        editorBeat = Metronome.instance.GetTimelineBeatPosition();
        if (scroll != 0) {
            Metronome.instance.SetTimelinePosition((int)(Metronome.instance.GetTimelinePosition() + scroll * 1000));
        }
        material.SetFloat("_Offset", -Mathf.Abs(editorBeat - Mathf.Floor(editorBeat)));
        UpdateNotes();


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

    private void SpawnNotes()
    {
        foreach (float beat in testNoteBeats) {
            GameObject newNote = Instantiate(dotPrefab, transform);
            newNote.SetActive(false);
            notes.Add(newNote);
        }
        UpdateNotes();
    }

    private void UpdateNotes()
    {
        (float start, float end) beatWindow = GetTimelineBeatWindow();

        for (int n = 0; n < notes.Count; n++) {
            float beat = testNoteBeats[n];
            float beatSpacing = 1 / beatTiling * timelineWidth;
            float xPosition = (beat - editorBeat) * beatSpacing;
            GameObject note = notes[n];
            RectTransform noteRect = note.GetComponent<RectTransform>();
            noteRect.anchoredPosition = new Vector2(xPosition, 0);
            
            if (beat > beatWindow.start && beat < beatWindow.end) {
                note.SetActive(true);
            } else {
                note.SetActive(false);
            }
        }
    }

    public (float start, float end) GetTimelineBeatWindow()
    {
        float beatSpacing = 1 / beatTiling;
        float halfRange = 0.5f / beatSpacing;
        float startBeat = editorBeat - halfRange;
        float endBeat = editorBeat + halfRange;
        return (startBeat, endBeat);
    }
}
