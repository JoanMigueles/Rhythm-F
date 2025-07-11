using UnityEngine;
using System.Collections.Generic;

public enum DialogState
{
    Inactive,
    Active,
    Paused,
    WaitingForCondition
}

[System.Serializable]
public class Mission
{
    public int targetAmount;
    public NoteType requiredNoteType;
    public bool swapsLanes = false;

    [HideInInspector] public int currentAmount;
    [HideInInspector] private bool lastLaneWasZero = false;

    public bool IsComplete => currentAmount >= targetAmount;

    public void StartMission()
    {
        lastLaneWasZero = false; // reset for each mission
        SpawnNextNote();
    }

    public void RegisterAction(bool miss)
    {
        if (IsComplete) return;

        if (!miss) {
            currentAmount++;
        }

        if (!IsComplete) {
            SpawnNextNote();
        }
    }

    public void SpawnNextNote()
    {
        Debug.Log("Spawned");
        float beat = Metronome.instance.GetBeatFromTime(Metronome.instance.GetTimelinePosition());
        float minTargetBeat = beat + 3.8f;
        float nextFourthBeat = Mathf.Ceil(minTargetBeat / 4f) * 4f;
        int time = Metronome.instance.GetTimeFromBeat(nextFourthBeat);

        int lane = 1;
        if (swapsLanes) {
            lane = lastLaneWasZero ? 1 : 0;
            lastLaneWasZero = !lastLaneWasZero;
        }

        Note note = NoteManager.instance.SpawnNote(new NoteData(time, lane, requiredNoteType, 500));
        note.transform.position = new Vector3(-10, 0, 0);
    }
}

    public class DialogueMissionManager : MonoBehaviour
{
    public static DialogueMissionManager instance { get; private set; }
    public DialogState state = DialogState.Inactive;

    [Header("Missions")]
    public List<Mission> missions = new List<Mission>();
    private int currentMissionIndex = -1;

    private Dialogue dialogue;

    private void Awake()
    {
        instance = this;
    }

    private void Start()
    {
        dialogue = GetComponent<Dialogue>();
        dialogue.StartDialogue();
    }

    public void SetState(DialogState newState)
    {
        state = newState;
    }

    public bool CanAdvanceDialogue()
    {
        return state == DialogState.Active;
    }

    public void WaitForNextMission()
    {
        currentMissionIndex++;

        if (currentMissionIndex < missions.Count) {
            state = DialogState.WaitingForCondition;
            missions[currentMissionIndex].SpawnNextNote();
        }
        else {
            state = DialogState.Active;
            dialogue.NextLine();
        }
    }

    public void RegisterMissionAction(bool miss)
    {
        if (state != DialogState.WaitingForCondition) return;

        if (currentMissionIndex < missions.Count) {
            Mission mission = missions[currentMissionIndex];
            mission.RegisterAction(miss);

            if (mission.IsComplete) {
                state = DialogState.Active;
                dialogue.NextLine();
            }
        }
    }
}
