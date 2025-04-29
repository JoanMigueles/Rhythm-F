using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SongPanel : MonoBehaviour
{
    [SerializeField] TMP_Text titleText;
    [SerializeField] TMP_Text artistText;
    [SerializeField] Button button;
    public void DisplaySongData(SongData songData) {
        titleText.text = songData.songName;
        artistText.text = songData.artist;
    }

    public void SetOpenSongFilePathListener(string songPath)
    {

        button.onClick.AddListener(() => OpenSongFilePath(songPath));
    }

    private void OpenSongFilePath(string songPath)
    {
        
    }
}


