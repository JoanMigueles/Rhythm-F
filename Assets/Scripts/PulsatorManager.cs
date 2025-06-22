using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PulsatorManager : MonoBehaviour
{
    public static PulsatorManager instance;
    public List<Pulsator> pulsators;
    public List<AnimationPulsator> animators;

    private int lastBeat = -1;

    private void Awake()
    {
        instance = this;
    }

    private void Update()
    {
        if (Metronome.instance == null) return;

        int currentBeat = Mathf.FloorToInt(Metronome.instance.GetTimelineBeatPosition());

        if (currentBeat != lastBeat) {
            lastBeat = currentBeat;
            foreach (var pul in pulsators) {
                pul.Pulse();
            }
            if (currentBeat % 2 == 0) {
                foreach (var animator in animators) {
                    animator.Pulse();
                }
            }
        }
    }

    public void AddPulsator(Pulsator pul)
    {
        pulsators.Add(pul);
    }

    public void RemovePulsator(Pulsator pul)
    {
        pulsators.Remove(pul);
    }
}
