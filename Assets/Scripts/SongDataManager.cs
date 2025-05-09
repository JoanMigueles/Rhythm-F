using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System.IO;

public class SongDataManager : MonoBehaviour
{
    public static SongDataManager instance { get; private set; }
    private SongData customSelectedSongData;
    private bool isNew;

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
        isNew = true;
        customSelectedSongData = new SongData();
    }

    public bool IsNewSong() { return isNew; }

    public void SetCustomSelectedSong(string songFilePath)
    {
        isNew = false;
        customSelectedSongData = SaveData.LoadCustomSong(songFilePath);
        string songPath = SaveData.GetAudioFilePath(customSelectedSongData.metadata.songName);
        Metronome.instance.SetCustomSong(songPath);
    }

    public void DeselectCustomSong()
    {
        isNew = false;
        customSelectedSongData = null;
    }

    public void SaveCustomSelectedSong()
    {
        isNew = false;
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
        SaveData.CreateAudioFile(customSelectedSongData, filePath);

        // Load

        Metronome.instance.SetCustomSong(SaveData.GetAudioFilePath(customSelectedSongData.metadata.audioFileName));
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
}
