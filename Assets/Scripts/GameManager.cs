using UnityEngine;
using UnityEngine.SceneManagement;

// GameManager handles all the persistent data between scenes and the navigation between scenes too
public class GameManager : MonoBehaviour
{
    public static GameManager instance { get; private set; }

    private SongMetadata? selectedSong;
    private Difficulty selectedDifficulty;
    private bool isPlaying;
    private bool wasPaused;

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
        Application.targetFrameRate = 140; // Sin límite de framerate
        selectedSong = null;
        isPlaying = false;
        wasPaused = true;
    }

    // ISPLAYING: Only true if we're on the gameplay screen or testing in the editor
    public bool IsPlaying()
    {
        return isPlaying;
    }

    public void SetPlaying(bool playing)
    {
        isPlaying = playing;
    }

    // Restart the level from the retry button
    public void RestartLevel()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    // Scene loading by name
    public void OpenScene(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }

    // Scene loading by stage type (only for levels)
    public void OpenLevelScene(Stage stage)
    {
        string sceneName = "";
        switch (stage) {
            case Stage.City:
                sceneName = "CityLevel";
                break;
            case Stage.Beach:
                sceneName = "BeachLevel";
                break;
            case Stage.Future:
                sceneName = "FutueLevel";
                break;
        }

        Metronome.instance.ReleasePlayers();
        SceneManager.LoadScene(sceneName);
    }

    // Quitting the game
    public void QuitGame()
    {
        Metronome.instance.ReleasePlayers();
        Application.Quit();
    }

    // SELECTED SONG: to know in any scene the song selected in the song list scene
    public void SetSelectedSong(SongMetadata? song)
    {
        selectedSong = song;
    }

    public SongMetadata? GetSelectedSong()
    {
        return selectedSong;
    }

    public bool IsSongSelected()
    {
        return selectedSong.HasValue;
    }

    // SELECTED DIFFICULTY: to know in any scene the difficulty selected in the song list scene
    public void SetSelectedDifficulty(Difficulty diff)
    {
        selectedDifficulty = diff;
    }

    public Difficulty GetSelectedDifficulty()
    {
        return selectedDifficulty;
    }

    public void TogglePause()
    {
        if (isPlaying) {
            Time.timeScale = 0f;
            SetPlaying(false);
            wasPaused = Metronome.instance.IsPaused();
            if (!wasPaused)
                Metronome.instance.PauseSong();
        } else {
            Time.timeScale = 1f;
            SetPlaying(true);
            if (!wasPaused)
                Metronome.instance.PlaySong();
        }
        
    }
}