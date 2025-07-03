using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using UnityEngine;

public static class SongPacker
{
    private static string customChartsPath = Path.Combine(Application.persistentDataPath, "custom_charts");
    private static string tempPath = Path.Combine(Application.persistentDataPath, "temp");
    private static string audioFilesPath = Path.Combine(Application.persistentDataPath, "custom_charts", "AudioFiles");
    private static string coverFilesPath = Path.Combine(Application.persistentDataPath, "custom_charts", "CoverFiles");

    public static void CreateRmblFile(SongData songData, string outputPath)
    {
        string songDataPath = songData.metadata.localPath; 
        string audioFilePath = SaveData.GetAudioFilePath(songData.metadata.audioFileName);
        string coverFilePath = SaveData.GetCoverFilePath(songData.metadata.coverFileName);

        if (!File.Exists(songDataPath)) return;

        // Create the .rmbl file (which is just a ZIP)
        try {
            using (FileStream zipStream = new FileStream(outputPath, FileMode.Create))
            using (ZipArchive archive = new ZipArchive(zipStream, ZipArchiveMode.Create)) {
                // Add the song data file
                archive.CreateEntryFromFile(songDataPath, Path.GetFileName(songDataPath));
                // Add the audio file
                if (File.Exists(audioFilePath))
                    archive.CreateEntryFromFile(audioFilePath, Path.GetFileName(audioFilePath));
                // Add the cover file
                if (File.Exists(coverFilePath))
                    archive.CreateEntryFromFile(coverFilePath, Path.GetFileName(coverFilePath));
            }

            Debug.Log($"Created .rmbl package at: {outputPath}");
        }
        catch (Exception ex) {
            Debug.LogError($"Failed to create .rmbl file: {ex.Message}");
        }
    }

    public static void ExtractRmblFile(string rmblPath)
    {
        if (!File.Exists(rmblPath)) {
            Debug.LogError("File not found: " + rmblPath);
            return;
        }

        // Ensure temp folder is clean
        if (Directory.Exists(tempPath))
            Directory.Delete(tempPath, true);
        Directory.CreateDirectory(tempPath);

        // Extract all files to temp
        ZipFile.ExtractToDirectory(rmblPath, tempPath);

        // Find .songdata file
        string songDataFilePath = Directory.GetFiles(tempPath, "*.songdata").FirstOrDefault();
        if (string.IsNullOrEmpty(songDataFilePath)) {
            Debug.LogError("No .songdata file found in .rmbl archive.");
            return;
        }

        SongData songData = SaveData.LoadCustomSong(songDataFilePath);
        songData.metadata.localPath = "";
        songData.metadata.audioFileName = "";
        songData.metadata.coverFileName = "";
        SaveData.SaveCustomSongData(songData);

        // Copy audio file
        string audioFilePath = Directory.GetFiles(tempPath, "*.mp3")
            .Concat(Directory.GetFiles(tempPath, "*.wav"))
            .Concat(Directory.GetFiles(tempPath, "*.ogg")).FirstOrDefault();
        if (!string.IsNullOrEmpty(audioFilePath)) {
            SaveData.CreateAudioFile(songData, audioFilePath);
        }

        // Copy cover file
        string coverFilePath = Directory.GetFiles(tempPath, "*.png")
            .Concat(Directory.GetFiles(tempPath, "*.jpg"))
            .Concat(Directory.GetFiles(tempPath, "*.jpeg")).FirstOrDefault();
        if (!string.IsNullOrEmpty(coverFilePath)) {
            SaveData.CreateCoverFile(songData, coverFilePath);
        }

        // Clean up temp
        Directory.Delete(tempPath, true);
        Debug.Log($"Extracted .rmbl file");
    }
}