using System.IO;
using UnityEngine;

public static class SaveData
{
    private static string customChartsPath = Path.Combine(Application.persistentDataPath, "custom_charts");

    public static void LoadAllSongs()
    {
        if (!Directory.Exists(customChartsPath)) return;
        string[] songDirPaths = Directory.GetDirectories(customChartsPath);
        // Loop through each directory
        foreach (string songDirPath in songDirPaths) {
            LoadSong(songDirPath);
        }
    }

    public static void LoadSong(string songDirPath)
    {
        if (Directory.Exists(songDirPath));
    }

    public static string SaveAudioFile(SongData songData, string filePath)
    {
        string fileName = Path.GetFileName(filePath);
        string songPath = GetOrCreateSongPath(songData);
        string destinationPath = Path.Combine(songPath, fileName);

        if (!File.Exists(destinationPath)) {
            File.Copy(filePath, destinationPath);
            Debug.Log("File saved to: " + destinationPath);
        }

        return destinationPath;
    }

    public static void SaveSong(SongData songData)
    {
        string songPath = GetOrCreateSongPath(songData);
    }

    public static string GetOrCreateSongPath(SongData songData)
    {
        if (!Directory.Exists(customChartsPath))
            Directory.CreateDirectory(customChartsPath);
        string songPath = Path.Combine(customChartsPath, $"{songData.songName} - {songData.artist}");
        if (!Directory.Exists(songPath)) {
            Directory.CreateDirectory(songPath);
        }
        return songPath;
    }

    public static string CreateSongPath()
    {

    }
}
