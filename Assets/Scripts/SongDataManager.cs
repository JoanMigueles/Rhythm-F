using UnityEngine;
using System.Collections.Generic;
using FMODUnity;
using UnityEngine.UI;
using System.Collections;


// Supporting data structures
[System.Serializable]
public class SongData
{
    public int songID;
    public string songName;
    public string artist;
    public string audioFileName;
    public string coverFileName;
    public float previewStartTime;
    public List<BPMFlag> BPMFlags = new List<BPMFlag>();
    public List<NoteData> easyNotes = new List<NoteData>();
    public List<NoteData> normalNotes = new List<NoteData>();
    public List<NoteData> hardNotes = new List<NoteData>();
    public List<NoteData> rumbleNotes = new List<NoteData>();
}

[System.Serializable]
public struct BPMFlag
{
    public int offset;
    public float BPM;
}

public class SongDataManager : MonoBehaviour
{
    [field: Header("Songs")]
    [field: SerializeField] public List<EventReference> songs { get; private set; }

    [field: Header("Custom Songs")]
    [field: SerializeField] public EventReference customSongReference { get; private set; }
    private string customSelectedSongPath;
    private SongData customSelectedSongData;


    private void Start()
    {

        /*
        // Load the .songdata file (omit the extension)
        TextAsset songDataFile = Resources.Load<TextAsset>("SongData/mySong");

        if (songDataFile != null) {
            string songDataContent = songDataFile.text;
            Debug.Log("Song data loaded: " + songDataContent);

            // Process your song data here
            ProcessSongData(songDataContent);
        }
        else {
            Debug.LogError("Failed to load song data file!");
        }

        void ProcessSongData(string data)
        {

            Debug.Log("Processing: " + data);
        }
        // TODO: if passed a song data through previous scene, load it, create new otherwise;
        SongFileConverter.LoadFromTextFormat(GameManager.instance.GetSelectedSong());*/
    }

    public void SetCustomSelectedSong(string selectedSongPath, SongData selectedSong)
    {
        customSelectedSongPath = selectedSongPath;
        customSelectedSongData = selectedSong;
    }

    public void SetSongName(string name)
    {
        customSelectedSongData.songName = name;
        EditorUIManager.instance.SetSongTitle(customSelectedSongData.songName, customSelectedSongData.artist);
    }

    public void SetSongArtist(string artist)
    {
        customSelectedSongData.artist = artist;
        EditorUIManager.instance.SetSongTitle(customSelectedSongData.songName, customSelectedSongData.artist);
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

    public IEnumerator LoadAndSaveAudioFile(string filePath)
    {
        /*
        string audioPath = SaveData.SaveAudioFile(NoteManager.instance.GetSongData(), filePath);

        // Create a new FMOD sound instance
        Metronome.instance.ReleaseSongInstance();
        programmerSound = new FMODProgrammerSound(audioPath, customSongReference);
        Metronome.instance.SetSongInstance(programmerSound.GetEventInstance());*/
        yield break;
    }
}
