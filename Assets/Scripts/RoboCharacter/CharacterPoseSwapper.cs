using DG.Tweening;
using System.Collections.Generic;
using UnityEngine;

public enum BaseAnimationState
{
    Running,
    Surfing,
    Holding,
    Dead
}

public class CharacterPoseSwapper : MonoBehaviour
{
    [Header("Base Poses")]
    public GameObject runPose;
    public GameObject surfPose;
    public GameObject holdingPose;
    public GameObject deadPose;

    [Header("Action Poses")]
    public List<GameObject> hitPoses;
    public GameObject slashRightPose;
    public GameObject slashLeftPose;
    public GameObject jumpPose;
    public GameObject damagePose;

    private GameObject currentBasePose;
    private GameObject currentActionPose;


    [Header("Hoverboard")]
    public GameObject hoverBoard;
    public GameObject shadow;
    public Tween hoverTween;

    void Start()
    {
        hoverBoard.transform.SetParent(null); // detach from parent
        hoverBoard.transform.position = new Vector3(-10f, -0.37f, 0f);
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
                ShowHoverboard(false);
                break;
            case BaseAnimationState.Surfing:
                currentBasePose = surfPose;
                ShowHoverboard(true);
                break;
            case BaseAnimationState.Holding:
                currentBasePose = holdingPose;
                ReturnToBasePose();
                break;
            case BaseAnimationState.Dead:
                currentBasePose = deadPose;
                ShowHoverboard(false);
                shadow.SetActive(false);
                break;
        }
        
        if (currentActionPose == null)
            currentBasePose.SetActive(true);
    }

    public void TriggerJump()
    {
        PlayActionPose(jumpPose, "Jump");
    }

    public void TriggerDamage()
    {
        PlayActionPose(damagePose, "Damage");
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

    public void ShowHoverboard(bool show)
    {
        float targetX = show ? -1.75f : -10f;
        if (hoverTween != null)
            hoverTween.Kill();
        hoverTween = hoverBoard.transform.DOMoveX(targetX, 0.4f)
            .SetEase(Ease.InOutBack);
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

    private void OnDisable()
    {
        if (hoverBoard == null) return;
        hoverTween.Kill();
        hoverBoard.SetActive(false);
    }

    private void OnEnable()
    {
        if (hoverBoard == null) return;
        hoverBoard.SetActive(true);
        hoverBoard.transform.position = new Vector3(-10f, -0.37f, 0f);
    }
}