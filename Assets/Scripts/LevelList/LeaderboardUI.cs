using UnityEngine;
using TMPro;
using Steamworks;
using System.Collections.Generic;

public class LeaderboardUI : MonoBehaviour
{
    public static LeaderboardUI instance;

    [Header("UI Fields")]
    public TMP_Text nameUser;
    public TMP_Text scoreUser;

    public TMP_Text nameTop1;
    public TMP_Text scoreTop1;

    public TMP_Text nameTop2;
    public TMP_Text scoreTop2;

    public TMP_Text nameTop3;
    public TMP_Text scoreTop3;

    private void Awake()
    {
        instance = this;
    }

    void Start()
    {
        if (SteamManager.Initialized) {
            nameUser.text = SteamFriends.GetPersonaName();
        }
        else {
            nameUser.text = "Guest";
            Debug.LogWarning("Steam is not initialized. Running in offline mode.");
        }

        ClearAllFields();
    }

    public void DisplayLeaderboard(int userScore, List<LeaderboardEntry> topScores)
    {
        ClearAllFields();

        scoreUser.text = userScore.ToString();

        if (topScores == null) return;

        if (topScores.Count > 0)
            SetTopEntry(nameTop1, scoreTop1, topScores[0].playerName, topScores[0].score.ToString());
        if (topScores.Count > 1)
            SetTopEntry(nameTop2, scoreTop2, topScores[1].playerName, topScores[1].score.ToString());
        if (topScores.Count > 2)
            SetTopEntry(nameTop3, scoreTop3, topScores[2].playerName, topScores[2].score.ToString());
    }

    void SetTopEntry(TMP_Text nameField, TMP_Text scoreField, string name, string score)
    {
        nameField.text = name;
        scoreField.text = score;
    }

    void ClearAllFields()
    {
        scoreUser.text = "-";
        SetTopEntry(nameTop1, scoreTop1, "-", "-");
        SetTopEntry(nameTop2, scoreTop2, "-", "-");
        SetTopEntry(nameTop3, scoreTop3, "-", "-");
    }
}
