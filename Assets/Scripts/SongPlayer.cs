using UnityEngine;
using FMOD.Studio;
using FMODUnity;
using UnityEngine.UI;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Window;
using System.Collections.Generic;

public class SongPlayer : MonoBehaviour
{

    [field: Header("Songs")]
    [field: SerializeField] public EventReference songReference { get; private set; }

    [field: Header("Metronome")]
    [field: SerializeField] public EventReference beepReference { get; private set; }
    //private EventInstance song;

    public static SongPlayer instance { get; private set; }
    // Create a dictionary to store player scores
    private Dictionary<string, EventInstance> events = new Dictionary<string, EventInstance>();

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

    private void Start()
    {
        CreateEventInstance(beepReference);
    }

    public EventInstance GetSong(EventReference eventReference)
    {
        return events[eventReference.Path];
    }

    public void Play(EventReference eventReference)
    {
        Debug.Log(eventReference.Path);
        events[eventReference.Path].start();
    }

    public void Stop(EventReference eventReference)
    {
        events[eventReference.Path].stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
    }

    public void CreateEventInstance(EventReference eventReference)
    {
        EventInstance eventInstance = RuntimeManager.CreateInstance(eventReference);
        events.Add(eventReference.Path, eventInstance);
    }

    // Para canciones
    public void DestroyEventInstace(EventReference eventReference)
    {
        events[eventReference.Path].release();
        events.Remove(eventReference.Path);
    }

    // Fin del juego ?????
    private void OnDisable()
    {
        foreach (EventInstance e in events.Values) {
            e.release();
        }
        events.Clear();
    }
}
