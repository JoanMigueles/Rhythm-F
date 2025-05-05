using System.Collections.Generic;
using System.IO;
using UnityEngine;

// Supporting data structures
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
        BPMFlags = new List<BPMFlag>();
        easyNotes = new List<NoteData>();
        normalNotes = new List<NoteData>();
        hardNotes = new List<NoteData>();
        rumbleNotes = new List<NoteData>();
    }

    public SongData(string name)
    {
        metadata = new SongMetadata();
        metadata.songName = name;
        BPMFlags = new List<BPMFlag>();
        easyNotes = new List<NoteData>();
        normalNotes = new List<NoteData>();
        hardNotes = new List<NoteData>();
        rumbleNotes = new List<NoteData>();
    }
}

public struct SongMetadata
{
    public string localPath;
    public int songID;
    public string songName;
    public string artist;
    public string audioFileName;
    public string coverFileName;
    public float previewStartTime;
}

public static class SaveData
{
    private static string customChartsPath = Path.Combine(Application.persistentDataPath, "custom_charts");
    private static string audioFilesPath = Path.Combine(Application.persistentDataPath, "custom_charts", "AudioFiles");
    private static string coverFilesPath = Path.Combine(Application.persistentDataPath, "custom_charts", "CoverFiles");

    public static List<SongMetadata> LoadAllCustomSongsMetadata()
    {
        List<SongMetadata> customSongsMetadata;

        // If nothing is saved return nothing
        if (!Directory.Exists(customChartsPath)) {
            customSongsMetadata = new List<SongMetadata>();
            return customSongsMetadata;
        }

        //
        string[] songDataFiles = Directory.GetFiles(customChartsPath, "*.songdata");
        customSongsMetadata = new List<SongMetadata>(songDataFiles.Length);

        for (int i = 0; i < songDataFiles.Length; i++) {
            customSongsMetadata.Add(LoadCustomSong(songDataFiles[i]).metadata);
        }

        return customSongsMetadata;
    }

    public static SongData LoadCustomSong(string songFilePath)
    {
        SongData song = new SongData();

        if (File.Exists(songFilePath)) {
            song = SongFileConverter.LoadFromTextFormat(songFilePath);
        }

        return song;
    }

    public static string GetAudioFilePath(string name)
    {
        return Path.Combine(audioFilesPath, name);
    }

    private static string GenerateFileName(SongData songData)
    {
        string name = !string.IsNullOrEmpty(songData.metadata.songName) ? songData.metadata.songName : "Custom";
        string artist = !string.IsNullOrEmpty(songData.metadata.songName) ? $"_{songData.metadata.artist}" : "";
        return $"{name}{artist}";
    }

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
        SongFileConverter.SaveToTextFormat(songData, filePath);
        return filePath;
    }

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
            }
            SongFileConverter.SaveToTextFormat(songData, filePath);
        }
    }

    public static void CreateAudioFile(SongData songData, string filePath)
    {
        if (!Directory.Exists(audioFilesPath))
            Directory.CreateDirectory(audioFilesPath);

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

    public static void RemoveAudioFile(SongData songData)
    {
        if (!Directory.Exists(audioFilesPath))
            return;

        string audioPath = Path.Combine(audioFilesPath, songData.metadata.audioFileName);

        File.Delete(audioPath);
        songData.metadata.audioFileName = "";
        SaveCustomSongData(songData);
    }

    private static bool IsLocallySaved(SongData songData)
    {
        return songData != null && !string.IsNullOrEmpty(songData.metadata.localPath) && File.Exists(songData.metadata.localPath);
    }

}
