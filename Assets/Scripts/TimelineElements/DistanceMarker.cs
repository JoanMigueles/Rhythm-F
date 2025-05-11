using UnityEngine;

public class DistanceMarker : MonoBehaviour
{
    public Transform elementA;
    public Transform elementB;

    void Update()
    {
        // Midpoint between the two circles
        Vector3 midPoint = (elementA.position + elementB.position) / 2f;
        transform.position = midPoint;

        // Distance between the circles
        float distance = Mathf.Abs(elementA.position.x - elementB.position.x);

        // Scale the square sprite along the X-axis
        transform.localScale = new Vector3(distance, transform.localScale.y, transform.localScale.z);

        // Optional: zero rotation since Y is constant
        transform.rotation = Quaternion.identity;
    }
}