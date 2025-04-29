using UnityEngine;

public class UIManager : MonoBehaviour
{
    [SerializeField] private GameObject background;

    public void OpenPanel(GameObject panel)
    {
        panel.SetActive(true);
        background.SetActive(true);
    }

    public void ClosePanel(GameObject panel)
    {
        panel.SetActive(false);
        background.SetActive(false);
    }
}
