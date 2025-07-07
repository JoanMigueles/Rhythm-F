using UnityEngine;
using TMPro;
using Steamworks;
using System.Collections.Generic;

public class LeaderboardUI : MonoBehaviour
{
    public static LeaderboardUI instance;

    [Header("UI Fields")]
    public TMP_Text[] usernames;
    public TMP_Text[] scores;

    private void Awake()
    {
        instance = this;
    }

    void Start()
    {
        ClearAllFields();
    }

    public void DisplayLeaderboard(List<LeaderboardEntry> topScores)
    {
        ClearAllFields();

        if (topScores == null) return;

        for (int i = 0; i < topScores.Count; i++) {
            SetTopEntry(usernames[i], scores[i], topScores[i].playerName, topScores[i].score.ToString());
        }
    }

    void SetTopEntry(TMP_Text nameField, TMP_Text scoreField, string name, string score)
    {
        nameField.text = name;
        scoreField.text = score;
    }

    void ClearAllFields()
    {
        for (int i = 0; i < usernames.Length; i++) {
            SetTopEntry(usernames[i], scores[i], "-", "-");
        }
    }
}
