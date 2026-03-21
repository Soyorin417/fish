using UnityEngine;

public class FishingSpot : MonoBehaviour
{
    [Header("µˆ”„µ„…Ë÷√")]
    public Transform castPoint;

    private void Reset()
    {
        BoxCollider col = GetComponent<BoxCollider>();
        if (col == null) col = gameObject.AddComponent<BoxCollider>();

        col.isTrigger = true;
        col.size = new Vector3(2f, 2f, 2f);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;

        if (castPoint != null)
        {
            Gizmos.DrawSphere(castPoint.position, 0.2f);
        }
    }
}