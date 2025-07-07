using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class LeaderboardEntry
{
    public string playerName;
    public int score;
    public float userAccuracy;

    public LeaderboardEntry (string player, int score, float userAccuracy)
    {
        playerName = player;
        this.score = score;
        this.userAccuracy = userAccuracy;
    }
}

[Serializable]
public class SongLeaderboardData
{
    public SongMetadata songMetadata;
    public Difficulty difficulty;
    public List<LeaderboardEntry> topScores = new List<LeaderboardEntry>(); // Top 5

    public SongLeaderboardData(SongMetadata songMetadata, Difficulty difficulty)
    {
        this.songMetadata = songMetadata;
        this.difficulty = difficulty;
        topScores = new List<LeaderboardEntry>();
    }

    public bool TryAddScore(LeaderboardEntry entry)
    {
        if (topScores.Count < 5) {
            topScores.Add(entry);
            topScores.Sort((a, b) => b.score.CompareTo(a.score));
            return true;
        }

        topScores.Sort((a, b) => b.score.CompareTo(a.score));

        if (topScores[topScores.Count - 1].score < entry.score) {
            topScores[topScores.Count - 1] = entry;

            topScores.Sort((a, b) => b.score.CompareTo(a.score));
            return true;
        }

        return false;
    }
}

[Serializable]
public class LeaderboardDataManager
{
    public List<SongLeaderboardData> leaderboardDataEntries;

    public LeaderboardDataManager()
    {
        leaderboardDataEntries = new List<SongLeaderboardData>();
    }

    public SongLeaderboardData GetLeaderboardData(int songID, Guid songGUID, Difficulty difficulty)
    {
        if (songID != -1) {
            return leaderboardDataEntries.Find(e =>
            e.songMetadata.songID == songID &&
            e.difficulty == difficulty);
        }
        return leaderboardDataEntries.Find(e =>
            e.songMetadata.songGUID == songGUID &&
            e.difficulty == difficulty
        );
    }

    public void RegisterScore(SongMetadata songMetadata, string player, int score, float accuracy, Difficulty difficulty)
    {
        SongLeaderboardData leaderboardData = GetLeaderboardData(songMetadata.songID, songMetadata.songGUID, difficulty);
        if (leaderboardData == null) {
            Debug.Log("New leaderboard");
            leaderboardData = new SongLeaderboardData(songMetadata, difficulty);
            leaderboardDataEntries.Add(leaderboardData);
        }


        Debug.Log("Adding score");
        leaderboardData.TryAddScore(new LeaderboardEntry(player, score, accuracy));
    }
}
