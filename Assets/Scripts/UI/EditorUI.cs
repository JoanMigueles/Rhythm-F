using DG.Tweening;
using SFB;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EditorUI : UIManager
{
    public static EditorUI instance {  get; private set; }
    public TMP_Text timer;
    public TMP_Text beat;
    public TMP_Text subdivision;
    public TMP_Text songTitle;

    public TMP_InputField titleField;
    public TMP_InputField artistField;
    public TMP_Text audioField;

    public Slider subdivisionSlider;
    private readonly int[] valueMap = { 1, 1, 2, 3, 4, 5, 6, 7, 8, 9, 12, 16 }; // Subdivision slider values
    public SongSlider songSlider;
    public WaveformTexture[] waveformTextures;

    public Button playButton;
    public Button pauseButton;

    public RectTransform topPanel;
    public RectTransform bottomPanel;
    public RectTransform leftPanel;
    private Sequence panelSequence;
    public GameObject testDummy;
    public bool isHidden;


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

    void Update()
    {
        // UPDATE TIMERS
        timer.text = FormatTimeMS(Metronome.instance.GetTimelinePosition());
        beat.text = Metronome.instance.GetTimelineBeatPosition().ToString("F2");
        
        // UPDATE WAVEFORM POSITION
        foreach (WaveformTexture waveformTexture in waveformTextures) {
            waveformTexture.SetSpriteWidth(Metronome.instance.GetSongLength() / 1000f * EditorManager.instance.noteSpeed);
            waveformTexture.transform.position = new Vector3(-Metronome.instance.GetTimelinePosition() / 1000f * EditorManager.instance.noteSpeed, waveformTexture.transform.position.y, 0f);
        }
    }

    private string FormatTimeMS(int milliseconds)
    {
        int totalCentiseconds = milliseconds / 10;
        int minutes = totalCentiseconds / 6000;
        int seconds = (totalCentiseconds % 6000) / 100;
        int centiseconds = totalCentiseconds % 100;

        return string.Format("{0:00}:{1:00}:{2:00}", minutes, seconds, centiseconds);
    }

    // SONG DATA DISPLAY
    public void DisplaySongData(SongMetadata metadata)
    {
        titleField.SetTextWithoutNotify(metadata.songName);
        artistField.SetTextWithoutNotify(metadata.artist);
        audioField.text = metadata.audioFileName;
        SetSongTitleDisplay(metadata.songName, metadata.artist);
    }

    private void SetSongTitleDisplay(string name, string artist)
    {
        if (string.IsNullOrEmpty(name) && string.IsNullOrEmpty(artist)) {
            songTitle.text = "Unnamed";
        }
        else if (string.IsNullOrEmpty(name)) {
            songTitle.text = $"Unnamed - {artist}";
        }
        else if (string.IsNullOrEmpty(artist)) {
            songTitle.text = $"{name}";
        }
        else {
            songTitle.text = $"{name} - {artist}";
        }

    }

    // SUBDIVISION SLIDER
    public void SetBeatSubdivisionSnapping(float subdivisionIndex)
    {
        int i = Mathf.RoundToInt(subdivisionIndex);

        if (subdivisionIndex == 0) {
            EditorManager.instance.beatSnapping = false;
            subdivision.text = "None";
        } else {
            EditorManager.instance.beatSnapping = true;
            subdivision.text = "1/" + valueMap[i].ToString();
        }

        EditorManager.instance.noteSubdivisionSnapping = valueMap[i];
    }

    // SONG TITLE INPUT FIELD
    public void SetSongName(string name)
    {
        EditorManager.instance.SetSongName(name);
    }

    // SONG ARTIST INPUT FIELD
    public void SetSongArtist(string artist)
    {
        EditorManager.instance.SetSongArtist(artist);
    }

    // SAVE AND EXIT BUTTON
    public void SaveAndExitButton()
    {
        EditorManager.instance.SaveChanges();
        GameManager.instance.OpenSongList();
    }

    // SAVE AND EXIT BUTTON
    public void ExitButton()
    {
        GameManager.instance.OpenSongList();
    }

    // METRONOME TOGGLE
    public void MetronomeToggle(bool on)
    {
        Metronome.instance.SetMetronomeSound(on);
    }

    // SAVE BUTTON
    public void SaveButton()
    {
        EditorManager.instance.SaveChanges();
    }

    // PLAY BUTTON
    public void PlayButton()
    {
        Metronome.instance.PlaySong();
    }

    // PAUSE BUTTON
    public void PauseButton()
    {
        Metronome.instance.PauseSong();
    }

    public void DisplayPause(bool isPlaying)
    {
        playButton.gameObject.SetActive(!isPlaying);
        pauseButton.gameObject.SetActive(isPlaying);
    }

    // STOP BUTTON
    public void StopButton() {
        Metronome.instance.StopSong();
    }

    // LOAD AUDIO BUTTON
    public void OpenAudioFileExplorer()
    {
        var extensions = new[] { new ExtensionFilter("Audio Files", "mp3", "wav", "ogg") };
        string[] paths = StandaloneFileBrowser.OpenFilePanel("Select Audio File", "", extensions, false);

        if (paths.Length > 0 && !string.IsNullOrEmpty(paths[0])) {
            StartCoroutine(EditorManager.instance.SaveAndLoadCustomAudioFile(paths[0]));
        }
    }

    // LOAD COVER BUTTON
    public void OpenCoverFileExplorer()
    {
        var extensions = new[] { new ExtensionFilter("Image Files", "png", "jpeg", "jpg") };
        string[] paths = StandaloneFileBrowser.OpenFilePanel("Select Cover File", "", extensions, false);
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

    // TEST GAMEPLAY BUTTON
    public void ToggleEditorPanels()
    {
        // Prevent toggling if animation is still playing
        if (panelSequence != null && panelSequence.IsActive() && panelSequence.IsPlaying())
            return;

        panelSequence = DOTween.Sequence();

        float duration = 1f;
        Ease ease = Ease.OutBounce;

        if (!isHidden) {
            panelSequence.Join(topPanel.DOAnchorPosY(topPanel.anchoredPosition.y + topPanel.rect.height, duration).SetEase(ease));
            panelSequence.Join(bottomPanel.DOAnchorPosY(bottomPanel.anchoredPosition.y - bottomPanel.rect.height, duration).SetEase(ease));
            panelSequence.Join(leftPanel.DOAnchorPosX(leftPanel.anchoredPosition.x - leftPanel.rect.width, duration).SetEase(ease));
            if (Metronome.instance.IsPaused()) Metronome.instance.PlaySong();
        }
        else {
            panelSequence.Join(topPanel.DOAnchorPosY(topPanel.anchoredPosition.y - topPanel.rect.height, duration).SetEase(ease));
            panelSequence.Join(bottomPanel.DOAnchorPosY(bottomPanel.anchoredPosition.y + bottomPanel.rect.height, duration).SetEase(ease));
            panelSequence.Join(leftPanel.DOAnchorPosX(leftPanel.anchoredPosition.x + leftPanel.rect.width, duration).SetEase(ease));
            Metronome.instance.PauseSong();
            EditorManager.instance.ReactivateNotes();
        }

        isHidden = !isHidden;
        testDummy.SetActive(isHidden);
    }
}
