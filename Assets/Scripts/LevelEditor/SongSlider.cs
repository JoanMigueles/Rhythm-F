using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;

public class SongSlider : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    private Slider slider;
    private bool isUserDragging = false;

    private void Awake()
    {
        slider = GetComponent<Slider>();
    }

    private void Update()
    {
        if (!isUserDragging) {
            slider.SetValueWithoutNotify(Metronome.instance.GetNormalizedTimelinePosition());
        }
    }

    public void SetTimelinePosition()
    {
        if (isUserDragging) {
            Metronome.instance.SetNormalizedTimelinePosition(slider.value);
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        isUserDragging = true;
        /*
        Metronome.instance.songInstance.getPaused(out wasPaused);
        Metronome.instance.songInstance.setPaused(true);*/
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        SetTimelinePosition();
        /*
        Metronome.instance.songInstance.setPaused(wasPaused);*/
        StartCoroutine(UnlockDrag());
    }

    private IEnumerator UnlockDrag()
    {
        yield return new WaitForSecondsRealtime(0.02f); // 20ms delay;
        isUserDragging = false;
    }
    public void SetMaxValue(int maxValue)
    {
        slider.maxValue = maxValue;
    }
}