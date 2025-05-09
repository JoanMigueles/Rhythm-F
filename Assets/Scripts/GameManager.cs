using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Timeline;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;
    private string selectedSong;
    private bool gameRunning;
    private bool isNew;

    private void Awake()
    {
        if (instance == null) {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        Application.targetFrameRate = -1; // Sin límite de framerate
        gameRunning = true;
    }

    public bool IsGameRunning()
    {
        return gameRunning;
    }

    public void PauseGame()
    {
        gameRunning = false;
    }

    public void ResumeGame()
    {
        gameRunning = true;
    }

    public void RestartLevel()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    // EDITOR OPENING
    public void OpenEditor()
    {
        SceneManager.LoadScene("LevelEditor");
    }

    // LIST OPENING
    public void OpenSongList()
    {
        SceneManager.LoadScene("List");
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}