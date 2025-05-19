using UnityEngine;

public class BigSliderNote : SliderNote
{
    public override void Move(int distance, bool laneSwap)
    {
        transform.position = new Vector3(NoteManager.instance.GetPositionFromTime(data.time + distance), 0f, 0f);
    }

    public override void UpdatePosition()
    {
        transform.position = new Vector3(NoteManager.instance.GetPositionFromTime(data.time), 0f, 0f);
        if (durationHandle != null) {
            durationHandle.transform.localPosition = new Vector3(NoteManager.instance.GetDistanceFromTime(data.duration), 0f, 0f);
        }
    }
}
