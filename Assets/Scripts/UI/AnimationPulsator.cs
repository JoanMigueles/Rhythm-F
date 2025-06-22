using DG.Tweening;
using UnityEngine;

[RequireComponent(typeof(Animator))]
public class AnimationPulsator : MonoBehaviour
{
    public string pulseTrigger = "Pulse"; // animator trigger name
    private Animator animator; // assign in Inspector

    private void Start()
    {
        animator = GetComponent<Animator>();
    }
    public void Pulse()
    {
        animator.SetTrigger(pulseTrigger);
    }
}
