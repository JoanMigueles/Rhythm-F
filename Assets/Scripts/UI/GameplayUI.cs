using DG.Tweening;
using FMODUnity;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameplayUI : UIManager
{

    [field: SerializeField] public EventReference winReference { get; private set; }
    [SerializeField] private BPMFlag winBPM;
    [SerializeField] private RhtyhmCharacterController robo;
    [SerializeField] private GameObject bottomBar;
    [SerializeField] private GameObject backButton;
    [SerializeField] private GameObject topBar;
    [SerializeField] private GameObject resultsBackground;
    [SerializeField] private GameObject roboSplash;
    [SerializeField] private GameObject resultsPanel;
    [SerializeField] private GameObject rankIcon;
    [SerializeField] private float startingsRankIconScale = 1.5f;
    [SerializeField] private TMP_Text[] resultTexts;

    [SerializeField] private TMP_Text scoreDisplay;
    [SerializeField] private Slider comboSlider;
    [SerializeField] private TMP_Text comboMultiplierDisplay;
    [SerializeField] private Slider healthSlider;
    [SerializeField] private TMP_Text healthDisplay;
    [SerializeField] private Color comboHighlight;
    [SerializeField] private Color comboNormal;

    private Vector3 bottomBarTargetPos;
    private Vector3 topBarTargetPos;
    private Vector3 backgroundTargetPos;
    private Vector3 splashTargetPos;
    private Vector3 resultsPanelTargetPos;
    private Vector2[] resultTextTargetPositions;

    private bool songEnded;

    void Start()
    {
        songEnded = false;

        // Get the desired position for each element in the result screen (predetermined in the scene)
        bottomBarTargetPos = bottomBar.transform.localPosition;
        topBarTargetPos = topBar.transform.localPosition;
        backgroundTargetPos = resultsBackground.transform.localPosition;
        splashTargetPos = roboSplash.transform.position;
        resultsPanelTargetPos = resultsPanel.transform.localPosition;

        // Move the result screen off the screen and hide it
        bottomBar.transform.localPosition = bottomBarTargetPos + Vector3.left * Screen.width;
        topBar.transform.localPosition = topBarTargetPos + Vector3.right * Screen.width;
        resultsBackground.transform.localPosition = backgroundTargetPos + Vector3.up * Screen.height;
        roboSplash.transform.position = splashTargetPos + Vector3.left * 10f;
        resultsPanel.transform.position = resultsPanelTargetPos + Vector3.right * Screen.width;
        resultTextTargetPositions = new Vector2[resultTexts.Length];

        CanvasGroup canvasGroup = rankIcon.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = rankIcon.AddComponent<CanvasGroup>();
        rankIcon.transform.localScale = Vector3.one * startingsRankIconScale;
        canvasGroup.alpha = 0f;
        backButton.SetActive(false);

        for (int i = 0; i < resultTexts.Length; i++) {
            RectTransform rt = resultTexts[i].rectTransform;
            resultTextTargetPositions[i] = rt.anchoredPosition;

            TMP_Text childText = resultTexts[i].transform.GetChild(0).GetComponent<TMP_Text>();
            rt.anchoredPosition += Vector2.up * 30f;
            resultTexts[i].alpha = 0f;
            childText.alpha = 0f;
        }

        // Set the max slider values from the character controller params
        comboSlider.maxValue = robo.comboMultiplierInterval;
        healthSlider.maxValue = robo.maxHealth;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.W)) ShowResults();
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

        if (NoteManager.instance.IsPastLastNote() && !songEnded) {
            songEnded = true;
            StartCoroutine(EndLevel());
        }
    }

    private IEnumerator EndLevel()
    {
        yield return new WaitForSeconds(2.5f);
        SetResults();
        yield return Metronome.instance.FadeOut();
        ShowResults();
    }

    public void SetResults()
    {
        // Score
        resultTexts[0].transform.GetChild(0).GetComponent<TMP_Text>().text = robo.GetScore().ToString();

        // Hit stats
        (int perfects, int greats, int misses) = robo.GetHitStats();
        resultTexts[1].transform.GetChild(0).GetComponent<TMP_Text>().text = perfects.ToString();
        resultTexts[2].transform.GetChild(0).GetComponent<TMP_Text>().text = greats.ToString();
        resultTexts[3].transform.GetChild(0).GetComponent<TMP_Text>().text = misses.ToString();

        // Max combo
        resultTexts[4].transform.GetChild(0).GetComponent<TMP_Text>().text = robo.GetMaxCombo().ToString();

        // Accuracy
        resultTexts[5].transform.GetChild(0).GetComponent<TMP_Text>().text = NoteManager.instance.GetAccuracy(perfects, greats, misses).ToString();

    }

    public void ShowResults()
    {
        float duration = 1f;

        Metronome.instance.SetSong(winReference);
        Metronome.instance.SetBPMFlags(new List<BPMFlag>{ winBPM });
        Metronome.instance.PlaySong();
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
                        int index = i;

                        TMP_Text parentText = resultTexts[index];
                        TMP_Text childText = parentText.transform.GetChild(0).GetComponent<TMP_Text>();

                        textSeq.Append(parentText.DOFade(1f, singleTextDuration))
                            .Join(childText.DOFade(1f, singleTextDuration))
                            .Join(parentText.rectTransform
                            .DOAnchorPosY(resultTextTargetPositions[index].y, singleTextDuration)
                            .SetEase(Ease.OutQuad));
                    }
                    backButton.SetActive(true);
                    robo.gameObject.SetActive(false);
                    textSeq.AppendCallback(() =>
                    {
                        Image rankImage = rankIcon.GetComponent<Image>();
                        CanvasGroup canvasGroup = rankIcon.GetComponent<CanvasGroup>();
                        if (canvasGroup == null)
                            canvasGroup = rankIcon.AddComponent<CanvasGroup>();

                        // Reset state
                        rankIcon.transform.localScale = Vector3.one * startingsRankIconScale;
                        canvasGroup.alpha = 0f;

                        // Animate scale down to (1,1,1) and fade in
                        Sequence rankSeq = DOTween.Sequence();
                        rankSeq.Join(rankIcon.transform.DOScale(Vector3.one, 1f)
                            .SetEase(Ease.OutBounce))
                            .OnComplete(() => {
                                PulsatorManager.instance.AddPulsator(rankImage.GetComponent<Pulsator>());
                            });
                        rankSeq.Join(canvasGroup.DOFade(1f, 0.4f));
                    });
                });
            });
    }

}
