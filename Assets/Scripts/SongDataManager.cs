using UnityEngine;
using System.Collections.Generic;
using FMODUnity;


// Supporting data structures
[System.Serializable]
public class SongData
{
    public int songID;
    public string songName;
    public string artist;
    public string audioFilePath;
    public string coverFilePath;
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

    private void Start()
    {
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
        SongFileConverter.LoadFromTextFormat(GameManager.instance.GetSelectedSong());
    }

    public void SaveSong()
    {

    }

    public void SaveSongAsNew()
    {

    }
}
