using FMOD.Studio;
using FMODUnity;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public class Metronome : MonoBehaviour
{
    public static Metronome instance { get; private set; }

    [field: Header("Songs")]
    [field: SerializeField] public List<EventReference> songs { get; private set; }

    [field: Header("Custom Songs")]
    [field: SerializeField] public EventReference customSongReference { get; private set; }

    [field: Header("Metronome")]
    [field: SerializeField] public EventReference beepReference { get; private set; }

    public EventInstance songInstance { get; private set; }
    private EventInstance beepInstance;

    // Beat parameters
    [field: Header("Beat Parameters")]
    private List<BPMFlag> BPMFlags;
    public BPMFlag currentBPMFlag;
    public float beatSecondInterval;
    public bool metronomeBeeps;

    private int timelinePosition;
    private float timelineBeatPosition;
    private int lastBeat;

    private FMODCustomMusicPlayer customPlayer;
    private bool isCustom;

    private void Awake()
    {
        isCustom = true;
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
        lastBeat = 0;
        if (BPMFlags == null || BPMFlags.Count == 0) {
            BPMFlags = new List<BPMFlag> {new BPMFlag(0)};
        }
        beatSecondInterval = 60f / BPMFlags[0].BPM;
    }

    private void Update()
    {
        int currentBeat = Mathf.FloorToInt(timelineBeatPosition);
        if (currentBeat != lastBeat) {
            if (metronomeBeeps && !IsPaused()) {
                beepInstance = RuntimeManager.CreateInstance(beepReference);
                beepInstance.start();
                beepInstance.release();
            }
            lastBeat = currentBeat;
        }

        currentBPMFlag = GetCurrentBPMFlag();
        beatSecondInterval = 60f / currentBPMFlag.BPM;
        if (isCustom && customPlayer != null) {
            timelinePosition = customPlayer.GetTimelinePosition();
            timelineBeatPosition = GetBeatFromTime(timelinePosition);
            if (timelinePosition >= customPlayer.LengthInMS - 100) {
                PauseSong();
            }
        } else {
            songInstance.getTimelinePosition(out int pos);
            timelinePosition = pos;
            timelineBeatPosition = GetBeatFromTime(timelinePosition);
        }
    }

    public int GetTimelinePosition()
    {
        return timelinePosition;
    }

    public float GetNormalizedTimelinePosition()
    {
        if (isCustom && customPlayer != null) {
            return customPlayer.GetNormalizedPosition();
        }
        return 0f;
    }

    public BPMFlag GetCurrentBPMFlag()
    {
        BPMFlag currentFlag = new BPMFlag(0);

        foreach (var flag in BPMFlags) {
            if (flag.offset <= timelinePosition && flag.offset >= currentFlag.offset) {
                currentFlag = flag;
            }
        }

        return currentFlag;
    }

    public float GetTimelineBeatPosition()
    {
        return timelineBeatPosition;
    }

    public float GetBeatFromTime(int time)
    {
        return (float)(time - currentBPMFlag.offset) / (beatSecondInterval * 1000f);
    }

    public int GetTimeFromBeat(float beat)
    {
        return Mathf.RoundToInt(beat * beatSecondInterval * 1000 + currentBPMFlag.offset);
    }
    public int GetTimeFromBeatInterval(float beat)
    {
        return Mathf.RoundToInt(beat * beatSecondInterval * 1000);
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

    public void SetCustomSong(string songPath)
    {
        ReleaseSongInstance();
        customPlayer = new FMODCustomMusicPlayer(songPath);
    }

    public void SetBPMFlags(List<BPMFlag> flags)
    {
        if (flags == null || flags.Count == 0) {
            BPMFlags = new List<BPMFlag> { new BPMFlag(0) };
            return;
        }
        BPMFlags = flags;
    }

    public uint GetSongLength()
    {
        if (isCustom && customPlayer != null) {
            return customPlayer.LengthInMS;
        }

        songInstance.getDescription(out EventDescription description);
        description.getLength(out int length);
        return (uint)length;
    }

    public Texture2D GetSongWaveformTexture()
    {
        return customPlayer.GenerateWaveformTexture((int)(customPlayer.LengthInMS / 1000f * 50), 200);
    }

    public void SetTimelinePosition(int time)
    {
        if (time < 0) time = 0;
        timelinePosition = time;

        if (isCustom && customPlayer != null) {
            customPlayer.SetTimelinePosition(timelinePosition);
            return;
        }

        if (songInstance.isValid()) {
            songInstance.setTimelinePosition(timelinePosition);
        }
    }

    public void SetNormalizedTimelinePosition(float pos)
    {
        if (isCustom && customPlayer != null) {
            customPlayer.SetNormalizedPosition(pos);
            return;
        }
    }

    public void PlaySong()
    {
        if (isCustom && customPlayer != null) {
            customPlayer.Play();
            if (EditorUIManager.instance != null) {
                EditorUIManager.instance.DisplayPause(true);
            }
            return;
        }

        if (songInstance.isValid()) {
            Debug.Log("song started");
            songInstance.start();
        } else {
            Debug.LogWarning("Invalid Instance");
        }
    }

    public void StopSong()
    {
        if (isCustom && customPlayer != null) {
            customPlayer.Stop();
            if (EditorUIManager.instance != null) {
                EditorUIManager.instance.DisplayPause(false);
            }
            return;
        }

        if (songInstance.isValid()) {
            Debug.Log("song started");
            songInstance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
        } else {
            Debug.LogWarning("Invalid Instance");
        }
    }

    public void PauseSong()
    {
        if (isCustom && customPlayer != null) {
            customPlayer.Pause(true);
            if (EditorUIManager.instance != null) {
                EditorUIManager.instance.DisplayPause(false);
            }
            return;
        }

    }

    public bool IsPaused()
    {
        if (isCustom && customPlayer != null) {
            return customPlayer.IsPaused;
        }
        return true;
    }

    public void SetMetronomeSound(bool on)
    {
        metronomeBeeps = on;
    }

    void OnDestroy()
    {
        if (customPlayer != null)
        {
            customPlayer.Dispose();
        }
        ReleaseSongInstance();
        beepInstance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
        beepInstance.release();
    }
}

public class FMODCustomMusicPlayer : IDisposable
{
    private FMOD.Sound sound;
    private FMOD.Channel channel;
    private FMOD.ChannelGroup musicChannelGroup;
    private string audioPath;
    private readonly FMOD.MODE soundMode = FMOD.MODE.CREATESTREAM | FMOD.MODE.LOOP_NORMAL | FMOD.MODE.ACCURATETIME;
    private uint lengthInMS;
    private bool isDisposed;

    public FMODCustomMusicPlayer(string songPath)
    {
        if (string.IsNullOrEmpty(songPath))
            throw new ArgumentException("Song path cannot be null or empty");

        audioPath = songPath;

        // Setup channel hierarchy
        InitializeChannelGroups();
        LoadSound();
        InitalizeSound();
    }

    private void InitializeChannelGroups()
    {
        RuntimeManager.CoreSystem.getMasterChannelGroup(out FMOD.ChannelGroup master);
        RuntimeManager.CoreSystem.createChannelGroup("Music", out musicChannelGroup);
        master.addGroup(musicChannelGroup);
        musicChannelGroup.setVolume(1.0f);
    }

    private void LoadSound()
    {
        if (sound.hasHandle()) sound.release();
        StopChannel();
        var result = RuntimeManager.CoreSystem.createSound(audioPath, soundMode, out sound);
        if (result != FMOD.RESULT.OK) {
            Debug.LogError($"Failed to load sound: {result} " + audioPath);
            return;
        }

        sound.getLength(out lengthInMS, FMOD.TIMEUNIT.MS);
    }

    public void InitalizeSound()
    {
        if (isDisposed) throw new ObjectDisposedException("FMODMusicPlayer");
        if (!sound.hasHandle()) return;

        StopChannel(); // Ensure any existing playback is stopped

        var result = RuntimeManager.CoreSystem.playSound(
            sound,
            musicChannelGroup,
            true, // Start paused
            out channel
        );

        if (result == FMOD.RESULT.OK) {
            channel.setPaused(true);
        }
        else {
            Debug.LogError($"Failed to play sound: {result}");
        }
    }

    public void Play()
    {
        if (IsPaused && channel.hasHandle()) {
            Pause(false);
        }
        else {
            InitalizeSound();
            Pause(false);
        }
    }

    public void Pause(bool pause)
    {
        if (isDisposed || !channel.hasHandle()) return;

        channel.setPaused(pause);
    }

    public void Stop()
    {
        if (isDisposed || !channel.hasHandle()) return;

        Pause(true);
        SetTimelinePosition(0);
    }

    public void StopChannel()
    {
        if (isDisposed || !channel.hasHandle()) return;

        channel.stop();
        channel.clearHandle();
    }

    // Timeline position control
    public int GetTimelinePosition(FMOD.TIMEUNIT unit = FMOD.TIMEUNIT.MS)
    {
        if (isDisposed || !channel.hasHandle()) return 0;

        channel.getPosition(out uint position, unit);
        return (int)position;
    }

    public void SetTimelinePosition(int position, FMOD.TIMEUNIT unit = FMOD.TIMEUNIT.MS)
    {
        if (isDisposed || !channel.hasHandle()) return;

        uint unsignedPosition = (position < 0) ? 0u : (uint)position;
        if (unit == FMOD.TIMEUNIT.MS) unsignedPosition = Math.Min(unsignedPosition, lengthInMS);
        channel.setPosition(unsignedPosition, unit);
    }

    public float GetNormalizedPosition()
    {
        if (lengthInMS == 0) return 0;
        return GetTimelinePosition() / (float)lengthInMS;
    }

    public void SetNormalizedPosition(float normalizedPosition)
    {
        normalizedPosition = Math.Clamp(normalizedPosition, 0f, 1f);
        SetTimelinePosition((int)(normalizedPosition * lengthInMS));
    }

    // Volume control
    public float Volume {
        get => musicChannelGroup.hasHandle() ?
            GetChannelGroupVolume(musicChannelGroup) : 1f;
        set {
            if (musicChannelGroup.hasHandle())
                musicChannelGroup.setVolume(Math.Clamp(value, 0f, 1f));
        }
    }

    private float GetChannelGroupVolume(FMOD.ChannelGroup group)
    {
        group.getVolume(out float volume);
        return volume;
    }

    // Cleanup
    public void Dispose()
    {
        if (isDisposed) return;

        StopChannel();

        if (sound.hasHandle()) {
            sound.release();
            sound.clearHandle();
        }

        if (musicChannelGroup.hasHandle()) {
            musicChannelGroup.release();
            musicChannelGroup.clearHandle();
        }

        Debug.Log("Music player disposed");
        isDisposed = true;
        GC.SuppressFinalize(this);
    }

    ~FMODCustomMusicPlayer() => Dispose();

    // Utility properties
    public bool IsPlaying {
        get {
            if (isDisposed || !channel.hasHandle()) return false;

            channel.isPlaying(out bool playing);
            return playing;
        }
    }

    public uint LengthInMS => lengthInMS;
    public bool IsPaused => IsPlaying && channel.getPaused(out bool paused) == FMOD.RESULT.OK && paused;

    public Texture2D GenerateWaveformTexture(int texWidth, int texHeight)
    {
        System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();

        stopwatch.Start();
        // === 1. Read PCM data ===
        FMOD.Sound sound;
        RuntimeManager.CoreSystem.createSound(
            audioPath,
            FMOD.MODE.OPENONLY | FMOD.MODE.ACCURATETIME,
            out sound
        );

        sound.getLength(out uint lengthPCM, FMOD.TIMEUNIT.PCM);
        sound.getFormat(out _, out FMOD.SOUND_FORMAT format, out int channels, out _);

        uint bytesPerSample = 2u; // For PCM16
        uint totalSamples = lengthPCM * (uint)channels;
        uint totalBytes = totalSamples * bytesPerSample;

        byte[] buffer = new byte[totalBytes];
        sound.readData(buffer, out uint bytesRead);
        sound.release();
        stopwatch.Stop();
        Debug.Log($"Time - Byte buffer extraction: {stopwatch.ElapsedMilliseconds} ms");

        // === 2. Convert byte buffer to float samples ===
        stopwatch.Restart();
        int sampleCount = (int)(bytesRead / bytesPerSample / channels);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++) {
            int index = i * channels * 2; // 2 bytes per channel
            short left = BitConverter.ToInt16(buffer, index);
            if (channels == 1) {
                samples[i] = left / 32768f;
            }
            else {
                short right = BitConverter.ToInt16(buffer, index + 2);
                samples[i] = ((left + right) / 2f) / 32768f;
            }
        }
        stopwatch.Stop();
        Debug.Log($"Time - Byte to float samples: {stopwatch.ElapsedMilliseconds} ms");

        // === 3. Downsample ===
        stopwatch.Restart();
        float[] amplitudes = new float[texWidth];
        int samplesPerPixel = samples.Length / texWidth;
        for (int i = 0; i < texWidth; i++) {
            float sum = 0f;
            for (int j = 0; j < samplesPerPixel; j++) {
                int idx = i * samplesPerPixel + j;
                sum += Mathf.Abs(samples[idx]);
            }
            amplitudes[i] = sum / samplesPerPixel;
        }
        stopwatch.Stop();
        Debug.Log($"Time - Downsampling: {stopwatch.ElapsedMilliseconds} ms");

        // === 4. Normalize ===
        stopwatch.Restart();
        float maxAmp = amplitudes.Max();
        if (maxAmp > 0) {
            for (int i = 0; i < amplitudes.Length; i++)
                amplitudes[i] /= maxAmp;
        }
        stopwatch.Stop();
        Debug.Log($"Time - Normalizing: {stopwatch.ElapsedMilliseconds} ms");

        // === 5. Generate texture ===
        stopwatch.Restart();

        Texture2D tex = new Texture2D(texWidth, texHeight, TextureFormat.Alpha8, false);
        tex.filterMode = FilterMode.Point;

        byte[] alphaData = new byte[texWidth * texHeight];
        int centerY = texHeight / 2;

        for (int x = 0; x < texWidth; x++) {
            int halfHeight = Mathf.RoundToInt(amplitudes[x] * (texHeight / 2f));
            int minY = Mathf.Max(0, centerY - halfHeight);
            int maxY = Mathf.Min(texHeight - 1, centerY + halfHeight);

            for (int y = minY; y <= maxY; y++) {
                int index = y * texWidth + x;
                alphaData[index] = 255; // blanco (visible)
            }
        }

        // Asignar los datos directamente
        tex.SetPixelData(alphaData, 0);
        tex.Apply(false, true);
        stopwatch.Stop();
        Debug.Log($"Time - Texture generation: {stopwatch.ElapsedMilliseconds} ms");

        return tex;
    }
}
