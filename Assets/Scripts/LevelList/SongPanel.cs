using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SongPanel : MonoBehaviour
{
    [SerializeField] TMP_Text titleText;
    [SerializeField] TMP_Text artistText;
    [SerializeField] Button button;
    public void DisplaySongMetadata(SongMetadata songMetadata) {
        titleText.text = songMetadata.songName;
        artistText.text = songMetadata.artist;
    }

    public void SetLoadSongFilePathListener(string songDirPath)
    {
        button.onClick.AddListener(() => SongDataManager.instance.SetCustomSelectedSong(songDirPath));
    }
}


