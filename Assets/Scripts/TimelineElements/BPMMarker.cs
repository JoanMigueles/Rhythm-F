using TMPro;
using Unity.VisualScripting;
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

    public override void SetSelected(bool selected)
    {
        SpriteRenderer[] sprites = GetComponentsInChildren<SpriteRenderer>();
        foreach (SpriteRenderer sprite in sprites) {
            sprite.color = selected ? highlightedColor : normalColor;
        }
    }

    public void SelectThisMarker()
    {
        EditorManager em = NoteManager.instance as EditorManager;
        if (em == null) return;

        em.SetEditMode("Select");
        em.Select(this);
    }

    public void SetThisMarkerBPM(string BPM)
    {
        EditorManager em = NoteManager.instance as EditorManager;
        if (float.TryParse(BPM, out float bpmValue) && bpmValue != originalBPM) {
            bpmValue = Mathf.Min(bpmValue, 999f); // Clamp max to 999
            bpmValue = Mathf.Round(bpmValue * 10f) / 10f; // Round to one decimal
            SelectThisMarker();
            em.EditSelectedMarker(bpmValue);
        }
        else {
            BPMField.SetTextWithoutNotify(originalBPM.ToString());
        }
    }

    public void UpdateDisplay(float bpmValue)
    {
        Debug.Log("Updated display to " + bpmValue.ToString());

        originalBPM = bpmValue; // Update stored value after successful input
        BPMField.SetTextWithoutNotify(bpmValue.ToString());
    }

    public override void Move(int distance, bool laneSwap)
    {
        transform.position = new Vector3(NoteManager.instance.GetPositionFromTime(flag.offset + distance), 3.3f, 0f);
    }

    public override void UpdatePosition()
    {
        transform.position = new Vector3(NoteManager.instance.GetPositionFromTime(flag.offset), 3.3f, 0f);
    }
}
