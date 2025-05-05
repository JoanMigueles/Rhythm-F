using TMPro;
using UnityEngine;

[System.Serializable]
public struct BPMFlag
{
    public int offset;
    public float BPM;

    public BPMFlag(int offset, float BPM = 120f)
    {
        this.offset = offset;
        this.BPM = BPM;
    }

    public BPMFlag(BPMFlag flag)
    {
        offset = flag.offset;
        BPM = flag.BPM;
    }
}

public class BPMMarker : TimelineElement
{
    public BPMFlag flag;
    public Color normalColor;
    public Color highlightedColor;
    private float originalBPM = 120f;
    [SerializeField] private TMP_InputField BPMField;

    public void Highlight(bool on)
    {
        SpriteRenderer[] sprites = GetComponentsInChildren<SpriteRenderer>();
        foreach (SpriteRenderer sprite in sprites) {
            sprite.color = on ? highlightedColor : normalColor;
        }
    }

    public void SelectThisMarker()
    {
        NoteManager.instance.SetEditMode("Select");
        NoteManager.instance.SelectMarker(this);
    }

    public void SetThisMarkerBPM(string BPM)
    {
        if (float.TryParse(BPM, out float bpmValue) && bpmValue != originalBPM) {
            bpmValue = Mathf.Min(bpmValue, 999f); // Clamp max to 999
            bpmValue = Mathf.Round(bpmValue * 10f) / 10f; // Round to one decimal
            SelectThisMarker();
            NoteManager.instance.EditSelectedMarker(bpmValue);
        }
        else {
            BPMField.SetTextWithoutNotify(originalBPM.ToString());
        }
    }

    public void UpdateDisplay(float bpmValue)
    {
        originalBPM = bpmValue; // Update stored value after successful input
        BPMField.SetTextWithoutNotify(bpmValue.ToString());
    }
}
