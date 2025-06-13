using UnityEngine;

public class DistanceMarker : MonoBehaviour
{
    public Transform elementA;
    public Transform elementB;

    void Update()
    {
        Vector3 midPoint = (elementA.position + elementB.position) / 2f;
        transform.position = midPoint;

        float distance = Mathf.Abs(elementA.position.x - elementB.position.x);

        transform.localScale = new Vector3(distance, transform.localScale.y, transform.localScale.z);
        transform.rotation = Quaternion.identity;
    }
}