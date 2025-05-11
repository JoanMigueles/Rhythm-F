using UnityEngine;

public class UIManager : MonoBehaviour
{
    [SerializeField] private GameObject background;
    public bool isPanelOpened {  get; private set; }

    // OPEN MENU PANEL
    public void OpenPanel(GameObject panel)
    {
        panel.SetActive(true);
        background.SetActive(true);
        if (EditorManager.instance != null ) EditorManager.instance.ClearSelection();
        isPanelOpened = true;
    }

    // CLOSE MENU PANEL
    public void ClosePanel(GameObject panel)
    {
        panel.SetActive(false);
        background.SetActive(false);
        isPanelOpened = false;
    }
}
