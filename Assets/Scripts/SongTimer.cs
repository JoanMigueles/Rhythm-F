using TMPro;
using UnityEngine;

public class SongTimer : MonoBehaviour
{
    private TMP_Text timer;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        timer = GetComponent<TMP_Text>();
    }

    // Update is called once per frame
    void Update()
    {
        timer.text = FormatTimeMS(Metronome.instance.GetTimelinePosition());
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
