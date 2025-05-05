using SFB;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EditorUIManager : UIManager
{
    public static EditorUIManager instance {  get; private set; }
    public TMP_Text timer;
    public TMP_Text beat;
    public TMP_Text subdivision;
    public TMP_Text songTitle;
    public Slider subdivisionSlider;
    public SongSlider songSlider;
    public WaveformTexture[] waveformTextures;
    public Timeline timeline;
    public Button playButton;
    public Button pauseButton;
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
        
        foreach (WaveformTexture waveformTexture in waveformTextures) {
            waveformTexture.SetSpriteWidth(Metronome.instance.GetSongLength() / 1000f * NoteManager.instance.noteSpeed);
            waveformTexture.transform.position = new Vector3(-Metronome.instance.GetTimelinePosition() / 1000f * NoteManager.instance.noteSpeed, waveformTexture.transform.position.y, 0f);
        }
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

    public void SetSongName(string name)
    {
        SongDataManager.instance.SetCustomSelectedSongName(name);
        SetSongTitle(name, SongDataManager.instance.GetCustomSelectedSongMetadata().artist);
    }

    public void SetSongArtist(string artist)
    {
        SongDataManager.instance.SetCustomSelectedSongArtist(artist);
        SetSongTitle(SongDataManager.instance.GetCustomSelectedSongMetadata().songName, artist);
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

    public void SaveButton()
    {
        NoteManager.instance.SaveActiveElements();
    }

    public void PlayButton()
    {
        Metronome.instance.PlaySong();
    }

    public void PauseButton()
    {
        Metronome.instance.PauseSong();
    }

    public void DisplayPause(bool isPlaying)
    {
        playButton.gameObject.SetActive(!isPlaying);
        pauseButton.gameObject.SetActive(isPlaying);
    }

    public void StopButton() {
        Metronome.instance.StopSong();
    }

    public void OpenAudioFileExplorer()
    {
        var extensions = new[] { new ExtensionFilter("Audio Files", "mp3", "wav", "ogg") };
        string[] paths = StandaloneFileBrowser.OpenFilePanel("Select Audio File", "", extensions, false);

        if (paths.Length > 0 && !string.IsNullOrEmpty(paths[0])) {
            StartCoroutine(SongDataManager.instance.SaveAndLoadCustomAudioFile(paths[0]));
            ApplyWaveformTexture();
        }
    }

    public void ApplyWaveformTexture()
    {
        Texture2D texture2D = Metronome.instance.GetSongWaveformTexture();
        if (waveformTextures != null) {
            foreach (WaveformTexture waveformTexture in waveformTextures) {
                waveformTexture.SetTexture(texture2D);
            }
        }
    }

    public void OpenCoverFileExplorer()
    {
        var extensions = new[] { new ExtensionFilter("Image Files", "png", "jpeg", "jpg") };
        string[] paths = StandaloneFileBrowser.OpenFilePanel("Select Cover File", "", extensions, false);
    }
}
