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
    [field: Header("Song")]
    public EventInstance songInstance { get; private set; }

    // Beat parameters
    [field: Header("Beat Parameters")]
    public float BPM = 60f;
    public int startingTimeDelay = 1000; //in ms
    private float beatSecondInterval;

    public SongSlider songSlider;

    private int timelinePosition;
    private float timelineBeatPosition;
    
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

        // Start playbacks
        songInstance.getDescription(out EventDescription description);
        description.getLength(out int length);
        Debug.Log(length);
        songSlider.SetMaxValue(length);

        //RuntimeManager.StudioSystem.getCoreSystem(out FMOD.System coreSystem);
        //coreSystem.getSoftwareFormat(out int sampleRate, out _, out _);
    }

    private void Update()
    {
        songInstance.getTimelinePosition(out timelinePosition);
        timelineBeatPosition = GetBeatFromTime(timelinePosition);
        
        /*if (!loaded) {
            LoadSoundData();
        }*/
        
    }

    /*
    void LoadSoundData()
    {
        // Get event description
        songInstance.getDescription(out EventDescription eventDescription);
        eventDescription.getSampleLoadingState(out LOADING_STATE loadingState);

        if (loadingState != LOADING_STATE.LOADED) {
            Debug.LogError("FMOD Event not loaded.");
            return;
        }

        // Get the underlying sound object
        songInstance.getChannelGroup(out FMOD.ChannelGroup channelGroup);
        channelGroup.getGroup(0, out FMOD.ChannelGroup group);
        group.getChannel(0, out FMOD.Channel channel);

        if (channel.hasHandle()) {
            channel.getCurrentSound(out FMOD.Sound sound);
            Debug.Log("Sound obtained");

            // Get length in milliseconds (FAST)
            sound.getLength(out uint lengthMS, FMOD.TIMEUNIT.MS);

            
            uint readSize = 4096; // Read in chunks
            waveform = new List<float>();
            byte[] buffer = new byte[readSize];

            uint bytesRead;
            uint totalRead = 0;
            uint sampleInterval = lengthMS / 1000; // Get interval in MS

            for (uint ms = 0; ms < lengthMS; ms += sampleInterval) {
                // Seek to the desired position in the stream
                channel.setPosition(ms, FMOD.TIMEUNIT.MS);

                // Read PCM data at this position
                FMOD.RESULT result = sound.readData(buffer, out bytesRead);
                if (result == FMOD.RESULT.ERR_FILE_EOF || bytesRead == 0) break;

                // Convert PCM16 to float (-1 to 1 range)
                for (int i = 0; i < bytesRead; i += 2) // Read every sample
                {
                    short sample = (short)(buffer[i] | (buffer[i + 1] << 8));
                    waveform.Add(sample / 32768f);
                    break; // Take only the first sample per chunk
                }
            }

            sound.release();

            Debug.Log($"Extracted {waveform.Count} waveform points.");
            // Use waveform data for visualization
            Debug.Log(waveform[0]);
            loaded = true;
        }

        
    }*/

    public int GetTimelinePosition()
    {
        return timelinePosition;
    }

    public float GetTimelineBeatPosition()
    {
        return timelineBeatPosition;
    }

    public float GetBeatFromTime(int time)
    {
        return (float)(time - startingTimeDelay) / (beatSecondInterval * 1000f);
    }
     
    public int GetTimeFromBeat(float beat)
    {
        return (int)(beat * beatSecondInterval * 1000 + startingTimeDelay);
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

    public void SetTimelinePosition(int time)
    {
        timelinePosition = time;
        if (songInstance.isValid()) {
            songInstance.setTimelinePosition(timelinePosition);
        }
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



