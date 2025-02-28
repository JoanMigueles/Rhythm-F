
using FMOD.Studio;
using FMODUnity;
using System;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Threading;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine;
using UnityEngine.UIElements;

public class Metronome : MonoBehaviour
{
    public static Metronome instance { get; private set; }

    [field: Header("Metronome")]
    [field: SerializeField] public EventReference beepReference { get; private set; }
    private EventInstance beepInstance;

    // Song
    private EventInstance songInstance;

    // Beat parameters
    [field: Header("Beat Parameters")]
    public float BPM = 60f;
    private float beatSecondInterval;
    public int startingTimeDelay = 1000; //in ms

    private float timelinePosition;
    private float songPosition;
    private int previousSongPosition;
    private float songPositionBeat;
    private float previousSongPositionBeat;
    
    private FMOD.ChannelGroup channelGroup;
    private int sampleRate;
    private ulong startDSPClock;
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
        songPositionBeat = ((float)songPosition - (float)startingTimeDelay) / 1000f / beatSecondInterval;
        previousSongPositionBeat = ((float)songPosition - (float)startingTimeDelay) / 1000f / beatSecondInterval;

        beepInstance = RuntimeManager.CreateInstance(beepReference);
        songInstance = RuntimeManager.CreateInstance(sd.songs[0]);

        tracker = new FMODBeatTracker(songInstance, beepInstance);
        tracker.SetPlayBeep(true);

        // Start playback
        songInstance.start();

        //RuntimeManager.StudioSystem.getCoreSystem(out FMOD.System coreSystem);
        //coreSystem.getSoftwareFormat(out sampleRate, out _, out _);
        //coreSystem.getMasterChannelGroup(out channelGroup);
        //channelGroup.getDSPClock(out startDSPClock, out ulong parent);
        //Debug.Log(startDSPClock);
    }

    private void Update()
    {
        if (channelGroup.hasHandle()) {
            songInstance.getTimelinePosition(out int tposition);
            //timelinePosition = tposition;

            channelGroup.getDSPClock(out ulong dspClock, out ulong parent);
            timelinePosition = (dspClock - startDSPClock) / (float)sampleRate * 1000;
            songPosition += Time.deltaTime * 1000;

            Debug.Log("Timelinepos: " + (int)tposition + ", Clock: " + (int)timelinePosition);

            if (Input.GetKeyDown(KeyCode.Space)) {

            }
        } else {
            songInstance.getChannelGroup(out channelGroup);
            channelGroup.getDSPClock(out ulong dspClock, out ulong parent);
            startDSPClock = dspClock;
            Debug.Log(startDSPClock);
        }
        
    }

    public float GetSongTime()
    {
        return songPosition;
    }

    public float GetSongBeat()
    {
        return songPositionBeat;
    }

    public float GetBeatInterval()
    {
        return beatSecondInterval;
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

    public EventInstance GetSongInstance(EventReference eventReference)
    {
        return songInstance;
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



