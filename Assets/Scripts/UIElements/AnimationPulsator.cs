using DG.Tweening;
using UnityEngine;

[RequireComponent(typeof(Animator))]
public class AnimationPulsator : MonoBehaviour
{
    public string pulseTrigger = "Pulse";
    private Animator animator;

    private void Start()
    {
        animator = GetComponent<Animator>();
        PulsatorManager.instance.AddPulsator(this);
    }
    public void Pulse()
    {
        animator.SetTrigger(pulseTrigger);
    }

    private void OnDestroy()
    {
        PulsatorManager.instance.RemovePulsator(this);
    }
}
