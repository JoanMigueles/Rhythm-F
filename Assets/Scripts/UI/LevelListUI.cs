using FMODUnity;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class LevelListUI : UIManager
{
    public static LevelListUI instance { get; private set; }
    [SerializeField] SongPanel songPanelPrefab;
    [SerializeField] GameObject content;
    [SerializeField] GameObject noSongsSign;
    [SerializeField] CenteredSnapScroll scroll;
    private SongMetadata? hoveredSong;
    private Coroutine hoveredSongCoroutine;

    private void Awake()
    {
        instance = this;
    }

    private void Start()
    {
        PlayMenuTheme();
        ReloadList();
    }

    public void ReloadList()
    {
        foreach (Transform child in content.transform) {
            Destroy(child.gameObject);
        }
        hoveredSong = null;

        List<SongMetadata> songMetadataList = ResourceLoader.LoadAllSongsMetadata();
        List<SongMetadata> customSongMetadataList = SaveData.LoadAllCustomSongsMetadata();

        foreach (SongMetadata metadata in songMetadataList) {
            SongPanel panel = Instantiate(songPanelPrefab, content.transform);
            panel.SetScroller(scroll);
            panel.SetSongMetadata(metadata);
        }

        if (customSongMetadataList.Count > 0) {
            foreach (SongMetadata metadata in customSongMetadataList) {
                SongPanel panel = Instantiate(songPanelPrefab, content.transform);
                panel.SetScroller(scroll);
                panel.SetSongMetadata(metadata);
            }
        }

        if (noSongsSign != null) {
            noSongsSign.SetActive(customSongMetadataList.Count == 0);
            PlayMenuTheme();
        }

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

    private IEnumerator SetHoveredSongCoroutine(SongMetadata metadata)
    {
        hoveredSong = metadata;
        Metronome.instance.ReleasePlayers();
        yield return new WaitForSeconds(0.5f);

        if (metadata.songID == -1) {
            string audioPath = SaveData.GetAudioFilePath(metadata.audioFileName);
            if (File.Exists(audioPath)) {
                Metronome.instance.SetCustomSong(SaveData.GetAudioFilePath(audioPath));

                SongData song = SaveData.LoadCustomSong(metadata.localPath);
                Metronome.instance.SetBPMFlags(song.BPMFlags);
            }
            else {
                PlayMenuTheme();
            }
        } else {
            SongData song = ResourceLoader.LoadSong(metadata.songID);
            EventReference reference = ResourceLoader.LoadEventReference(metadata.songID);

            Metronome.instance.SetBPMFlags(song.BPMFlags);
            Metronome.instance.SetSong(reference);

        }
        

        yield return Metronome.instance.FadeIn(2f);
    }
}
