using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Timeline;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;
    public float trackSpeed;
    private bool gameRunning;

    public Metronome metronome;
    public Marker marker;

    // Audio source
    public AudioSource musicSource;

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
        if (musicSource != null) {
            musicSource.Pause();
        }
    }

    public void ResumeGame()
    {
        gameRunning = true;
        if (musicSource != null) {
            musicSource.UnPause();
        }
    }

    public void RestartLevel()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}