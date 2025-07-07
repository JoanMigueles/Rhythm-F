using DG.Tweening;
using FMODUnity;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SocialPlatforms.Impl;
using UnityEngine.UI;

public class GameplayUI : UIManager
{
    public static GameplayUI instance { get; private set; }
    [field: SerializeField] public EventReference winReference { get; private set; }
    [SerializeField] private BPMFlag winBPM;
    [field: SerializeField] public EventReference loseReference { get; private set; }

    // Character
    [SerializeField] private RhtyhmCharacterController robo;

    // Screens
    [SerializeField] private GameObject winScreen;
    [SerializeField] private GameObject loseScreen;
    [SerializeField] private GameObject pauseScreen;

    // Win results screen elements
    [SerializeField] private GameObject bottomBar;
    [SerializeField] private GameObject backButton;
    [SerializeField] private GameObject topBar;
    [SerializeField] private GameObject resultsBackground;
    [SerializeField] private GameObject roboSplash;
    [SerializeField] private GameObject resultsPanel;
    [SerializeField] private CanvasGroup[] resultTexts;
    [SerializeField] private CanvasGroup rankIcon;
    [SerializeField] private float startingsRankIconScale = 1.5f;

    // Rank sprites
    [SerializeField] private Sprite spRankSprite;
    [SerializeField] private Sprite sRankSprite;
    [SerializeField] private Sprite aRankSprite;
    [SerializeField] private Sprite bRankSprite;
    [SerializeField] private Sprite cRankSprite;
    [SerializeField] private Sprite dRankSprite;


    // Lose screen elements
    [SerializeField] private CanvasGroup loseBackground;
    [SerializeField] private CanvasGroup loseButtons;
 
    // Gameplay UI
    [SerializeField] private TMP_Text scoreDisplay;
    [SerializeField] private Slider comboSlider;
    [SerializeField] private TMP_Text comboMultiplierDisplay;
    [SerializeField] private Slider healthSlider;
    [SerializeField] private TMP_Text healthDisplay;
    [SerializeField] private Color comboHighlight;
    [SerializeField] private Color comboNormal;

    [SerializeField] private GameObject scorePanel;
    [SerializeField] private GameObject lifePanel;

    private Vector3 bottomBarTargetPos;
    private Vector3 topBarTargetPos;
    private Vector3 backgroundTargetPos;
    private Vector3 splashTargetPos;
    private Vector3 resultsPanelTargetPos;
    private Vector2[] resultTextTargetPositions;

    private void Awake()
    {
        instance = this;
    }

    void Start()
    {
        // Get the desired position for each element in the result screen (predetermined in the scene)
        bottomBarTargetPos = bottomBar.transform.localPosition;
        topBarTargetPos = topBar.transform.localPosition;
        backgroundTargetPos = resultsBackground.transform.localPosition;
        splashTargetPos = roboSplash.transform.position;
        resultsPanelTargetPos = resultsPanel.transform.localPosition;

        resultTextTargetPositions = new Vector2[resultTexts.Length];
        for (int i = 0; i < resultTexts.Length; i++) {
            RectTransform rt = resultTexts[i].GetComponent<RectTransform>();
            resultTextTargetPositions[i] = rt.anchoredPosition;
            resultTexts[i].alpha = 0;
        }

        // Move the result screen off the screen and hide it
        bottomBar.transform.localPosition = bottomBarTargetPos + Vector3.left * Screen.width;
        topBar.transform.localPosition = topBarTargetPos + Vector3.right * Screen.width;
        resultsBackground.transform.localPosition = backgroundTargetPos + Vector3.up * Screen.height;
        roboSplash.transform.position = splashTargetPos + Vector3.left * 10f;
        resultsPanel.transform.position = resultsPanelTargetPos + Vector3.right * Screen.width;

        rankIcon.transform.localScale = Vector3.one * startingsRankIconScale;
        rankIcon.alpha = 0f;
        backButton.SetActive(false);

        loseBackground.alpha = 0f;
        loseButtons.alpha = 0f;

        // Set the max slider values from the character controller params
        comboSlider.maxValue = robo.comboMultiplierInterval;
        healthSlider.maxValue = robo.maxHealth;
    }

    private void Update()
    {
        // For testing
        if (Input.GetKeyDown(KeyCode.W)) StartCoroutine(WinLevel());
        if (Input.GetKeyDown(KeyCode.Escape) && !robo.IsDead()) {
            TogglePauseScreen();
        }

        scoreDisplay.text = robo.GetScore().ToString();

        int multiplier = robo.GetComboMultiplier();
        comboMultiplierDisplay.text = "x" + multiplier.ToString();
        if (multiplier == robo.maxComboMultiplier) {
            comboSlider.value = robo.comboMultiplierInterval;
            comboSlider.fillRect.GetComponent<Image>().color = comboHighlight;
        } else {
            comboSlider.value = robo.GetCombo() % robo.comboMultiplierInterval;
            comboSlider.fillRect.GetComponent<Image>().color = comboNormal;
        }

        int health = robo.GetHealth();
        healthSlider.value = health;
        healthDisplay.text = health.ToString() + "/" + robo.maxHealth;
    }

    public IEnumerator WinLevel()
    {
        yield return new WaitForSeconds(2.5f);
        SetResults();
        NoteManager.instance.gameObject.SetActive(false);

        Metronome.instance.AddFade(false, 2f);
        yield return new WaitForSeconds(2f);

        Metronome.instance.SetBPMFlags(new List<BPMFlag> { winBPM });
        Metronome.instance.SetLooping(false);
        Metronome.instance.SetSong(winReference);
        Metronome.instance.PlaySong();

        ShowResults();
    }

    public IEnumerator LoseLevel()
    {
        NoteManager.instance.gameObject.SetActive(false);
        ShowLoseScreen();

        Metronome.instance.AddFade(false, 0.5f);
        yield return new WaitForSeconds(1f);

        Metronome.instance.SetBPMFlags(new List<BPMFlag>());
        Metronome.instance.SetLooping(false);
        Metronome.instance.SetSong(loseReference);
        Metronome.instance.PlaySong();
    }

    public void SetResults()
    {
        int score = robo.GetScore();
        (int perfects, int greats, int misses) = robo.GetHitStats();
        float accuracy = NoteManager.instance.GetAccuracy(perfects, greats, misses);
        GameManager.instance.RegisterScore(score, accuracy);

        // Score
        resultTexts[0].transform.GetChild(0).GetComponent<TMP_Text>().text = score.ToString();

        // Hit stats
        resultTexts[1].transform.GetChild(0).GetComponent<TMP_Text>().text = perfects.ToString();
        resultTexts[2].transform.GetChild(0).GetComponent<TMP_Text>().text = greats.ToString();
        resultTexts[3].transform.GetChild(0).GetComponent<TMP_Text>().text = misses.ToString();

        // Max combo
        resultTexts[4].transform.GetChild(0).GetComponent<TMP_Text>().text = robo.GetMaxCombo().ToString();

        // Accuracy
        resultTexts[5].transform.GetChild(0).GetComponent<TMP_Text>().text = accuracy.ToString("F1") + " %";
        Sprite rankSprite = GetRankIcon(accuracy);
        rankIcon.GetComponent<Image>().sprite = rankSprite;
    }

    public void ShowResults()
    {
        float duration = 1f;

        winScreen.SetActive(true);
        roboSplash.SetActive(true);

        bottomBar.transform.DOLocalMove(bottomBarTargetPos, duration/2).SetEase(Ease.OutCubic);
        topBar.transform.DOLocalMove(topBarTargetPos, duration/2).SetEase(Ease.OutCubic);
        resultsBackground.transform.DOLocalMove(backgroundTargetPos, duration)
            .SetEase(Ease.OutBounce)
            .OnComplete(() =>
            {
                roboSplash.transform.DOMove(splashTargetPos, duration).SetEase(Ease.OutCubic);
                resultsPanel.transform.DOLocalMove(resultsPanelTargetPos, duration).SetEase(Ease.OutCubic).OnComplete(() => {
                    Sequence textSeq = DOTween.Sequence();
                    float singleTextDuration = 0.3f;

                    for (int i = 0; i < resultTexts.Length; i++) {
                        RectTransform textTransform = resultTexts[i].GetComponent<RectTransform>();
                        textSeq.Append(resultTexts[i].DOFade(1f, singleTextDuration))
                            .Join(textTransform.DOAnchorPosY(resultTextTargetPositions[i].y, singleTextDuration)
                            .SetEase(Ease.OutQuad));
                    }

                    backButton.SetActive(true);
                    robo.gameObject.SetActive(false);
                    textSeq.AppendCallback(() =>
                    {
                        // Animate scale down to (1,1,1) and fade in
                        Sequence rankSeq = DOTween.Sequence();
                        rankSeq.Join(rankIcon.transform.DOScale(Vector3.one, 1f)
                            .SetEase(Ease.OutBounce))
                            .OnComplete(() => {
                                rankIcon.GetComponent<Pulsator>().enabled = true;
                            });
                        rankSeq.Join(rankIcon.DOFade(1f, 0.4f));
                    });
                });
            });
    }

    public void ShowLoseScreen()
    {
        float duration = 1f;

        loseScreen.SetActive(true);

        RectTransform scoreRect = scorePanel.GetComponent<RectTransform>();
        scoreRect.DOAnchorPosY(scoreRect.anchoredPosition.y + scoreRect.rect.height + 200, duration).SetEase(Ease.InOutCubic);

        RectTransform lifeRect = lifePanel.GetComponent<RectTransform>();
        lifeRect.DOAnchorPosY(lifeRect.anchoredPosition.y - lifeRect.rect.height - 200, duration).SetEase(Ease.InOutCubic);

        // Fade in canvas
        loseBackground.DOFade(1f, duration).OnComplete(() => {
            loseBackground.blocksRaycasts = true;
            loseBackground.interactable = true;
            loseButtons.DOFade(1f, duration).OnComplete(() => {
                loseButtons.blocksRaycasts = true;
                loseButtons.interactable = true;
            });
        });
    }
    
    public void TogglePauseScreen()
    {
        bool paused = GameManager.instance.TogglePause();
        pauseScreen.SetActive(paused);
    }

    private Sprite GetRankIcon(float accuracy)
    {
        if (accuracy >= 100) return spRankSprite;
        else if (accuracy >= 95) return sRankSprite;
        else if (accuracy >= 90) return aRankSprite;
        else if (accuracy >= 85) return bRankSprite;
        else if (accuracy >= 72) return cRankSprite;
        else return dRankSprite;
    }

    private void OnDestroy()
    {
        DOTween.KillAll();
    }
}
