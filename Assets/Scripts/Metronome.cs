using FMOD.Studio;
using FMODUnity;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Timeline;

public class Metronome : MonoBehaviour
{
    // Beat parameters
    public float BPM = 60f;
    private float beatSecondInterval;

    // Song parameters
    private EventInstance song;
    public int startingTimeDelay = 1000; //in ms
    private int songPosition; //in ms
    private float songPositionBeat;
    private float previousSongPositionBeat;
    //private float dspSongTime;

    private GameManager gm;
    private SongPlayer sp;


    private void Start()
    {
        gm = GameManager.instance;
        sp = SongPlayer.instance;

        beatSecondInterval = 60f / BPM;
        //dspSongTime = (float)AudioSettings.dspTime;
        songPositionBeat = ((float)songPosition - (float)startingTimeDelay) / 1000f / beatSecondInterval;
        previousSongPositionBeat = ((float)songPosition - (float)startingTimeDelay) / 1000f / beatSecondInterval;

        sp.CreateEventInstance(sp.songReference);
        song = sp.GetSong(sp.songReference);
        sp.Play(sp.songReference);
        FMODBeatTracker tracker = new FMODBeatTracker(song);
    }

    void Update()
    {
        if (gm.IsGameRunning()) {
            /*
            
            song.getTimelinePosition(out songPosition);

            // Adjusted beat calculation
            songPositionBeat = ((float)songPosition - (float)startingTimeDelay) / 1000f / beatSecondInterval;

            Debug.Log($"[Metronome] songPosition: {songPosition} ms, songPositionBeat: {songPositionBeat}, previousSongPositionBeat: {previousSongPositionBeat}");

            if (Mathf.Floor(songPositionBeat) != Mathf.Floor(previousSongPositionBeat) && songPositionBeat > 0) {
                Beep();
            }

            previousSongPositionBeat = songPositionBeat;*/
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

    public void Beep()
    {
        sp.Play(sp.beepReference);
    }
}

public class FMODBeatTracker
{
    private EventInstance instance;

    public FMODBeatTracker(EventInstance eventInstance)
    {
        instance = eventInstance;
        instance.setCallback(BeatEventCallback, EVENT_CALLBACK_TYPE.TIMELINE_BEAT);
        Debug.Log("Created callback");
    }

    private static FMOD.RESULT BeatEventCallback(EVENT_CALLBACK_TYPE type, IntPtr instancePtr, IntPtr parametersPtr)
    {
        if (type == EVENT_CALLBACK_TYPE.TIMELINE_BEAT) {
            Debug.Log("BEAT");
            TIMELINE_BEAT_PROPERTIES beatProperties =
                (TIMELINE_BEAT_PROPERTIES)System.Runtime.InteropServices.Marshal.PtrToStructure(parametersPtr, typeof(TIMELINE_BEAT_PROPERTIES));
            SongPlayer.instance.Play(SongPlayer.instance.beepReference);
            Debug.Log($"Beat: {beatProperties.beat}, Bar: {beatProperties.bar}");
        }
        return FMOD.RESULT.OK;
    }
}
