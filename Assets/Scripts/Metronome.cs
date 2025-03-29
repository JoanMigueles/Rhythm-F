using FMOD.Studio;
using FMODUnity;
using System;
using System.Runtime.InteropServices;
using UnityEngine;

public class Metronome : MonoBehaviour
{
    public static Metronome instance { get; private set; }

    [field: Header("Metronome")]
    [field: SerializeField] public EventReference beepReference { get; private set; }
    private EventInstance beepInstance;

    // Song
    public EventInstance songInstance { get; private set; }

    // Beat parameters
    [field: Header("Beat Parameters")]
    public float BPM = 60f;
    private float beatSecondInterval;
    public int startingTimeDelay = 1000; //in ms

    private int timelinePosition;
    private int timelineBeatPosition;
    
    private FMOD.ChannelGroup channelGroup;
    private GameManager gm;
    private SongData sd;
    private FMODBeatTracker tracker;

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
        gm = GameManager.instance;
        sd = GetComponent<SongData>();

        beatSecondInterval = 60f / BPM;
        //songPositionBeat = ((float)songPosition - (float)startingTimeDelay) / 1000f / beatSecondInterval;
        //previousSongPositionBeat = ((float)songPosition - (float)startingTimeDelay) / 1000f / beatSecondInterval;

        beepInstance = RuntimeManager.CreateInstance(beepReference);
        songInstance = RuntimeManager.CreateInstance(sd.songs[0]);

        tracker = new FMODBeatTracker(songInstance, beepInstance);
        tracker.SetPlayBeep(true);

        // Start playback
        songInstance.start();

        //RuntimeManager.StudioSystem.getCoreSystem(out FMOD.System coreSystem);
        //coreSystem.getSoftwareFormat(out int sampleRate, out _, out _);
    }

    private void Update()
    {
        songInstance.getTimelinePosition(out int tposition);
        ExtractSoundFromEvent(songInstance);
        
    }

    public int GetSongTime()
    {
        return timelinePosition;
    }

    public int GetSongBeat()
    {
        return timelineBeatPosition;
    }

    public void ReleaseSongInstance()
    {
        songInstance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
        songInstance.release();
    }

    public void SetSongInstance(EventReference eventReference)
    {
        songInstance = RuntimeManager.CreateInstance(eventReference);
    }

    public void SetSongInstance(EventInstance eventInstance)
    {
        songInstance = eventInstance;
    }

    public void PlaySong()
    {
        if (songInstance.isValid()) {
            songInstance.start();
        }
    }
    public void StopSong()
    {
        if (songInstance.isValid()) {
            songInstance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
        }
    }

    private FMOD.Sound ExtractSoundFromEvent(EventInstance eventInstance)
    {
        // Ensure the event is playing
        eventInstance.getPlaybackState(out PLAYBACK_STATE state);
        if (state != PLAYBACK_STATE.PLAYING) {
            Debug.LogError("Event is not playing, cannot extract sound.");
            return new FMOD.Sound();
        }

        // Get the ChannelGroup
        eventInstance.getChannelGroup(out FMOD.ChannelGroup channelGroup);
        if (!channelGroup.hasHandle()) {
            Debug.LogError("Failed to get ChannelGroup from event!");
            return new FMOD.Sound();
        }

        // Get the first active channel
        FMOD.RESULT channelResult = channelGroup.getChannel(0, out FMOD.Channel channel);
        if (channelResult != FMOD.RESULT.OK || !channel.hasHandle()) {
            Debug.LogError("No active channel found in ChannelGroup.");
            return new FMOD.Sound();
        }

        // Extract the sound from the channel
        FMOD.RESULT soundResult = channel.getCurrentSound(out FMOD.Sound sound);
        if (soundResult == FMOD.RESULT.OK && sound.hasHandle()) {
            Debug.Log("Successfully extracted sound from FMOD Event!");
            return sound;
        }

        Debug.LogError("Failed to extract sound. FMOD error: " + soundResult);
        return new FMOD.Sound();
    }

    void OnDestroy()
    {
        ReleaseSongInstance();
        beepInstance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
        beepInstance.release();
    }
}

public class FMODBeatTracker
{
    private EventInstance songEventInstance;
    private EventInstance beepEventInstance;
    private (int beat, int bar) currentBeat;
    private GCHandle handle;
    private bool playBeep = false; 

    public FMODBeatTracker(EventInstance songInstance, EventInstance beepInstance)
    {
        // Get instances
        beepEventInstance = beepInstance;
        songEventInstance = songInstance;

        // Store GCHandle to this tracker
        handle = GCHandle.Alloc(this);
        IntPtr handlePtr = GCHandle.ToIntPtr(handle);
        songEventInstance.setUserData(handlePtr);

        // Set callback
        songEventInstance.setCallback(BeatEventCallback, EVENT_CALLBACK_TYPE.TIMELINE_BEAT);
        Debug.Log("Created callback");
    }

    private static FMOD.RESULT BeatEventCallback(EVENT_CALLBACK_TYPE type, IntPtr instancePtr, IntPtr parametersPtr)
    {
        // Retrieve the user data
        IntPtr userData;
        EventInstance eventInstance = new EventInstance(instancePtr);
        eventInstance.getUserData(out userData);

        // Get the object to use
        if (userData != IntPtr.Zero) {
            GCHandle handle = GCHandle.FromIntPtr(userData);

            if (type == EVENT_CALLBACK_TYPE.TIMELINE_BEAT) {
                TIMELINE_BEAT_PROPERTIES beatProperties = (TIMELINE_BEAT_PROPERTIES)Marshal.PtrToStructure(parametersPtr, typeof(TIMELINE_BEAT_PROPERTIES));

                if (handle.Target is FMODBeatTracker tracker) {
                    if (tracker.playBeep) {
                        tracker.PlayBeep();
                    }
                    tracker.SetCurrentBeatProperties(beatProperties.beat, beatProperties.bar);
                }
            } 
            else if (type == EVENT_CALLBACK_TYPE.DESTROYED) {
                // Now the event has been destroyed, unpin the timeline memory so it can be garbage collected
                handle.Free();
            }
        }

        return FMOD.RESULT.OK;
    }

    private void PlayBeep()
    {
        beepEventInstance.start();
    }

    private void SetCurrentBeatProperties(int beat, int bar)
    {
        currentBeat = (beat, bar);
    }

    public (int beat, int bar) GetCurrentBeatProperties()
    {
        return currentBeat;
    }

    public void SetPlayBeep(bool play)
    {
        playBeep = play;
    }
}



