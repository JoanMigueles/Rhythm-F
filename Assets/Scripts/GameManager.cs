using Steamworks;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// GameManager handles all the persistent data between scenes and the navigation between scenes too
public class GameManager : MonoBehaviour
{
    public static GameManager instance { get; private set; }

    private string sessionUsername;
    private LeaderboardDataManager leaderboardManager;
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
        Application.targetFrameRate = 140;
        leaderboardManager = SaveBinary.LoadLeaderboards();
        selectedSong = null;
        isPlaying = false;
        wasPaused = true;

        if (SteamManager.Initialized) {
            sessionUsername = SteamFriends.GetPersonaName();
        }
        else {
            sessionUsername = "Guest";
        }
    }

    public void RegisterScore(int score, float accuracy)
    {
        if (!selectedSong.HasValue) return;
        if (leaderboardManager == null) {
            Debug.Log("Registering score (null manager)");
            leaderboardManager = new LeaderboardDataManager();
        }

        Debug.Log("Registering score...");

        leaderboardManager.RegisterScore(selectedSong.Value, sessionUsername, score, accuracy, selectedDifficulty);
        SaveBinary.SaveLeaderboards(leaderboardManager);
    }

    public List<LeaderboardEntry> GetTopScores(SongMetadata metadata)
    {
        if (leaderboardManager == null) {
            leaderboardManager = new LeaderboardDataManager();
        }

        SongLeaderboardData songLeaderboard = leaderboardManager.GetLeaderboardData(metadata.songID, metadata.songGUID, selectedDifficulty);
        if (songLeaderboard == null) {
            return new List<LeaderboardEntry>();
        }
        return songLeaderboard.topScores;
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
        isPlaying = false;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    // Scene loading by name
    public void OpenScene(string sceneName)
    {
        isPlaying = false;
        SceneManager.LoadScene(sceneName);
    }

    // Scene loading by stage type (only for levels)
    public void OpenLevelScene(Stage stage)
    {
        isPlaying = false;
        string sceneName = "";
        switch (stage) {
            case Stage.City:
                sceneName = "CityLevel";
                break;
            case Stage.Beach:
                sceneName = "BeachLevel";
                break;
            case Stage.Future:
                sceneName = "FutureLevel";
                break;
        }

        Metronome.instance.ReleasePlayers();
        SceneManager.LoadScene(sceneName);
    }

    // Quitting the game
    public void QuitGame()
    {
        isPlaying = false;
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

    public bool TogglePause()
    {
        if (isPlaying) {
            Time.timeScale = 0f;
            SetPlaying(false);
            wasPaused = Metronome.instance.IsPaused();
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
            if (!wasPaused)
                Metronome.instance.PauseSong();
        } else {
            Time.timeScale = 1f;
            SetPlaying(true);
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
            if (!wasPaused)
                Metronome.instance.PlaySong();
        }
        return !isPlaying;
    }
}