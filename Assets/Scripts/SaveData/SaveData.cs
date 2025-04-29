using System.Collections.Generic;
using System.IO;
using UnityEngine;

public static class SaveData
{
    private static string customChartsPath = Path.Combine(Application.persistentDataPath, "custom_charts");

    public static List<(string, SongData)> LoadAllCustomSongs()
    {
        if (!Directory.Exists(customChartsPath)) return new List<(string, SongData)>();
        

        string[] songDirPaths = Directory.GetDirectories(customChartsPath);
        List<(string, SongData)> loadedSongs = new List<(string, SongData)>(songDirPaths.Length);
        for (int i = 0; i < songDirPaths.Length; i++) {
            loadedSongs.Add((songDirPaths[i], LoadCustomSong(songDirPaths[i])));
        }
        return loadedSongs;
    }

    public static SongData LoadCustomSong(string songDirPath)
    {
        SongData song = new SongData();
        if (Directory.Exists(songDirPath)) {
            string[] songDataFiles = Directory.GetFiles(songDirPath, "*.songdata");
            if (songDataFiles.Length > 0) {
                string songDataPath = songDataFiles[0];
                song = SongFileConverter.LoadFromTextFormat(songDataPath);
            }
        }

        return song;
    }

    /*
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

    public static void SaveCustomSong(SongData songData)
    {
        string songPath = GetOrCreateSongPath(songData);
    }*/

    public static string CreateSongPath()
    {
        if (!Directory.Exists(customChartsPath))
            Directory.CreateDirectory(customChartsPath);
        string songPath = Path.Combine(customChartsPath, "New Chart");
        int rev = 1;
        while (Directory.Exists(songPath)) {
            songPath = Path.Combine(customChartsPath, $"New Chart ({rev})");
            rev++;
        }
        return songPath;
    }

    public static string CreateSongPath(SongData songData)
    {
        if (!Directory.Exists(customChartsPath))
            Directory.CreateDirectory(customChartsPath);
        string songPath = Path.Combine(customChartsPath, $"{songData.songName} - {songData.artist}");
        int rev = 1;
        while (Directory.Exists(songPath)) {
            songPath = Path.Combine(customChartsPath, $"{songData.songName} - {songData.artist} ({rev})");
            rev++;
        }
        return songPath;
    }
}
