using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

[System.Serializable]
public enum Stage
{
    City,
    Beach,
    Future
}

[System.Serializable]
public class SongData
{
    public SongMetadata metadata;
    public List<BPMFlag> BPMFlags;
    public List<NoteData> easyNotes;
    public List<NoteData> normalNotes;
    public List<NoteData> hardNotes;
    public List<NoteData> rumbleNotes;

    public SongData()
    {
        metadata = new SongMetadata();
        metadata.songGUID = Guid.NewGuid();
        BPMFlags = new List<BPMFlag>();
        easyNotes = new List<NoteData>();
        normalNotes = new List<NoteData>();
        hardNotes = new List<NoteData>();
        rumbleNotes = new List<NoteData>();
    }
}

[System.Serializable]
public struct SongMetadata
{
    public string localPath;
    public int songID;
    public Guid songGUID;
    public string songName;
    public string artist;
    public string audioFileName;
    public string coverFileName;
    public int previewStartTime;
    public Stage stage;
}

public static class SaveData
{
    private static string customChartsPath = Path.Combine(Application.persistentDataPath, "custom_charts");
    private static string audioFilesPath = Path.Combine(Application.persistentDataPath, "custom_charts", "AudioFiles");
    private static string coverFilesPath = Path.Combine(Application.persistentDataPath, "custom_charts", "CoverFiles");

    // ALL CUSTOM SONG METADATA
    public static List<SongMetadata> LoadAllCustomSongsMetadata()
    {
        List<SongMetadata> customSongsMetadata;

        // If nothing is saved return nothing
        if (!Directory.Exists(customChartsPath)) {
            customSongsMetadata = new List<SongMetadata>();
            return customSongsMetadata;
        }

        string[] songDataFiles = Directory.GetFiles(customChartsPath, "*.songdata");
        customSongsMetadata = new List<SongMetadata>(songDataFiles.Length);

        for (int i = 0; i < songDataFiles.Length; i++) {
            customSongsMetadata.Add(LoadCustomSong(songDataFiles[i]).metadata);
        }

        return customSongsMetadata;
    }

    // CUSTOM SONG DATA
    public static SongData LoadCustomSong(string songFilePath)
    {
        SongData song = new SongData();

        if (File.Exists(songFilePath)) {
            song = SongFileConverter.LoadFromTextFormat(songFilePath);
            song.metadata.songID = -1;
        }

        return song;
    }

    // AUDIO PATH FROM NAME
    public static string GetAudioFilePath(string name)
    {
        return Path.Combine(audioFilesPath, name);
    }

    // COVER PATH FROM NAME
    public static string GetCoverFilePath(string name)
    {
        return Path.Combine(coverFilesPath, name);
    }

    // FILE NAME FORMAT
    private static string GenerateFileName(SongData songData)
    {
        string name = !string.IsNullOrEmpty(songData.metadata.songName) ? songData.metadata.songName : "Custom";
        string artist = !string.IsNullOrEmpty(songData.metadata.songName) ? $"_{songData.metadata.artist}" : "";
        return $"{name}{artist}";
    }

    // CREATE
    private static string CreateCustomSongFile(SongData songData)
    {
        if (!Directory.Exists(customChartsPath))
            Directory.CreateDirectory(customChartsPath);

        string fileName = GenerateFileName(songData);
        string filePath = Path.Combine(customChartsPath, $"{fileName}.songdata");
        int rev = 1;
        while (File.Exists(filePath)) {
            filePath = Path.Combine(customChartsPath, $"{fileName}_{rev}.songdata");
            rev++;
        }

        Debug.Log("Creating " + filePath);
        SongFileConverter.SaveToTextFormat(songData, filePath);

        return filePath;
    }

    // SAVE
    public static void SaveCustomSongData(SongData songData)
    {
        if (!IsLocallySaved(songData)) {
            songData.metadata.localPath = CreateCustomSongFile(songData);
        } else {
            string fileName = GenerateFileName(songData);
            string filePath = Path.Combine(customChartsPath, $"{fileName}.songdata");
            int rev = 1;
            bool found = false;
            while (File.Exists(filePath)) {
                if (songData.metadata.localPath == filePath) {
                    found = true;
                    break;
                } else {
                    filePath = Path.Combine(customChartsPath, $"{fileName}_{rev}.songdata");
                    rev++;
                }
            }
            if (!found) {
                File.Delete(songData.metadata.localPath);
                songData.metadata.localPath = filePath;
            }
            SongFileConverter.SaveToTextFormat(songData, filePath);
        }
    }

    // REMOVE
    public static void RemoveCustomSongData(SongMetadata metadata)
    {
        if (!IsLocallySaved(metadata)) return;

        if (Directory.Exists(audioFilesPath) && !string.IsNullOrEmpty(metadata.audioFileName))
            File.Delete(GetAudioFilePath(metadata.audioFileName));

        if (Directory.Exists(coverFilesPath) && !string.IsNullOrEmpty(metadata.coverFileName))
            File.Delete(GetCoverFilePath(metadata.coverFileName));

        File.Delete(metadata.localPath);
    }

    // CREATE AUDIO
    public static void CreateAudioFile(SongData songData, string filePath)
    {
        if (!Directory.Exists(audioFilesPath))
            Directory.CreateDirectory(audioFilesPath);

        if (!string.IsNullOrEmpty(songData.metadata.audioFileName))
            File.Delete(GetAudioFilePath(songData.metadata.audioFileName));

        string audioName = Path.GetFileNameWithoutExtension(filePath);
        string audioExtension = Path.GetExtension(filePath);

        string audioPath = Path.Combine(audioFilesPath, $"{audioName}{audioExtension}");
        int rev = 1;
        while (File.Exists(audioPath)) {
            audioPath = Path.Combine(audioFilesPath, $"{audioName}_{rev}{audioExtension}");
            rev++;
        }

        File.Copy(filePath, audioPath);
        songData.metadata.audioFileName = Path.GetFileName(audioPath);
        SaveCustomSongData(songData);
    }

    // CREATE COVER
    public static void CreateCoverFile(SongData songData, string filePath)
    {
        if (!Directory.Exists(coverFilesPath))
            Directory.CreateDirectory(coverFilesPath);

        if (!string.IsNullOrEmpty(songData.metadata.coverFileName))
            File.Delete(GetCoverFilePath(songData.metadata.coverFileName));

        string coverName = Path.GetFileNameWithoutExtension(filePath);
        string coverExtension = Path.GetExtension(filePath);

        string coverPath = Path.Combine(coverFilesPath, $"{coverName}{coverExtension}");
        int rev = 1;
        while (File.Exists(coverPath)) {
            coverPath = Path.Combine(coverFilesPath, $"{coverName}_{rev}{coverExtension}");
            rev++;
        }

        File.Copy(filePath, coverPath);
        songData.metadata.coverFileName = Path.GetFileName(coverPath);
        SaveCustomSongData(songData);
    }

    // GET COVER SPRITE
    public static Sprite GetCoverSprite(string filePath)
    {
        if (!File.Exists(filePath)) return null;

        byte[] fileData = File.ReadAllBytes(filePath);
        Texture2D texture = new Texture2D(2, 2);
        if (texture.LoadImage(fileData)) {
            Sprite sprite = Sprite.Create(
                texture,
                new Rect(0, 0, texture.width, texture.height),
                new Vector2(0.5f, 0.5f) // pivot
            );
            return sprite;
        }
        else {
            Debug.LogError("Could not load image data into texture.");
            return null;
        }
    }


    // IS SAVED
    private static bool IsLocallySaved(SongData songData)
    {
        return songData != null && !string.IsNullOrEmpty(songData.metadata.localPath) && File.Exists(songData.metadata.localPath);
    }
    private static bool IsLocallySaved(SongMetadata metadata)
    {
        return !string.IsNullOrEmpty(metadata.localPath) && File.Exists(metadata.localPath);
    }

}
