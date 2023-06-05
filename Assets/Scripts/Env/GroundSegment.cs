using UnityEngine;

public class GroundSegment : MonoBehaviour
{
    public Vector2 startPoint = new Vector2(-10, 0);
    public Vector2 endPoint = new Vector2(10, 0);
    public Vector3 startPointGlobal {
        get {
            return new Vector3(startPoint.x, startPoint.y) + transform.position;
        }
    }
    public Vector3 endPointGlobal
    {
        get
        {
            return new Vector3(endPoint.x, endPoint.y) + transform.position;
        }
    }

    private Vector2 ls;
    private Vector2 le;

    // Function to check intersection with a line segment.
    public bool CheckIntersection(Vector2 lineStart, Vector2 lineEnd)
    {
        ls = lineStart;
        le = lineEnd;
        Vector2 a = (Vector2)startPointGlobal;
        Vector2 b = (Vector2)endPointGlobal;
        Vector2 c = lineStart;
        Vector2 d = lineEnd;

        // Compute the intersection point
        float denominator = (b.x - a.x) * (d.y - c.y) - (b.y - a.y) * (d.x - c.x);

        // Check if lines are parallel
        if (denominator == 0)
        {
            return false;
        }

        float numerator1 = (a.y - c.y) * (d.x - c.x) - (a.x - c.x) * (d.y - c.y);
        float numerator2 = (a.y - c.y) * (b.x - a.x) - (a.x - c.x) * (b.y - a.y);

        float r = numerator1 / denominator;
        float s = numerator2 / denominator;

        // An intersection exists if r and s are between 0 and 1
        return (r >= 0 && r <= 1) && (s >= 0 && s <= 1);
    }


    // Gizmo to visualize the ground line in the scene view
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(startPointGlobal, endPointGlobal);
        Gizmos.color = Color.magenta;
        Gizmos.DrawLine(ls, le);
    }
}
