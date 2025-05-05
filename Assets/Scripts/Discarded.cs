
//songPositionBeat = ((float)songPosition - (float)startingTimeDelay) / 1000f / beatSecondInterval;
//previousSongPositionBeat = ((float)songPosition - (float)startingTimeDelay) / 1000f / beatSecondInterval;

//beepInstance = RuntimeManager.CreateInstance(beepReference);
//SetSongInstance(songs[0]);

//tracker = new FMODBeatTracker(songInstance, beepInstance);
//tracker.SetPlayBeep(true);

//RuntimeManager.StudioSystem.getCoreSystem(out FMOD.System coreSystem);
//coreSystem.getSoftwareFormat(out int sampleRate, out _, out _);

/*using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class UITimeline : MonoBehaviour
{
    [SerializeField] private float beatTiling = 10f;
    private float editorBeat = 0;
    private float timelineWidth;

    private Material material;

    private void OnValidate()
    {
        if (beatTiling < 0.1f)
            beatTiling = 0.1f;
    }

    private void Start()
    {
        timelineWidth = GetComponent<RectTransform>().rect.width;
        Image img = GetComponent<Image>();
        material = new Material(img.material);
        img.material = material;

        material.SetFloat("_Tiling", beatTiling);
        material.SetFloat("_Offset", -Mathf.Abs(editorBeat - Mathf.Floor(editorBeat)));
    }

    private void Update()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        editorBeat = Metronome.instance.GetTimelineBeatPosition();
        if (scroll != 0) {
            Metronome.instance.SetTimelinePosition((int)(Metronome.instance.GetTimelinePosition() + scroll * 1000));
        }
        material.SetFloat("_Offset", -Mathf.Abs(editorBeat - Mathf.Floor(editorBeat)));


        // Example: Adjust beat spacing dynamically (replace this with actual logic)
        if (Input.GetKeyDown(KeyCode.UpArrow)) {
            beatTiling -= 2f;
            material.SetFloat("_Tiling", beatTiling);

        }
        else if (Input.GetKeyDown(KeyCode.DownArrow)) {
            beatTiling += 2f;
            material.SetFloat("_Tiling", beatTiling);
        }
    }

    public (float start, float end) GetTimelineBeatWindow()
    {
        float beatSpacing = 1 / beatTiling;
        float halfRange = 0.5f / beatSpacing;
        float startBeat = editorBeat - halfRange;
        float endBeat = editorBeat + halfRange;
        return (startBeat, endBeat);
    }
}*/

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



/*
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

/*
public class FMODProgrammerSound
{
    private EventInstance eventInstance;
    private string audioPath;
    private GCHandle handle;
    private FMOD.Sound sound; // Store the sound instance
    private bool isSoundCreated = false; // Track if we've created the sound

    private EVENT_CALLBACK eventCallback;

    public FMODProgrammerSound(string songPath, EventReference eventRef)
    {
        audioPath = songPath;
        eventInstance = RuntimeManager.CreateInstance(eventRef);

        handle = GCHandle.Alloc(this);
        IntPtr handlePtr = GCHandle.ToIntPtr(handle);
        eventInstance.setUserData(handlePtr);

        eventCallback = new EVENT_CALLBACK(ProgrammerSoundCallback);
        eventInstance.setCallback(eventCallback, EVENT_CALLBACK_TYPE.CREATE_PROGRAMMER_SOUND |
                                      EVENT_CALLBACK_TYPE.DESTROY_PROGRAMMER_SOUND |
                                      EVENT_CALLBACK_TYPE.DESTROYED);
    }

    [AOT.MonoPInvokeCallback(typeof(EVENT_CALLBACK))]
    private static FMOD.RESULT ProgrammerSoundCallback(EVENT_CALLBACK_TYPE type, IntPtr instancePtr, IntPtr parameterPtr)
    {
        IntPtr userData;
        EventInstance eventInstance = new EventInstance(instancePtr);
        eventInstance.getUserData(out userData);

        if (userData != IntPtr.Zero) {
            GCHandle handle = GCHandle.FromIntPtr(userData);

            if (type == EVENT_CALLBACK_TYPE.CREATE_PROGRAMMER_SOUND) {
                PROGRAMMER_SOUND_PROPERTIES parameter = (PROGRAMMER_SOUND_PROPERTIES)Marshal.PtrToStructure(parameterPtr, typeof(PROGRAMMER_SOUND_PROPERTIES));

                if (handle.Target is FMODProgrammerSound programmer) {
                    // Only create the sound if it hasn't been created yet
                    if (!programmer.isSoundCreated) {
                        FMOD.Sound sound;
                        RuntimeManager.CoreSystem.createSound(programmer.audioPath, FMOD.MODE.CREATESTREAM, out sound);
                        programmer.sound = sound; // Store the sound instance
                        programmer.isSoundCreated = true;
                    }

                    // Always use the stored sound instance
                    parameter.sound = programmer.sound.handle;
                }

                Marshal.StructureToPtr(parameter, parameterPtr, false);
            }

            else if (type == EVENT_CALLBACK_TYPE.DESTROY_PROGRAMMER_SOUND) {
                // Don't release the sound here - we'll release it when the object is destroyed
                // Just let FMOD know we're done with this particular instance
            }
            else if (type == EVENT_CALLBACK_TYPE.DESTROYED) {
                if (handle.Target is FMODProgrammerSound programmer) {
                    // Release the sound when the event is truly destroyed
                    if (programmer.sound.hasHandle()) {
                        programmer.sound.release();
                        programmer.sound.clearHandle();
                        programmer.isSoundCreated = false;
                    }
                }
                handle.Free();
            }
        }

        return FMOD.RESULT.OK;
    }

    public EventInstance GetEventInstance()
    {
        return eventInstance;
    }

    // Add a proper cleanup method
    public void Release()
    {
        if (eventInstance.hasHandle()) {
            eventInstance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
            eventInstance.release();
        }

        // The sound will be released in the DESTROYED callback
    }
}*/


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