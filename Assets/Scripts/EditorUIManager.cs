using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EditorUIManager : UIManager
{
    public static EditorUIManager instance { get; private set; }
    public TMP_Text timer;
    public TMP_Text beat;
    public TMP_Text subdivision;
    public TMP_Text songTitle;
    public Slider subdivisionSlider;
    public SongSlider songSlider;
    public Timeline timeline;
    private readonly int[] valueMap = { 1, 1, 2, 3, 4, 5, 6, 7, 8, 9, 12, 16 };

    private void Awake()
    {
        instance = this;
    }

    private void Start()
    {
        subdivisionSlider.minValue = 0;
        subdivisionSlider.maxValue = valueMap.Length - 1;
        subdivisionSlider.value = 2;
    }

    // Update is called once per frame
    void Update()
    {
        timer.text = FormatTimeMS(Metronome.instance.GetTimelinePosition());
        beat.text = Metronome.instance.GetTimelineBeatPosition().ToString("F2");

        // Convert mouse screen position to world position
        Vector3 worldPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        // Get just the horizontal (x) position
        float horizontalPosition = worldPosition.x;

    }

    public string FormatTimeMS(int milliseconds)
    {
        int totalCentiseconds = milliseconds / 10;
        int minutes = totalCentiseconds / 6000;
        int seconds = (totalCentiseconds % 6000) / 100;
        int centiseconds = totalCentiseconds % 100;

        return string.Format("{0:00}:{1:00}:{2:00}", minutes, seconds, centiseconds);
    }

    public void SetBeatSubdivisionSnapping(float subdivisionIndex)
    {
        // Array holding all possible values in order
        int i = Mathf.RoundToInt(subdivisionIndex);

        if (subdivisionIndex == 0) {
            NoteManager.instance.beatSnapping = false;
            subdivision.text = "None";
        } else {
            NoteManager.instance.beatSnapping = true;
            subdivision.text = "1/" + valueMap[i].ToString();
        }
        
        NoteManager.instance.noteSubdivisionSnapping = valueMap[i];
    }

    public void SetSongSliderMaxValue(int value)
    {
        songSlider.SetMaxValue(value);
    }

    public void SetSongTitle(string name, string artist)
    {
        if (string.IsNullOrEmpty(name) && string.IsNullOrEmpty(artist)) {
            songTitle.text = "---";
        }
        else if (string.IsNullOrEmpty(name)) {
            songTitle.text = $"Unnamed - {artist}";
        }
        else if (string.IsNullOrEmpty(artist)) {
            songTitle.text = $"{name}";
        } else {
            songTitle.text = $"{name} - {artist}";
        }
        
    }
}
