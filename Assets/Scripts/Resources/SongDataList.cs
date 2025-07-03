using FMODUnity;
using System.Collections.Generic;
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
        
        for (int i = 0; i < list.songsList.Count; i++) {
            list.songsList[i].data.metadata.songID = i;
            metadataList.Add(list.songsList[i].data.metadata);
        }
        return metadataList;
    }

    public static SongData LoadSong(int id)
    {
        if (list == null) list = Resources.Load<SongDataList>("SongDataList");
        return list.songsList[id].data;
    }

    public static EventReference LoadEventReference(int id)
    {
        if (list == null) list = Resources.Load<SongDataList>("SongDataList");
        return list.songsList[id].songReference;
    }
}