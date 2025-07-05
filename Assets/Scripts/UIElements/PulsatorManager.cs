using System.Collections.Generic;
using UnityEngine;

public class PulsatorManager : MonoBehaviour
{
    public static PulsatorManager instance;
    private List<Pulsator> pulsators;
    private List<AnimationPulsator> animators;

    private int lastBeat = -1;

    private void Awake()
    {
        instance = this;
        pulsators = new List<Pulsator>();
        animators = new List<AnimationPulsator>();
    }

    public void Pulse(int currentBeat)
    {
        foreach (var pul in pulsators) {
            if (pul != null && pul.gameObject.activeSelf) {
                pul.Pulse();
            }
        }
        if (currentBeat % 2 == 0) {
            foreach (var animator in animators) {
                if (animator != null && animator.gameObject.activeSelf) {
                    animator.Pulse();
                }
            }
        }
    }

    public void AddPulsator(Pulsator pul)
    {
        pulsators.Add(pul);
    }

    public void AddPulsator(AnimationPulsator anim)
    {
        animators.Add(anim);
    }

    public void RemovePulsator(Pulsator pul)
    {
        pulsators.Remove(pul);
    }

    public void RemovePulsator(AnimationPulsator anim)
    {
        animators.Add(anim);
    }
}
