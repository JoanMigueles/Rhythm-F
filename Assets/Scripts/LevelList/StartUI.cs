using DG.Tweening;
using FMODUnity;
using UnityEngine;

public class StartUI : UIManager
{
    public GameObject splashArt;
    private void Start()
    {
        PlayMenuTheme();
        splashArt.transform.DOMoveX(-11f, 0.5f)
            .SetRelative()
            .SetEase(Ease.InOutSine);
    }

    public void QuitButton()
    {
        GameManager.instance.QuitGame();
    }
}
