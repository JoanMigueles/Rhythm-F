using DG.Tweening;
using SFB;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EditorUI : UIManager
{
    public static EditorUI instance {  get; private set; }
    public bool isHidden;

    [SerializeField] private TMP_Text timer;
    [SerializeField] private TMP_Text beat;
    [SerializeField] private TMP_Text subdivision;
    [SerializeField] private TMP_Text songTitle;
    [SerializeField] private TMP_Text currentBPM;
    [SerializeField] private Image coverImage;

    [SerializeField] private TMP_Text stageName;
    [SerializeField] private SpriteRenderer stageBackground;
    [SerializeField] private Sprite[] stageSprites;

    [SerializeField] private TMP_InputField titleField;
    [SerializeField] private TMP_InputField artistField;
    [SerializeField] private TMP_Text audioField;

    [SerializeField] private Slider subdivisionSlider;
    private readonly int[] valueMap = { 1, 1, 2, 3, 4, 5, 6, 7, 8, 9, 12, 16 }; // Subdivision slider values
    [SerializeField] private SongSlider songSlider;
    [SerializeField] private WaveformTexture[] waveformTextures;

    [SerializeField] private Button playButton;
    [SerializeField] private Button pauseButton;

    [SerializeField] private RectTransform topPanel;
    [SerializeField] private RectTransform bottomPanel;
    [SerializeField] private RectTransform leftPanel;
    [SerializeField] private CanvasGroup actionPopup;
    private Sequence panelSequence;
    private ButtonModeHighlight currentModeButton;

    [SerializeField] private GameObject testDummy;

    private EditorManager em;

    private void Awake()
    {
        instance = this;
    }

    private void Start()
    {
        subdivisionSlider.minValue = 0;
        subdivisionSlider.maxValue = valueMap.Length - 1;
        subdivisionSlider.value = 2;
        em = NoteManager.instance as EditorManager;
    }

    void Update()
    {
        // UPDATE TIMERS
        timer.text = FormatTimeMS(Metronome.instance.GetTimelinePosition());
        beat.text = Metronome.instance.GetTimelineBeatPosition().ToString("F2");
        currentBPM.text = Metronome.instance.GetCurrentBPM().ToString();
        
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

    // HIGHLIGHTED MODE
    public void SetHighlightedButton(ButtonModeHighlight button)
    {
        if (currentModeButton != null) {
            currentModeButton.Highlight(false);
        }

        currentModeButton = button;
        currentModeButton.Highlight(true);
    }

    // SONG DATA DISPLAY
    public void DisplaySongData(SongMetadata metadata)
    {
        titleField.SetTextWithoutNotify(metadata.songName);
        artistField.SetTextWithoutNotify(metadata.artist);
        audioField.text = metadata.audioFileName;

        SetSongTitleDisplay(metadata.songName, metadata.artist);
        SetBackgroundDisplay(metadata.stage);
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
    
    private void SetBackgroundDisplay(Stage stage)
    {
        stageName.text = stage.ToString();
        stageBackground.sprite = stageSprites[(int)stage % stageSprites.Length];
    }

    // SUBDIVISION SLIDER
    public void SetBeatSubdivisionSnapping(float subdivisionIndex)
    {
        int i = Mathf.RoundToInt(subdivisionIndex);

        if (subdivisionIndex == 0) {
            em.beatSnapping = false;
            subdivision.text = "None";
        } else {
            em.beatSnapping = true;
            subdivision.text = "1/" + valueMap[i].ToString();
        }

        em.noteSubdivisionSnapping = valueMap[i];
    }

    public void DisplayCoverImage(Sprite sprite)
    {
        coverImage.sprite = sprite;
    }

    // METRONOME TOGGLE
    public void MetronomeToggle(bool on)
    {
        Metronome.instance.SetMetronomeSound(on);
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

    // TEXT POPUP
    public void ActionPopup(string message)
    {
        actionPopup.transform.GetChild(0).GetComponent<TMP_Text>().text = message;
        actionPopup.alpha = 1.0f;
        actionPopup.DOKill();
        actionPopup.DOFade(0f, 2f).SetEase(Ease.InOutQuad);
    }

    // LOAD AUDIO BUTTON
    public void OpenAudioFileExplorer()
    {
        var extensions = new[] { new ExtensionFilter("Audio Files", "mp3", "wav", "ogg") };
        string[] paths = StandaloneFileBrowser.OpenFilePanel("Select Audio File", "", extensions, false);

        if (paths.Length > 0 && !string.IsNullOrEmpty(paths[0])) {
            StartCoroutine(em.SaveAndLoadCustomAudioFile(paths[0]));
        }
    }

    // LOAD COVER BUTTON
    public void OpenCoverFileExplorer()
    {
        var extensions = new[] { new ExtensionFilter("Image Files", "png", "jpeg", "jpg") };
        string[] paths = StandaloneFileBrowser.OpenFilePanel("Select Cover File", "", extensions, false);
        if (paths.Length > 0 && !string.IsNullOrEmpty(paths[0])) {
            StartCoroutine(em.SaveAndLoadCoverFile(paths[0]));
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
            panelSequence.Join(topPanel.DOAnchorPosY(topPanel.anchoredPosition.y + topPanel.rect.height + 200, duration).SetEase(ease));
            panelSequence.Join(bottomPanel.DOAnchorPosY(bottomPanel.anchoredPosition.y - bottomPanel.rect.height - 200, duration).SetEase(ease));
            panelSequence.Join(leftPanel.DOAnchorPosX(leftPanel.anchoredPosition.x - leftPanel.rect.width, duration).SetEase(ease));
            if (Metronome.instance.IsPaused()) Metronome.instance.PlaySong();
            em.SetTestingData();
        }
        else {
            panelSequence.Join(topPanel.DOAnchorPosY(topPanel.anchoredPosition.y - topPanel.rect.height - 200, duration).SetEase(ease));
            panelSequence.Join(bottomPanel.DOAnchorPosY(bottomPanel.anchoredPosition.y + bottomPanel.rect.height + 200, duration).SetEase(ease));
            panelSequence.Join(leftPanel.DOAnchorPosX(leftPanel.anchoredPosition.x + leftPanel.rect.width, duration).SetEase(ease));
            Metronome.instance.PauseSong();
            em.ReactivateNotes();
        }

        isHidden = !isHidden;
        GameManager.instance.SetPlaying(isHidden);
        testDummy.SetActive(isHidden);
    }

    private void OnDestroy()
    {
        actionPopup.DOKill();
    }
}
