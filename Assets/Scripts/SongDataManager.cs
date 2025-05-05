using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System.IO;

public class SongDataManager : MonoBehaviour
{
    public static SongDataManager instance { get; private set; }
    private SongData customSelectedSongData;

    private void Awake()
    {
        if (instance == null) {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else {
            Destroy(gameObject);
        }
    }

    public void CreateCustomSong()
    {
        customSelectedSongData = new SongData("Custom");
        SaveData.SaveCustomSongData(customSelectedSongData);
    }

    public void SetCustomSelectedSong(string songDirPath)
    {
        customSelectedSongData = SaveData.LoadCustomSong(songDirPath);
    }

    public void SaveCustomSelectedSong()
    {
        SaveData.SaveCustomSongData(customSelectedSongData);
    }

    public void SetCustomSelectedSongName(string name)
    {
        customSelectedSongData.metadata.songName = name;
    }

    public void SetCustomSelectedSongArtist(string artist)
    {
        customSelectedSongData.metadata.artist = artist;
    }

    public SongMetadata GetCustomSelectedSongMetadata()
    {
        return customSelectedSongData.metadata;
    }

    public IEnumerator SaveAndLoadCustomAudioFile(string filePath)
    {
        // Save
        string audioPath = SaveData.SaveAudioFile(customSelectedSongData, filePath);
        customSelectedSongData.metadata.audioFileName = Path.GetFileName(audioPath);

        // Load
        Metronome.instance.LoadCustomAudioFile(customSelectedSongData.metadata);
        yield break;
    }

    public List<NoteData> GetDifficultyNoteData(Difficulty difficulty)
    {
        switch (difficulty) {
            case Difficulty.Easy:
                return customSelectedSongData.easyNotes;
            case Difficulty.Normal:
                return customSelectedSongData.normalNotes;
            case Difficulty.Hard:
                return customSelectedSongData.hardNotes;
            case Difficulty.Rumble:
                return customSelectedSongData.rumbleNotes;
        }
        return null;
    }

    public void SetDifficultyNoteData(List<NoteData> notesData, Difficulty difficulty)
    {
        switch (difficulty) {
            case Difficulty.Easy:
                customSelectedSongData.easyNotes = notesData;
                break;
            case Difficulty.Normal:
                customSelectedSongData.normalNotes = notesData;
                break;
            case Difficulty.Hard:
                customSelectedSongData.hardNotes = notesData;
                break;
            case Difficulty.Rumble:
                customSelectedSongData.rumbleNotes = notesData;
                break;
        }
    }

    public void SetBPMFlags(List<BPMFlag> bpmFlags)
    {
        customSelectedSongData.BPMFlags = bpmFlags;
    }

    public bool IsSongDataSelected()
    {
        return customSelectedSongData != null;
    }
    public void SetTemporalSongData()
    {
        customSelectedSongData = new SongData();
    }
}
