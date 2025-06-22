using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;
    public List<Note> notes {  get; private set; }
    private SongMetadata? selectedSong;
    private Difficulty selectedDifficulty;
    private bool isPlaying;

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
    }

    public bool IsPlaying()
    {
        return isPlaying;
    }

    public void SetPlaying(bool playing)
    {
        isPlaying = playing;
    }

    public void RestartLevel()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    // EDITOR OPENING
    public void OpenScene(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }

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

    public void QuitGame()
    {
        Metronome.instance.ReleasePlayers();
        Application.Quit();
    }

    // SELECTED SONG
    public void SetSelectedSong(SongMetadata song)
    {
        selectedSong = song;
    }
    public void SetSelectedDifficulty(Difficulty diff)
    {
        selectedDifficulty = diff;
    }

    public SongMetadata? GetSelectedSong()
    {
        return selectedSong;
    }

    public bool IsSongSelected()
    {
        return selectedSong.HasValue;
    }

    // NOTES
    public void SetNotes(List<Note> notes)
    {
        this.notes = notes;
    }
}