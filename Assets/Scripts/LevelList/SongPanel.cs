using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SongPanel : MonoBehaviour
{
    [SerializeField] TMP_Text titleText;
    [SerializeField] TMP_Text artistText;
    [SerializeField] Button button;
    private SongMetadata metadata;

    public void SetSongMetadata(SongMetadata songMetadata) {
        metadata = songMetadata;
        titleText.text = songMetadata.songName;
        artistText.text = songMetadata.artist;
        button.onClick.AddListener(() => {
            GameManager.instance.SetSelectedSong(songMetadata.localPath);
            GameManager.instance.OpenEditor();
        });
    }
    public SongMetadata GetSongMetadata()
    {
        return metadata;
    }
}


