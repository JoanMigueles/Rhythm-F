using UnityEngine;
using System.Collections.Generic;
using FMODUnity;
public class SongData : MonoBehaviour
{
    [field: Header("Songs")]
    [field: SerializeField] public List<EventReference> songs { get; private set; }
}
