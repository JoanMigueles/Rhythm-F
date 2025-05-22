using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;
    public List<Note> notes {  get; private set; }
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
        Application.targetFrameRate = 140; // Sin límite de framerate
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
        Metronome.instance.ReleaseCustomPlayer();
        SceneManager.LoadScene("LevelEditor");
    }

    // LIST OPENING
    public void OpenSongList()
    {
        Metronome.instance.ReleaseCustomPlayer();
        SceneManager.LoadScene("List");
    }

    public void QuitGame()
    {
        Metronome.instance.ReleaseCustomPlayer();
        Application.Quit();
    }

    // SELECTED SONG
    public void SetSelectedSong(string songPath)
    {
        selectedSong = songPath;
    }

    public string GetSelectedSong()
    {
        return selectedSong;
    }

    public bool IsSongSelected()
    {
        return !string.IsNullOrEmpty(selectedSong);
    }

    // NOTES
    public void SetNotes(List<Note> notes)
    {
        this.notes = notes;
    }
}