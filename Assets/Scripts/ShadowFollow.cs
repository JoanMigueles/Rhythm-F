using UnityEngine;

public class ShadowFollow : MonoBehaviour
{
    public float floorY = 0f;
    private Transform target;

    private void Start()
    {
        target = transform.parent;
    }
    void LateUpdate()
    {
        if (target != null)
        {
            transform.position = new Vector3(target.position.x, floorY, transform.position.z);
            transform.rotation = Quaternion.identity;
        }
    }
}