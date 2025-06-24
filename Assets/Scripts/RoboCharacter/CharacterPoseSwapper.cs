using System.Collections.Generic;
using UnityEngine;

public enum BaseAnimationState
{
    Running,
    Surfing,
    Holding
}

public class CharacterPoseSwapper : MonoBehaviour
{
    [Header("Base Poses")]
    public GameObject runPose;
    public GameObject surfPose;
    public GameObject holdingPose;

    [Header("Action Poses")]
    public List<GameObject> hitPoses;
    public GameObject slashRightPose;
    public GameObject slashLeftPose;
    public GameObject jumpPose;

    private GameObject currentBasePose;
    private GameObject currentActionPose;

    void Start()
    {
        SetBasePose(BaseAnimationState.Running); // Start with run
    }

    public void SetBasePose(BaseAnimationState state)
    {
        if (currentBasePose != null)
            currentBasePose.SetActive(false);

        switch (state)
        {
            case BaseAnimationState.Running:
                currentBasePose = runPose;
                break;
            case BaseAnimationState.Surfing:
                currentBasePose = surfPose;
                break;
            case BaseAnimationState.Holding:
                currentBasePose = holdingPose;
                ReturnToBasePose();
                break;
        }
        
        if (currentActionPose == null)
            currentBasePose.SetActive(true);
    }

    public void TriggerJump()
    {
        PlayActionPose(jumpPose, "Jump");
    }

    public void TriggerHit()
    {
        int index = Random.Range(0, hitPoses.Count);
        GameObject randomHitPose = hitPoses[index];

        PlayActionPose(randomHitPose, "Hit" + (index + 1).ToString());
    }

    public void TriggerSlash(bool left)
    {
        GameObject slashPose = left ? slashLeftPose : slashRightPose;
        string animationName = left ? "SlashL" : "SlashR";

        PlayActionPose(slashPose, animationName);
    }

    private void PlayActionPose(GameObject poseObject, string animationName)
    {
        if (currentActionPose != null)
        {
            currentActionPose.SetActive(false); // Interrupt
        }
        currentBasePose.SetActive(false);

        currentActionPose = poseObject;
        currentActionPose.SetActive(true);

        var anim = currentActionPose.GetComponent<Animator>();
        anim.Play(animationName, 0, 0f); // Force restart from beginning
    }

    public void ReturnToBasePose()
    {
        if (currentActionPose != null)
            currentActionPose.SetActive(false);

        currentActionPose = null;
        currentBasePose.SetActive(true);
    }
}