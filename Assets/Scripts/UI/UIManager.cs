using UnityEngine;

public class UIManager : MonoBehaviour
{
    [SerializeField] private GameObject background;

    // OPEN MENU PANEL
    public void OpenPanel(GameObject panel)
    {
        panel.SetActive(true);
        background.SetActive(true);
        if (NoteManager.instance != null ) NoteManager.instance.ClearSelection();
    }

    // CLOSE MENU PANEL
    public void ClosePanel(GameObject panel)
    {
        panel.SetActive(false);
        background.SetActive(false);
    }
}
