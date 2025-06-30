using FMOD.Studio;
using FMODUnity;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Metronome : MonoBehaviour
{
    public static Metronome instance { get; private set; }

    [field: Header("Metronome")]
    [field: SerializeField] public EventReference beepReference { get; private set; }

    // Beat parameters
    [field: Header("Beat Parameters")]
    private List<BPMFlag> BPMFlags;
    public BPMFlag currentBPMFlag;
    public float beatSecondInterval;
    public bool metronomeBeeps;

    public bool isCustom;
    public bool smoothTimelineOn;
    private float headstartTimer;
    private int timelinePosition;
    private float smoothTimelinePosition;
    private int previousTimelinePosition;
    private float timelineBeatPosition;
    private int lastBeat;

    private FMODCustomMusicPlayer customPlayer;
    private EventInstance instancePlayer;
    private EventReference currentEventRef;
    public EventReference CurrentEventRef => currentEventRef;

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
        lastBeat = 0;
        isCustom = false;
        smoothTimelineOn = false;
        if (BPMFlags == null || BPMFlags.Count == 0) {
            BPMFlags = new List<BPMFlag> {new BPMFlag(0)};
        }
        beatSecondInterval = 60f / BPMFlags[0].BPM;
    }

    private void Update()
    {
        // CHECK WHICH BPM IS CURRENTLY ON
        currentBPMFlag = GetCurrentBPMFlag();
        beatSecondInterval = 60f / currentBPMFlag.BPM;

        // OBTAIN THE TIMELINE POSITION
        if (headstartTimer > 0) {
            headstartTimer -= Time.deltaTime;
            if (headstartTimer <= 0f && GameManager.instance.IsPlaying() && IsPaused()) {
                PlaySong();
                headstartTimer = 0f;
            }
        }

        if (isCustom && customPlayer != null) {
            timelinePosition = customPlayer.GetTimelinePosition() - (int)(headstartTimer * 1000);
        }
        else if (instancePlayer.isValid()) {
            instancePlayer.getTimelinePosition(out timelinePosition);
            timelinePosition -= (int)(headstartTimer * 1000);
        }

        // IF IS PLAYING, USE A SMOOTHER POSITION
        if (!IsPaused() && smoothTimelineOn) {
            smoothTimelinePosition += Time.deltaTime;
            timelinePosition = (int)(smoothTimelinePosition * 1000) - (int)(headstartTimer * 1000);
        }

        // SONG END DETECTION FOR CUSTOM SONGS
        if (isCustom && customPlayer != null && timelinePosition >= customPlayer.LengthInMS) {
            PauseSong();
        }

        // BEAT CHANGE DETECTION
        timelineBeatPosition = GetBeatFromTime(timelinePosition);
        int currentBeat = Mathf.FloorToInt(timelineBeatPosition);
        if (currentBeat != lastBeat) {
            if (metronomeBeeps && !IsPaused()) {
                RuntimeManager.PlayOneShot(beepReference);
            }
            lastBeat = currentBeat;
        }
    }
    // ---------------------------------------------------------------------------------------------------------------------------------------------
    // FLAGS
    // ---------------------------------------------------------------------------------------------------------------------------------------------
    public BPMFlag GetCurrentBPMFlag()
    {
        BPMFlag currentFlag;
        if (BPMFlags.Count == 0) {
            currentFlag = new BPMFlag(0);
            return currentFlag;
        }

        currentFlag = BPMFlags[0];
        foreach (BPMFlag flag in BPMFlags) {
            if (flag.offset > timelinePosition)
                break;

            currentFlag = flag;
        }

        return currentFlag;
    }

    public void SetBPMFlags(List<BPMFlag> flags)
    {
        if (flags == null || flags.Count == 0) {
            BPMFlags = new List<BPMFlag> { new BPMFlag(0) };
            return;
        }
        BPMFlags = flags;
    }

    // ---------------------------------------------------------------------------------------------------------------------------------------------
    // BEAT & TIME GETTERS
    // ---------------------------------------------------------------------------------------------------------------------------------------------
    public int GetTimelinePosition()
    {
        return timelinePosition;
    }

    public float GetNormalizedTimelinePosition()
    {
        if (customPlayer == null) return 0;
        if (customPlayer.LengthInMS == 0) return 0;
        return GetTimelinePosition() / (float)customPlayer.LengthInMS;
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

    // ---------------------------------------------------------------------------------------------------------------------------------------------
    // PLAYERS
    // ---------------------------------------------------------------------------------------------------------------------------------------------
    public void SetCustomSong(string songPath)
    {
        ReleasePlayers();
        Debug.Log("Setting custom song...");
        isCustom = true;
        currentEventRef = new EventReference();
        customPlayer = new FMODCustomMusicPlayer(songPath);
    }

    public void SetSong(EventReference eventRef)
    {
        ReleasePlayers();
        Debug.Log("Setting song...");
        isCustom = false;
        currentEventRef = eventRef;
        instancePlayer = RuntimeManager.CreateInstance(eventRef);
        instancePlayer.start();
        instancePlayer.setPaused(true);
    }

    public void ReleasePlayers()
    {
        Debug.Log("Releasing players...");
        if (customPlayer != null) customPlayer.Dispose();
        if (instancePlayer.isValid()) {
            instancePlayer.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
            instancePlayer.release();
        }
    }

    public uint GetSongLength()
    {
        if (isCustom && customPlayer != null) return customPlayer.LengthInMS;
        else if (instancePlayer.isValid()) {
            instancePlayer.getDescription(out EventDescription description);
            description.getLength(out int lengthMS); // length in milliseconds
            return (uint)lengthMS;
        }
        return 0;
    }

    public Texture2D GetSongWaveformTexture()
    {
        if (isCustom && customPlayer == null) return null;
        return customPlayer.GenerateWaveformTexture((int)(customPlayer.LengthInMS / 1000f * 50), 200);
    }

    public void SetTimelinePosition(int time)
    {
        if (time < 0) time = 0;
        if (isCustom && customPlayer != null)
            customPlayer.SetTimelinePosition(time);
        else if (instancePlayer.isValid())
            instancePlayer.setTimelinePosition(time);

        headstartTimer = 0;
        smoothTimelinePosition = time / 1000f;
    }

    public void SetNormalizedTimelinePosition(float pos)
    {
        pos = Math.Clamp(pos, 0f, 1f);
        int time = (int)(pos * customPlayer.LengthInMS);
        if (isCustom && customPlayer != null) 
            customPlayer.SetTimelinePosition(time);
        else if (instancePlayer.isValid())
            instancePlayer.setTimelinePosition(time);

        headstartTimer = 0;
        smoothTimelinePosition = time / 1000f;
    }

    public void PlaySong()
    {
        if (isCustom && customPlayer != null)
            customPlayer.Play();
        else if (instancePlayer.isValid())
            instancePlayer.setPaused(false);

        headstartTimer = 0;
        smoothTimelinePosition = timelinePosition / 1000f;
        if (EditorUI.instance != null) {
            EditorUI.instance.DisplayPause(true);
        }
    }

    public void PlaySongHeadstart(float headstart)
    {
        timelinePosition = 0;
        headstartTimer = headstart;
    }

    public void StopSong()
    {
        if (isCustom && customPlayer != null)
            customPlayer.Stop();
        else if (instancePlayer.isValid())
            instancePlayer.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);

        smoothTimelinePosition = timelinePosition / 1000f;
        if (EditorUI.instance != null) {
            EditorUI.instance.DisplayPause(false);
        }
    }

    public void PauseSong()
    {
        if (isCustom && customPlayer != null)
            customPlayer.Pause(true);
        else if (instancePlayer.isValid())
            instancePlayer.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);

        headstartTimer = 0;
        if (EditorUI.instance != null) {
            EditorUI.instance.DisplayPause(false);
        }
    }

    public bool IsPaused()
    {
        if (isCustom && customPlayer != null)
            return customPlayer.IsPaused;
        else if (instancePlayer.isValid()) {
            instancePlayer.getPaused(out bool paused);
            return paused;
        }
        return true;
    }

    public IEnumerator FadeIn(float duration = 2f)
    {
        if (isCustom && customPlayer != null) {
            customPlayer.Volume = 0;
            yield return new WaitForSeconds(0.2f);
            customPlayer.Pause(false);

            float elapsed = 0f;
            while (elapsed < duration) {
                if (customPlayer == null)
                    yield break;

                if (!customPlayer.IsPlaying || customPlayer.IsPaused)
                    yield break;

                elapsed += Time.deltaTime;
                float volume = Mathf.Clamp01(elapsed / duration);
                customPlayer.Volume = volume;

                yield return null;
            }

            customPlayer.Volume = 1f;
        }
        else if (instancePlayer.isValid()) {
            instancePlayer.setVolume(0f);
            instancePlayer.setPaused(false);

            float elapsed = 0f;
            while (elapsed < duration) {
                if (!instancePlayer.isValid())
                    yield break;

                instancePlayer.getPlaybackState(out PLAYBACK_STATE state);
                instancePlayer.getPaused(out bool paused);

                if (state == PLAYBACK_STATE.STOPPED || state == PLAYBACK_STATE.STOPPING || state == PLAYBACK_STATE.SUSTAINING || paused)
                    yield break;

                elapsed += Time.deltaTime;
                float volume = Mathf.Clamp01(elapsed / duration);
                instancePlayer.setVolume(volume);

                yield return null;
            }

            instancePlayer.setVolume(1f);
        }
    }

    public IEnumerator FadeOut(float duration = 2f)
    {
        if (isCustom && customPlayer != null) {
            customPlayer.Volume = 0;
            customPlayer.Pause(false);

            float elapsed = 0f;
            while (elapsed < duration) {
                if (customPlayer == null)
                    yield break;

                if (!customPlayer.IsPlaying || customPlayer.IsPaused)
                    yield break;

                elapsed += Time.deltaTime;
                float volume = Mathf.Lerp(1f, 0f, elapsed / duration);
                customPlayer.Volume = volume;

                yield return null;
            }

            customPlayer.Volume = 0f;
            customPlayer.Pause(true);
        }
        else if (instancePlayer.isValid()) {
            instancePlayer.setVolume(0f);

            float elapsed = 0f;
            while (elapsed < duration) {
                if (!instancePlayer.isValid())
                    yield break;

                instancePlayer.getPlaybackState(out PLAYBACK_STATE state);
                instancePlayer.getPaused(out bool paused);

                if (state == PLAYBACK_STATE.STOPPED || state == PLAYBACK_STATE.STOPPING || state == PLAYBACK_STATE.SUSTAINING || paused)
                    yield break;

                elapsed += Time.deltaTime;
                float volume = Mathf.Lerp(1f, 0f, elapsed / duration);
                instancePlayer.setVolume(volume);

                yield return null;
            }

            instancePlayer.setVolume(0f);
            instancePlayer.setPaused(true);
        }
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
        if (instancePlayer.isValid()) {
            instancePlayer.release();
            instancePlayer.clearHandle();
        }
    }

    void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus)
        {
            customPlayer?.Pause(true); // Pause when focus is lost
        }
        else
        {
            customPlayer?.Pause(false); // Resume if needed
        }
    }

    void OnApplicationPause(bool isPaused)
    {
        if (isPaused)
        {
            customPlayer?.Pause(true); // Pause on app suspend
        }
        else
        {
            customPlayer?.Pause(false); // Resume if appropriate
        }
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

        // === 2. Convert byte buffer to float samples ===
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

        // === 3. Downsample ===
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

        // === 4. Normalize ===
        float maxAmp = amplitudes.Max();
        if (maxAmp > 0) {
            for (int i = 0; i < amplitudes.Length; i++)
                amplitudes[i] /= maxAmp;
        }

        // === 5. Generate texture ===
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

        return tex;
    }
}
