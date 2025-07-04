using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SongPanel : MonoBehaviour
{
    public bool openEditor;
    [SerializeField] TMP_Text titleText;
    [SerializeField] TMP_Text artistText;
    [SerializeField] Image coverImage;
    [SerializeField] Button button;
    [SerializeField] private Color selectedColor;
    [SerializeField] private Color color;

    private CenteredSnapScroll scroll;
    private SongMetadata metadata;
    private bool isHovered;

    public void SetScroller(CenteredSnapScroll scroller)
    {
        scroll = scroller;
    }

    public void SetSongMetadata(SongMetadata songMetadata) {
        metadata = songMetadata;
        titleText.text = songMetadata.songName;
        if (songMetadata.songID != -1) titleText.text += "(*)";
        artistText.text = songMetadata.artist;
        coverImage.sprite = SaveData.GetCoverSprite(SaveData.GetCoverFilePath(metadata.coverFileName));
        button.onClick.AddListener(() => {
            if (isHovered) {
                Metronome.instance.ReleasePlayers();
                GameManager.instance.SetSelectedSong(songMetadata);
                if (openEditor)
                    GameManager.instance.OpenScene("LevelEditor");
                else GameManager.instance.OpenLevelScene(metadata.stage);
            } else {
                RectTransform rt = GetComponent<RectTransform>();
                scroll.SnapToItem(rt, true);
            }
        });
    }

    public SongMetadata GetSongMetadata()
    {
        return metadata;
    }

    public void SetHovered(bool hovered)
    {
        isHovered = hovered;
        Pulsator pulse = GetComponent<Pulsator>();
        pulse.enabled = hovered;
        GetComponent<Image>().color = hovered ? selectedColor : color;
    }
}


