using FMODUnity;
using System.Collections.Generic;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    [SerializeField] private GameObject background;
    [field: Header("Menu Theme")]
    [field: SerializeField] public EventReference menuReference { get; private set; }
    [SerializeField] private BPMFlag menuBPM;
    private bool isPanelOpened;

    // OPEN MENU PANEL
    public void OpenPanel(GameObject panel)
    {
        panel.SetActive(true);
        background.SetActive(true);
        EditorManager em = NoteManager.instance as EditorManager;
        if (em != null) em.ClearSelection();
        isPanelOpened = true;
    }

    // CLOSE MENU PANEL
    public void ClosePanel(GameObject panel)
    {
        panel.SetActive(false);
        background.SetActive(false);
        isPanelOpened = false;
    }

    // PANEL OPENED CHECK
    public bool IsPanelOpened()
    {
        return isPanelOpened;
    }

    // LOAD SCENE
    public void OpenScene(string sceneName)
    {
        Metronome.instance.SetMetronomeSound(false);
        GameManager.instance.OpenScene(sceneName);
    }

    // RELOAD SCENE
    public void ReloadScene()
    {
        GameManager.instance.RestartLevel();
    }

    // PLAY MENU THEME
    public void PlayMenuTheme()
    {
        Metronome.instance.SetBPMFlags(new List<BPMFlag> { menuBPM });
        Metronome.instance.SetSong(menuReference);
        Metronome.instance.PlaySong();
    }
}
