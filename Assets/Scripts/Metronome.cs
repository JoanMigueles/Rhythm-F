using FMOD.Studio;
using FMODUnity;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Metronome handles all sound playing and beat tracking
public class Metronome : MonoBehaviour
{
    public static Metronome instance { get; private set; }

    [field: Header("Metronome")]
    [field: SerializeField] public EventReference beepReference { get; private set; }
    public bool metronomeBeeps;

    // Timeline position tracking
    private float timelinePosition = 0;
    private float previousTimelinePosition = 0;
    private int timelinePositionMs = 0;

    // Beat tracking
    private List<BPMFlag> BPMFlags;
    private BPMFlag currentBPMFlag;
    private float timelineBeatPosition = 0;
    private float beatSecondInterval = 0;
    private int lastBeat = 0;

    private FMODCustomMusicPlayer customPlayer;
    private EventInstance instancePlayer;
    private Coroutine fadeCoroutine;
    private bool noPlayers = false;
    private bool paused = true;
    private bool looping = false;

    private void Awake()
    {
        BPMFlags = new List<BPMFlag>{ new BPMFlag(0) };
        if (instance == null) {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else {
            Destroy(gameObject);
        }
    }

    private void Update()
    {
        currentBPMFlag = GetCurrentBPMFlag();
        beatSecondInterval = 60f / currentBPMFlag.BPM;

        if (noPlayers) {
            ResetTimelinePosition();
            return;
        }

        // Get the timeline position with each player
        // -- Custom player is a class that manually sets FMOD channels at low level by loading audio files at runtime, since FMOD Event instances dont allow other audio files that are not from
        // FMOD Studio Assets (making them useless for custom song playing)
        // -- Instance player is the main way of playing bult-in sounds and songs (from FMOD Studio)
        if (customPlayer != null)
            timelinePositionMs = customPlayer.GetTimelinePosition();
        else if (instancePlayer.isValid())
            instancePlayer.getTimelinePosition(out timelinePositionMs);

        // Smooth timeline position when playing (time from delta time), get an accurate timeline position when paused (time obtained from FMOD)

        // ---- Explanation ----
        // Since FMOD is async, the time obtained from it is NOT updated each frame, making it look "not smooth" when determining the position of enemies with time info,
        // for that reason, delta time is used only while the song is playing, ensuring the synch of the custom timer with the start of the playing state.
        // This also means that when paused, the time is synced back again with the audio system.
        // And also allows to get times past and before the song length range (since FMOD's timeline position only allows values in the duration range of the sound)
        // It's not the prettiest solution to the smooth time problem but it's what works best after testing many other options :)
        if (!IsPaused())
            timelinePosition += Time.deltaTime;
        else {
            bool withinSongRange = timelinePosition >= 0 && timelinePosition <= GetSongLength() / 1000f;
            if (withinSongRange) {
                float newTimelinePosition = timelinePositionMs / 1000f;
                timelinePosition = newTimelinePosition;
            }
        }

        // Check for song start for negative timeline positions (for song headstart behavior on gameplay)
        if (timelinePosition >= 0 && previousTimelinePosition < 0 && !IsPaused()) {
            timelinePosition = 0;
            PlaySong();
        }

        previousTimelinePosition = timelinePosition;


        // Check for song end for looping
        if (!looping)  {
            // Custom player DOES loop by default, force a pause by checking when it ends
            if (customPlayer != null) {
                if ((int)(timelinePosition * 1000) >= GetSongLength()) {
                    PauseSong();
                    SetTimelinePosition((int)GetSongLength());
                }
            }
        } else {
            if ((int)(timelinePosition * 1000) >= GetSongLength()) {
                PauseSong();
                SetTimelinePosition(0);
                PlaySong();
            }
        }

        // Beat Handling
        timelineBeatPosition = GetBeatFromTime(timelinePositionMs);
        int currentBeat = Mathf.FloorToInt(timelineBeatPosition);

        if (currentBeat != lastBeat) {
            if (PulsatorManager.instance != null) {
                PulsatorManager.instance.Pulse(currentBeat);
            }
            if (metronomeBeeps && !IsPaused()) {
                RuntimeManager.PlayOneShot(beepReference);
            }
            lastBeat = currentBeat;
        }
    }

    private void ResetTimelinePosition()
    {
        timelineBeatPosition = 0;
        lastBeat = 0;
        timelinePosition = 0;
        previousTimelinePosition = 0;
        timelinePositionMs = 0;
    }

    // ---------------------------------------------------------------------------------------------------------------------------------------------
    // FLAGS
    // ---------------------------------------------------------------------------------------------------------------------------------------------
    private BPMFlag GetCurrentBPMFlag()
    {
        BPMFlag currentFlag;
        if (BPMFlags.Count == 0) {
            currentFlag = new BPMFlag(0);
            return currentFlag;
        }

        currentFlag = BPMFlags[0];
        foreach (BPMFlag flag in BPMFlags) {
            if (flag.offset > GetTimelinePosition())
                break;

            currentFlag = flag;
        }

        return currentFlag;
    }

    public float GetCurrentBPM()
    {
        return currentBPMFlag.BPM;
    }

    public float GetBeatSecondInterval()
    {
        return beatSecondInterval;
    }

    public void SetBPMFlags(List<BPMFlag> flags)
    {
        if (flags == null || flags.Count == 0) {
            BPMFlags = new List<BPMFlag> { new BPMFlag(0) };
            return;
        }
        BPMFlags = flags;
        lastBeat = 0;
    }

    // ---------------------------------------------------------------------------------------------------------------------------------------------
    // BEAT & TIME GETTERS
    // ---------------------------------------------------------------------------------------------------------------------------------------------
    public int GetTimelinePosition()
    {
        return (int)(timelinePosition * 1000);
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

    public float GetSmoothTimelineBeatPosition()
    {

        return GetBeatFromTime((int)(timelinePosition * 1000));
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
        customPlayer = new FMODCustomMusicPlayer(songPath);
        noPlayers = false;
        paused = true;

    }

    public void SetSong(EventReference eventRef)
    {
        ReleasePlayers();
        Debug.Log("Setting song...");
        instancePlayer = RuntimeManager.CreateInstance(eventRef);
        instancePlayer.start();
        instancePlayer.setPaused(true);
        noPlayers = false;
        paused = true;
    }

    public void ReleasePlayers()
    {
        Debug.Log("Releasing players...");
        if (fadeCoroutine != null)
            StopCoroutine(fadeCoroutine);
        if (customPlayer != null) {
            customPlayer.Dispose();
            customPlayer = null;
        }
        if (instancePlayer.isValid()) {
            instancePlayer.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
            instancePlayer.release();
        }

        noPlayers = true;
        ResetTimelinePosition();
    }

    // ---------------------------------------------------------------------------------------------------------------------------------------------
    // SONG INFORMATION GETTERS
    // ---------------------------------------------------------------------------------------------------------------------------------------------
    public uint GetSongLength()
    {
        if (customPlayer != null) return customPlayer.LengthInMS;
        else if (instancePlayer.isValid()) {
            instancePlayer.getDescription(out EventDescription description);
            description.getLength(out int lengthMS); // length in milliseconds
            return (uint)lengthMS;
        }
        return 0;
    }

    public Texture2D GetSongWaveformTexture()
    {
        if (customPlayer == null) return null;
        return customPlayer.GenerateWaveformTexture((int)(customPlayer.LengthInMS / 1000f * 50), 200);
    }

    // ---------------------------------------------------------------------------------------------------------------------------------------------
    // PLAYBACK METHODS
    // ---------------------------------------------------------------------------------------------------------------------------------------------
    public void SetTimelinePosition(int time)
    {
        int timeForPlayers = Math.Clamp(time, 0, (int)GetSongLength());
        if (customPlayer != null)
            customPlayer.SetTimelinePosition(timeForPlayers);
        else if (instancePlayer.isValid())
            instancePlayer.setTimelinePosition(timeForPlayers);

        timelinePositionMs = timeForPlayers;
        timelinePosition = time / 1000f;
        timelineBeatPosition = GetBeatFromTime((int)(timelinePosition * 1000));
        lastBeat = Mathf.FloorToInt(timelineBeatPosition);
    }

    public void SetNormalizedTimelinePosition(float pos)
    {
        if (customPlayer == null) return;

        pos = Math.Clamp(pos, 0f, 1f);
        int time = (int)(pos * customPlayer.LengthInMS);
        customPlayer.SetTimelinePosition(time);

        timelinePositionMs = time;
        timelinePosition = time / 1000f;
        timelineBeatPosition = GetBeatFromTime((int)(timelinePosition * 1000));
        lastBeat = Mathf.FloorToInt(timelineBeatPosition);
    }

    public void PlaySong()
    {
        paused = false;
        if (GetTimelinePosition() >= 0) {
            if (customPlayer != null)
                customPlayer.Play();
            else if (instancePlayer.isValid())
                instancePlayer.setPaused(false);
        }

        if (EditorUI.instance != null) {
            EditorUI.instance.DisplayPause(true);
        }

        timelineBeatPosition = GetBeatFromTime((int)(timelinePosition * 1000));
        lastBeat = Mathf.FloorToInt(timelineBeatPosition);
    }

    public void StopSong()
    {
        paused = true;
        if (customPlayer != null)
            customPlayer.Stop();
        else if (instancePlayer.isValid())
            instancePlayer.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);

        if (EditorUI.instance != null) {
            EditorUI.instance.DisplayPause(false);
        }

        ResetTimelinePosition();
    }

    public void PauseSong()
    {
        Debug.Log("pause");
        paused = true;
        if (customPlayer != null)
            customPlayer.Pause(true);
        else if (instancePlayer.isValid())
            instancePlayer.setPaused(true);

        if (EditorUI.instance != null) {
            EditorUI.instance.DisplayPause(false);
        }

        timelineBeatPosition = GetBeatFromTime((int)(timelinePosition * 1000));
        lastBeat = Mathf.FloorToInt(timelineBeatPosition);
    }

    public bool IsPaused()
    {
        return paused;
    }

    private bool SongIsPlaying()
    {
        if (customPlayer != null) {
            return customPlayer.IsPlaying;
        } else {
            instancePlayer.getPlaybackState(out PLAYBACK_STATE state);
            return state == PLAYBACK_STATE.PLAYING;
        }

    }

    public void SetMetronomeSound(bool on)
    {
        metronomeBeeps = on;
    }

    public void SetLooping(bool looping)
    {
        this.looping = looping;
    }

    // ---------------------------------------------------------------------------------------------------------------------------------------------
    // FADERS
    // ---------------------------------------------------------------------------------------------------------------------------------------------
    public void AddFade(bool fadeIn, float duration = 2f)
    {
        if (fadeCoroutine != null)
            StopCoroutine(fadeCoroutine);

        if (fadeIn)
            fadeCoroutine = StartCoroutine(FadeIn(duration));
        else
            fadeCoroutine = StartCoroutine(FadeOut(duration));
    }

    private IEnumerator FadeIn(float duration = 2f)
    {
        if (customPlayer != null) {
            customPlayer.Volume = 0;
            yield return new WaitForSeconds(0.2f);
            PlaySong();

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
            PlaySong();

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

    private IEnumerator FadeOut(float duration = 2f)
    {
        if (customPlayer != null) {
            customPlayer.Volume = 0;
            PlaySong();

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
            PauseSong();
        }
        else if (instancePlayer.isValid()) {
            instancePlayer.setVolume(0f);
            PlaySong();

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
            PauseSong();
        }
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
