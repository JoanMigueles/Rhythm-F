using UnityEngine;

public class SliderNote : Note
{
    public NoteHandle durationHandle;

    public override void UpdatePosition()
    {
        base.UpdatePosition();
        if (durationHandle != null) {
            durationHandle.transform.localPosition = new Vector3(NoteManager.instance.GetDistanceFromTime(data.duration), 0f, 0f);
        }
    }
}
