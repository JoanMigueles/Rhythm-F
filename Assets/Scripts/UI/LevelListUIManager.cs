using System.Collections.Generic;
using UnityEngine;

public class LevelListUIManager : UIManager
{
    [SerializeField] SongPanel songPanelPrefab;
    [SerializeField] GameObject content;
    [SerializeField] CenteredSnapScroll scroll;

    private void Start()
    {
        ReloadList();
    }

    public void ReloadList()
    {
        foreach (Transform child in content.transform) {
            Destroy(child.gameObject);
        }
        List<SongMetadata> songMetadataList = SaveData.LoadAllCustomSongsMetadata();

        foreach (SongMetadata metadata in songMetadataList) {
            SongPanel panel = Instantiate(songPanelPrefab, content.transform);
            panel.DisplaySongMetadata(metadata);
            panel.SetLoadSongFilePathListener(metadata.localPath);
        }

        scroll.SetItems();
    }

    public void CreateCustomSongButton()
    {
        SongDataManager.instance.CreateCustomSong();
        GameManager.instance.OpenEditor();
    }
}
