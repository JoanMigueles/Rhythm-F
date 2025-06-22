using FMODUnity;
using System.Collections.Generic;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    [SerializeField] private GameObject background;
    [field: Header("Menu Theme")]
    [field: SerializeField] public EventReference menuReference { get; private set; }
    [SerializeField] private BPMFlag menuBPM;
    public bool isPanelOpened {  get; private set; }

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

    // LOAD SCENE
    public void OpenScene(string sceneName)
    {
        GameManager.instance.OpenScene(sceneName);
    }

    public void PlayMenuTheme()
    {
        if (Metronome.instance.CurrentEventRef.Equals(menuReference)) return;

        Metronome.instance.SetSong(menuReference);
        Metronome.instance.SetBPMFlags(new List<BPMFlag> {menuBPM});
        Metronome.instance.PlaySong();
        StartCoroutine(Metronome.instance.FadeIn());
    }
}
