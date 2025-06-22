using System;
using System.Collections.Generic;

[Serializable]
public class LeaderboardEntry
{
    public string playerName;
    public int score;
}

[Serializable]
public class SongLeaderboardData
{
    public string songName;
    public string songEditor;
    public string difficulty;
    public int userScore;
    public List<LeaderboardEntry> topScores = new List<LeaderboardEntry>(); // Top 3
}

[Serializable]
public class LeaderboardData
{
    public List<SongLeaderboardData> entries = new List<SongLeaderboardData>();

    public SongLeaderboardData GetEntry(string songName, string songEditor, string difficulty)
    {
        return entries.Find(e =>
            e.songName == songName &&
            e.songEditor == songEditor &&
            e.difficulty == difficulty
        );
    }

    public void AddOrUpdateEntry(SongLeaderboardData newEntry)
    {
        var existing = GetEntry(newEntry.songName, newEntry.songEditor, newEntry.difficulty);
        if (existing != null)
        {
            existing.userScore = newEntry.userScore;
            existing.topScores = newEntry.topScores;
        }
        else
        {
            entries.Add(newEntry);
        }
    }
}
