using FMODUnity;
using UnityEngine;

[CreateAssetMenu(fileName = "SongDataResource", menuName = "Scriptable Objects/SongDataResource")]
public class SongDataResource : ScriptableObject
{
    public SongData data;
    [field: SerializeField] public EventReference songReference { get; private set; }
    public Sprite coverImage;
}
