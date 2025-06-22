using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;

public static class SaveBinary
{
    private static string path = Application.persistentDataPath + "/leaderboard.score";

    public static void SaveLeaderboard(LeaderboardData data)
    {
        BinaryFormatter formatter = new BinaryFormatter();
        using (FileStream stream = new FileStream(path, FileMode.Create))
        {
            formatter.Serialize(stream, data);
        }
    }

    public static LeaderboardData LoadLeaderboard()
    {
        if (!File.Exists(path))
        {
            Debug.LogError("Leaderboard file not found: " + path);
            return new LeaderboardData(); // Return empty
        }

        BinaryFormatter formatter = new BinaryFormatter();
        using (FileStream stream = new FileStream(path, FileMode.Open))
        {
            return formatter.Deserialize(stream) as LeaderboardData;
        }
    }
}
