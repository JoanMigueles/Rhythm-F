using UnityEngine;

public class OnAnimationEnd : MonoBehaviour
{
    private CharacterPoseSwapper swapper;

    private void Start()
    {
        swapper = GetComponentInParent<CharacterPoseSwapper>();
    }

    public void ReturnToBasePose()
    {
        swapper.ReturnToBasePose();
    }
}
