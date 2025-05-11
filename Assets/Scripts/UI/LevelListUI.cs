using System.Collections.Generic;
using UnityEngine;

public class LevelListUI : UIManager
{
    public static LevelListUI instance { get; private set; }
    [SerializeField] SongPanel songPanelPrefab;
    [SerializeField] GameObject content;
    [SerializeField] CenteredSnapScroll scroll;
    private SongMetadata hoveredSong;

    private void Awake()
    {
        instance = this;
    }

    private void Start()
    {
        GameManager.instance.SetSelectedSong("");
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
            panel.SetSongMetadata(metadata);
        }

        scroll.SetItems();
    }

    public void CreateCustomSongButton()
    {
        GameManager.instance.OpenEditor();
    }

    public void DeleteCustomSongButton()
    {
        SaveData.RemoveCustomSongData(hoveredSong);
        ReloadList();
    }

    public void SetHoveredSong(SongMetadata metadata)
    {
        hoveredSong = metadata;
    }
}
