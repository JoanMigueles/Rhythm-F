using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class Timeline : MonoBehaviour
{
    [SerializeField] private float beatTiling = 10f;
    private float editorBeat = 0;
    private float timelineWidth;
    
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
    }

    private void Update()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        editorBeat = Metronome.instance.GetTimelineBeatPosition();
        if (scroll != 0) {
            Metronome.instance.SetTimelinePosition((int)(Metronome.instance.GetTimelinePosition() + scroll * 1000));
        }
        material.SetFloat("_Offset", -Mathf.Abs(editorBeat - Mathf.Floor(editorBeat)));


        // Example: Adjust beat spacing dynamically (replace this with actual logic)
        if (Input.GetKeyDown(KeyCode.UpArrow)) {
            beatTiling -= 2f;
            material.SetFloat("_Tiling", beatTiling);

        }
        else if (Input.GetKeyDown(KeyCode.DownArrow)) {
            beatTiling += 2f;
            material.SetFloat("_Tiling", beatTiling);
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
