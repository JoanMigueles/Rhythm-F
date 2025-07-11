using UnityEngine;
using FMODUnity;
using FMOD.Studio;
using System.Collections;

public class FMODEnemyMover : MonoBehaviour
{
    [SerializeField] private EventReference musicEvent;

    private EventInstance musicInstance;
    private FMOD.ChannelGroup channelGroup;
    private FMOD.ChannelGroup masterGroup;
    private FMOD.Channel channel;

    private ulong startDSPClock;
    private int sampleRate;

    private ulong time;
    private ulong previousTime;
    private double tChannel;
    private double previousTChannel;
    private double startTime;

    void Start()
    {
        Application.targetFrameRate = 1000;

        musicInstance = RuntimeManager.CreateInstance(musicEvent);
        musicInstance.start();

        RuntimeManager.CoreSystem.getSoftwareFormat(out sampleRate, out _, out _);
        RuntimeManager.CoreSystem.getMasterChannelGroup(out masterGroup);

        // Try to get the channel after playback starts
        Invoke(nameof(TryGetChannel), 0.1f);
    }

    private IEnumerator ResetTimelineAfterDelay(float delaySeconds)
    {
        yield return new WaitForSeconds(delaySeconds);

        musicInstance.setTimelinePosition(1000);
        startTime = 1000/1000f;
        // Reset the channel reference because FMOD recreates it
        channel = default;
        Invoke(nameof(TryGetChannel), 0.05f);

        yield return ResetTimelineAfterDelay(delaySeconds);
    }

    void TryGetChannel()
    {
        if (musicInstance.isValid()) {
            musicInstance.getChannelGroup(out channelGroup);
            if (channelGroup.hasHandle()) {
                channelGroup.getNumGroups(out int numGroups);
                if (numGroups > 0) {
                    channelGroup.getGroup(0, out FMOD.ChannelGroup auxGroup);
                    auxGroup.getChannel(0, out channel);
                }
            }
        }
    }

    void Update()
    {
        musicInstance.getPaused(out bool paused);
        if (Input.GetKeyDown(KeyCode.Space)) {
            musicInstance.setPaused(!paused);
        }

        if (!channel.hasHandle() || paused) {
            return;
        }

        channel.getDSPClock(out time, out _);
        masterGroup.getDSPClock(out ulong time2, out _);

        if (time == previousTime) {
            tChannel += Time.deltaTime;
        }
        else {
            tChannel = (double)time / sampleRate + startTime;
        }
        previousTime = time;
        previousTChannel = tChannel;

        musicInstance.getTimelinePosition(out int timelinePos);

        Debug.Log($"{tChannel:F3}, {timelinePos / 1000f:F3}");

        transform.position = new Vector3((float)tChannel * 3f - 3f, 0f, 0f);
    }

    void OnDestroy()
    {
        musicInstance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
        musicInstance.release();
    }
}
