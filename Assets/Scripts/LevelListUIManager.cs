using UnityEngine;

public class LevelListUIManager : UIManager
{
    [SerializeField] SongPanel songPanelPrefab;
    [SerializeField] GameObject content;

    private void Start()
    {
        /*
        SongData[] songDataList = SaveData.LoadAllCustomSongs();
        foreach (SongData songData in songDataList) {
            SongPanel panel = Instantiate(songPanelPrefab, content.transform);
            panel.DisplaySongData(songData);
            panel.SetOpenSongFilePathListener();
        }*/
    }
}
