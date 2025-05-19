using UnityEngine;

public class StartUI : MonoBehaviour
{
    public void PlayButton()
    {
        GameManager.instance.OpenSongList();
    }

    public void QuitButton()
    {
        GameManager.instance.QuitGame();
    }
}
