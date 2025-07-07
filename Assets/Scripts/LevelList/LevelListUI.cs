using FMODUnity;
using SFB;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LevelListUI : UIManager
{
    public static LevelListUI instance { get; private set; }
    [SerializeField] private SongPanel songPanelPrefab;
    [SerializeField] private GameObject content;
    [SerializeField] private GameObject noSongsSign;
    [SerializeField] private CenteredSnapScroll scroll;
    [SerializeField] private TMP_Text nameDisplay;
    [SerializeField] private TMP_Text artistDisplay;
    [SerializeField] private TMP_Text bpmDisplay;
    [SerializeField] private Image coverImage;

    [SerializeField] private bool customsOnly;
    private SongMetadata? hoveredSong;
    private List<SongPanel> songPanels;
    private Coroutine hoveredSongCoroutine;

    private void Awake()
    {
        instance = this;
    }

    private void Start()
    {
        ReloadList();
    }

    public void ReloadList()
    {
        songPanels = new List<SongPanel>();
        foreach (Transform child in content.transform) {
            Destroy(child.gameObject);
        }
        hoveredSong = null;

        List<SongMetadata> songMetadataList = ResourceLoader.LoadAllSongsMetadata();
        if (!customsOnly) {
            foreach (SongMetadata metadata in songMetadataList) {
                SongPanel panel = Instantiate(songPanelPrefab, content.transform);
                panel.SetScroller(scroll);
                panel.SetSongMetadata(metadata);
                
                songPanels.Add(panel);
            }
        }

        List<SongMetadata> customSongMetadataList = SaveData.LoadAllCustomSongsMetadata();
        foreach (SongMetadata metadata in customSongMetadataList) {
            SongPanel panel = Instantiate(songPanelPrefab, content.transform);
            panel.SetScroller(scroll);
            panel.SetSongMetadata(metadata);
            songPanels.Add(panel);
        }

        DisplayRanks();

        if (noSongsSign != null) {
            noSongsSign.SetActive(customSongMetadataList.Count == 0);
        }

        PlayMenuTheme();
        scroll.SetItems();
    }

    public void DeleteCustomSongButton()
    {
        Metronome.instance.ReleasePlayers();
        if (hoveredSong.HasValue)
            SaveData.RemoveCustomSongData(hoveredSong.Value);
        ReloadList();
    }

    public void SetHoveredSong(SongMetadata metadata)
    {
        if (hoveredSongCoroutine != null) {
            StopCoroutine(hoveredSongCoroutine);
        }
        hoveredSongCoroutine = StartCoroutine(SetHoveredSongCoroutine(metadata));
    }

    public void EditHoveredSong()
    {

        Metronome.instance.ReleasePlayers();
        if (hoveredSong.HasValue) {
            GameManager.instance.SetSelectedSong(hoveredSong.Value);
        } else {
            GameManager.instance.SetSelectedSong(null);
        }
        
        GameManager.instance.OpenScene("LevelEditor");
    }

    public void EditNewSong()
    {
        Metronome.instance.ReleasePlayers();
        GameManager.instance.SetSelectedSong(null);
        GameManager.instance.OpenScene("LevelEditor");
    }

    public void ImportSong()
    {
        var extensions = new[] { new ExtensionFilter("Rumble Files", "rmbl") };
        string[] paths = StandaloneFileBrowser.OpenFilePanel("Select Rumble File", "", extensions, false);

        if (paths.Length > 0 && !string.IsNullOrEmpty(paths[0])) {
            SongPacker.ExtractRmblFile(paths[0]);
        }
        ReloadList();
    }

    public void ExportHoveredSong()
    {
        if (!hoveredSong.HasValue) return;
        SongData songData = SaveData.LoadCustomSong(hoveredSong.Value.localPath);
        var extensionList = new[] {
            new ExtensionFilter("Rumble File", "rmbl"),
            new ExtensionFilter("All Files", "*")
        };

        string path = StandaloneFileBrowser.SaveFilePanel("Save File", "", $"{hoveredSong.Value.songName}", extensionList);
        if (!string.IsNullOrEmpty(path)) {
            if (Path.GetExtension(path).ToLower() != ".rmbl")
                path += ".rmbl";

            SongPacker.CreateRmblFile(songData, path);
        }
    }

    public void DisplayHoveredLeaderboard()
    {
        if (LeaderboardUI.instance == null) return;
        if (hoveredSong.HasValue)
            LeaderboardUI.instance.DisplayLeaderboard(GameManager.instance.GetTopScores(hoveredSong.Value));
    }

    public void DisplayRanks()
    {
        if (LeaderboardUI.instance == null) return;
        if (songPanels == null) return;
        foreach (SongPanel panel in songPanels) {
            panel.DisplayRank();
        }
    }

    private IEnumerator SetHoveredSongCoroutine(SongMetadata metadata)
    {
        hoveredSong = metadata;
        if (nameDisplay != null)
            nameDisplay.text = metadata.songName;
        if (artistDisplay != null)
            artistDisplay.text = metadata.artist;
        if (coverImage != null) {
            if (metadata.songID != -1)
                coverImage.sprite = ResourceLoader.LoadSongCover(metadata.songID);
            else
                coverImage.sprite = SaveData.GetCoverSprite(SaveData.GetCoverFilePath(metadata.coverFileName));
        }
        DisplayHoveredLeaderboard();

        Metronome.instance.ReleasePlayers();
        Metronome.instance.SetBPMFlags(new List<BPMFlag> { new BPMFlag(0, 0)});
        yield return new WaitForSeconds(0.5f);

        if (metadata.songID == -1) {
            string audioPath = SaveData.GetAudioFilePath(metadata.audioFileName);
            if (File.Exists(audioPath)) {
                SongData song = SaveData.LoadCustomSong(metadata.localPath);
                Metronome.instance.SetBPMFlags(song.BPMFlags);
                Metronome.instance.SetLooping(true);
                Metronome.instance.SetCustomSong(SaveData.GetAudioFilePath(audioPath));
                Metronome.instance.SetTimelinePosition(metadata.previewStartTime);
            }
            else {
                PlayMenuTheme();
            }
        } else {
            SongData song = ResourceLoader.LoadSong(metadata.songID);
            EventReference reference = ResourceLoader.LoadEventReference(metadata.songID);

            Metronome.instance.SetBPMFlags(song.BPMFlags);
            Metronome.instance.SetLooping(true);
            Metronome.instance.SetSong(reference);
            Metronome.instance.SetTimelinePosition(metadata.previewStartTime);
        }

        Metronome.instance.AddFade(true, 2f);
    }
}
