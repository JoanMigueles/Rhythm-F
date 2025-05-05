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
    public string localDirPath;
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

    public static List<SongMetadata> LoadAllCustomSongsMetadata()
    {
        List<SongMetadata> customSongsMetadata;
        if (!Directory.Exists(customChartsPath)) {
            customSongsMetadata = new List<SongMetadata> ();
            return customSongsMetadata;
        }

        string[] songDirPaths = Directory.GetDirectories(customChartsPath);
        customSongsMetadata = new List<SongMetadata>(songDirPaths.Length);
        for (int i = 0; i < songDirPaths.Length; i++) {
            customSongsMetadata.Add(LoadCustomSong(songDirPaths[i]).metadata);
        }
        return customSongsMetadata;
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

    private static string GenerateDirectoryName(SongData songData)
    {
        string name = !string.IsNullOrEmpty(songData.metadata.songName) ? songData.metadata.songName : "Custom";
        string artist = !string.IsNullOrEmpty(songData.metadata.songName) ? $"_{songData.metadata.artist}" : "";
        return name + artist;
    }

    private static string CreateCustomSongDir(SongData songData)
    {
        if (!Directory.Exists(customChartsPath))
            Directory.CreateDirectory(customChartsPath);

        string dirName = GenerateDirectoryName(songData);
        string songDirPath = Path.Combine(customChartsPath, dirName);
        int rev = 1;
        while (Directory.Exists(songDirPath)) {
            songDirPath = Path.Combine(customChartsPath, $"{dirName}_{rev}");
            rev++;
        }
        Directory.CreateDirectory(songDirPath);
        return songDirPath;
    }

    public static void SaveCustomSongData(SongData songData)
    {
        if (!IsLocallySaved(songData)) {
            songData.metadata.localDirPath = CreateCustomSongDir(songData);
        } else {
            string dirName = GenerateDirectoryName(songData);
            string songDirPath = Path.Combine(customChartsPath, dirName);
            int rev = 1;
            bool found = false;
            while (Directory.Exists(songDirPath)) {
                if (songData.metadata.localDirPath == songDirPath) {
                    found = true;
                    break;
                } else {
                    songDirPath = Path.Combine(customChartsPath, $"{dirName}_{rev}");
                    rev++;
                }
            }
            if (!found) {
                Directory.Move(songData.metadata.localDirPath, songDirPath);
                songData.metadata.localDirPath = songDirPath;
            }
        }
        foreach (string existingFile in Directory.GetFiles(songData.metadata.localDirPath, "*.songdata")) {
            File.Delete(existingFile);
        }
        string songFileName = (!string.IsNullOrEmpty(songData.metadata.songName) ? songData.metadata.songName : "Custom") + ".songdata";
        string destinationPath = Path.Combine(songData.metadata.localDirPath, songFileName);
        SongFileConverter.SaveToTextFormat(songData, destinationPath);
    }

    public static string SaveAudioFile(SongData songData, string filePath)
    {

        string fileName = Path.GetFileName(filePath);
        string destinationPath;

        if (!IsLocallySaved(songData)) {
            SaveCustomSongData(songData);
        }

        destinationPath = Path.Combine(customChartsPath, songData.metadata.localDirPath, fileName);

        if (!File.Exists(destinationPath)) {
            File.Copy(filePath, destinationPath);
            Debug.Log("File saved to: " + destinationPath);
        }

        return destinationPath;
    }

    private static bool IsLocallySaved(SongData songData)
    {
        return songData != null && !string.IsNullOrEmpty(songData.metadata.localDirPath) && Directory.Exists(songData.metadata.localDirPath);
    }

}
