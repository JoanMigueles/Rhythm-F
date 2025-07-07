using FMODUnity;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "SongDataList", menuName = "Scriptable Objects/SongDataList")]
public class SongDataList : ScriptableObject
{
    public List<SongDataResource> songsList;
    
}

public static class ResourceLoader
{
    private static SongDataList list;
    public static List<SongMetadata> LoadAllSongsMetadata()
    {
        list = Resources.Load<SongDataList>("SongDataList");
        List<SongMetadata> metadataList = new List<SongMetadata>();
        
        foreach (SongDataResource songResource in list.songsList) {
            metadataList.Add(songResource.data.metadata);
        }
        return metadataList;
    }

    public static SongData LoadSong(int id)
    {
        if (list == null) list = Resources.Load<SongDataList>("SongDataList");
        SongData song = list.songsList.FirstOrDefault(s => s.data.metadata.songID == id).data;
        return song;
    }

    public static EventReference LoadEventReference(int id)
    {
        if (list == null) list = Resources.Load<SongDataList>("SongDataList");
        EventReference reference = list.songsList.FirstOrDefault(s => s.data.metadata.songID == id).songReference;
        return reference;
    }

    public static Sprite LoadSongCover(int id)
    {
        if (list == null) list = Resources.Load<SongDataList>("SongDataList");
        Sprite sprite = list.songsList.FirstOrDefault(s => s.data.metadata.songID == id).coverImage;
        return sprite;
    }
}