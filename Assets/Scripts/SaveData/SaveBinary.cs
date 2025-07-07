using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;

public static class SaveBinary
{
    private static string path = Application.persistentDataPath + "/leaderboard.score";

    public static void SaveLeaderboards(LeaderboardDataManager data)
    {
        BinaryFormatter formatter = new BinaryFormatter();
        using (FileStream stream = new FileStream(path, FileMode.Create))
        {
            formatter.Serialize(stream, data);
        }
    }

    public static LeaderboardDataManager LoadLeaderboards()
    {
        if (!File.Exists(path))
        {
            Debug.LogWarning("Leaderboard file not found: " + path);
            return new LeaderboardDataManager(); // Return empty
        }

        BinaryFormatter formatter = new BinaryFormatter();
        using (FileStream stream = new FileStream(path, FileMode.Open))
        {
            return formatter.Deserialize(stream) as LeaderboardDataManager;
        }
    }
}
