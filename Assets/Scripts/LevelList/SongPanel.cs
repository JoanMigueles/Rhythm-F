using System.Collections.Generic;
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

    // Rank sprites
    [SerializeField] private Image rankIcon;
    [SerializeField] private Sprite spRankSprite;
    [SerializeField] private Sprite sRankSprite;
    [SerializeField] private Sprite aRankSprite;
    [SerializeField] private Sprite bRankSprite;
    [SerializeField] private Sprite cRankSprite;
    [SerializeField] private Sprite dRankSprite;

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
        if (songMetadata.songID != -1) {
            coverImage.sprite = ResourceLoader.LoadSongCover(songMetadata.songID);
        } else {
            coverImage.sprite = SaveData.GetCoverSprite(SaveData.GetCoverFilePath(metadata.coverFileName));
        }
        artistText.text = songMetadata.artist;
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

    public void DisplayRank()
    {
        if (rankIcon == null) return;

        List<LeaderboardEntry> topScores = GameManager.instance.GetTopScores(metadata);
        if (topScores.Count == 0) {
            rankIcon.sprite = null;
            rankIcon.gameObject.SetActive(false);
            return;
        }

        LeaderboardEntry bestEntry = topScores[0];
        if (bestEntry == null) {
            rankIcon.sprite = null;
            rankIcon.gameObject.SetActive(false);
            return;
        }

        rankIcon.gameObject.SetActive(true);
        rankIcon.sprite = GetRankIcon(bestEntry.userAccuracy);
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

    private Sprite GetRankIcon(float accuracy)
    {
        if (accuracy >= 100) return spRankSprite;
        else if (accuracy >= 95) return sRankSprite;
        else if (accuracy >= 90) return aRankSprite;
        else if (accuracy >= 85) return bRankSprite;
        else if (accuracy >= 72) return cRankSprite;
        else return dRankSprite;
    }
}


