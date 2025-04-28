using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Timeline;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;
    private string selectedSong;
    private bool gameRunning;

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

    public void SetSelectedSong()
    {
        selectedSong = string.Empty;
    }
    public string GetSelectedSong()
    {
        return selectedSong;
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}