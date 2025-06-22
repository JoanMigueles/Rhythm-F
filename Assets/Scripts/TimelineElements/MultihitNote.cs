using DG.Tweening;
using Unity.VisualScripting;
using UnityEngine;

public class MultihitNote : DurationNote
{
    public GameObject durationLine;
    private bool beingHit = false;
    public override void Move(int distance, bool laneSwap)
    {
        transform.position = new Vector3(NoteManager.instance.GetPositionFromTime(data.time + distance), 0f, 0f);
    }

    public override void UpdatePosition()
    {
        CheckForSound();
        if (beingHit) { 
            transform.position = new Vector3(0f, 0f, 0f); 
        } 
        else {
            transform.position = new Vector3(NoteManager.instance.GetPositionFromTime(data.time), 0f, 0f);
        }
        
        if (durationHandle != null) {
            durationHandle.transform.localPosition = new Vector3(NoteManager.instance.GetDistanceFromTime(data.duration), 0f, 0f);
        }
    }

    public void SetHitting(bool hitting)
    {
        beingHit = hitting;
    }

    public void Pulsate()
    {

    }
    private void HideHandle(bool hide)
    {
        durationHandle.GetComponent<SpriteRenderer>().enabled = !hide;
        durationLine.GetComponent<SpriteRenderer>().enabled = !hide;
    }
    public override void SetDisplayMode(bool gameplay)
    {
        base.SetDisplayMode(gameplay);
        HideHandle(gameplay);

        if (!gameplay) {
            beingHit = false;
        }
    }
}
