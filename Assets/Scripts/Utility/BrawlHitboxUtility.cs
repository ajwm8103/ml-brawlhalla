using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class BrawlHitboxUtility
{
    private readonly static float s_pixelsToMeters = 1.024f / 320f;
    public readonly static LayerMask s_playerHurtboxLayerMask = LayerMask.GetMask("PlayerHurtbox");
    public static Vector3 GetHitboxOffset(float xOffset, float yOffset){
        return new Vector3(xOffset * 2f, -yOffset * 2f, 0) * s_pixelsToMeters;
    }

    public static Vector2 GetHitboxSize(float width, float height)
    {
        return new Vector2(width, height) * 2 * s_pixelsToMeters;
    }
    public static Vector2 GetHurtboxOffset(float xOffset, float yOffset)
    {
        return new Vector3(xOffset * 2f, -yOffset * 2f, 0) * s_pixelsToMeters;
    }

    public static Vector2 GetHurtboxSize(float width, float height)
    {
        return new Vector2(width, height) * s_pixelsToMeters;
    }

    public static void DrawHitbox(Hitbox hitbox, Vector2 pos, float z){
        //Debug.Log(hitbox.xOffset);
        Gizmos.color = Color.red;
        Vector2 size = GetHitboxSize(hitbox.width, hitbox.height);
        if (hitbox.width < hitbox.height){
            float radius = size.x * 0.5f;
            float halfHeight = size.y * 0.5f;
            float circleHeight = halfHeight - radius;

            //Gizmos.DrawWireSphere(pos, radius);
            Gizmos.DrawWireSphere((Vector3)(pos + Vector2.up * circleHeight) + Vector3.forward * z, radius);
            Gizmos.DrawWireSphere((Vector3)(pos - Vector2.up * circleHeight) + Vector3.forward * z, radius);
            Gizmos.DrawLine((Vector3)(pos + (Vector2.up * circleHeight) + Vector2.right * radius) + Vector3.forward * z, (Vector3)(pos - (Vector2.up * circleHeight) + Vector2.right * radius) + Vector3.forward * z);
            Gizmos.DrawLine((Vector3)(pos + (Vector2.up * circleHeight) - Vector2.right * radius) + Vector3.forward * z, (Vector3)(pos - (Vector2.up * circleHeight) - Vector2.right * radius) + Vector3.forward * z);
        } else if (hitbox.width == hitbox.height){
            float radius = size.x * 0.5f;
            Gizmos.DrawWireSphere((Vector3)(pos) + Vector3.forward * z, radius);
        } else {
            float radius = size.y * 0.5f;
            float halfLength = size.x * 0.5f;
            float circleLength = halfLength - radius;

            //Gizmos.DrawWireSphere(pos, radius);
            Gizmos.DrawWireSphere((Vector3)(pos + Vector2.right * circleLength) + Vector3.forward * z, radius);
            Gizmos.DrawWireSphere((Vector3)(pos - Vector2.right * circleLength) + Vector3.forward * z, radius);
            Gizmos.DrawLine((Vector3)(pos + (Vector2.right * circleLength) + Vector2.up * radius) + Vector3.forward * z, (Vector3)(pos - (Vector2.right * circleLength) + Vector2.up * radius) + Vector3.forward * z);
            Gizmos.DrawLine((Vector3)(pos + (Vector2.right * circleLength) - Vector2.up * radius) + Vector3.forward * z, (Vector3)(pos - (Vector2.right * circleLength) - Vector2.up * radius) + Vector3.forward * z);
        }
    }

    public static void DrawCapsule2D(Vector2 offset, Vector2 size, float z, Color color)
    {
        //Central sphere.
        Gizmos.color = color;
        if (size.x < size.y)
        {
            float radius = size.x * 0.5f;
            float halfHeight = size.y * 0.5f;
            float circleHeight = halfHeight - radius;

            //Gizmos.DrawWireSphere(pos, radius);
            Gizmos.DrawWireSphere((Vector3)(offset + Vector2.up * circleHeight) + Vector3.forward * z, radius);
            Gizmos.DrawWireSphere((Vector3)(offset - Vector2.up * circleHeight) + Vector3.forward * z, radius);
            Gizmos.DrawLine((Vector3)(offset + (Vector2.up * circleHeight) + Vector2.right * radius) + Vector3.forward * z, (Vector3)(offset - (Vector2.up * circleHeight) + Vector2.right * radius) + Vector3.forward * z);
            Gizmos.DrawLine((Vector3)(offset + (Vector2.up * circleHeight) - Vector2.right * radius) + Vector3.forward * z, (Vector3)(offset - (Vector2.up * circleHeight) - Vector2.right * radius) + Vector3.forward * z);
        }
        else if (size.x == size.y)
        {
            float radius = size.x * 0.5f;
            Gizmos.DrawWireSphere((Vector3)(offset) + Vector3.forward * z, radius);
        }
        else
        {
            float radius = size.y * 0.5f;
            float halfLength = size.x * 0.5f;
            float circleLength = halfLength - radius;

            //Gizmos.DrawWireSphere(pos, radius);
            Gizmos.DrawWireSphere((Vector3)(offset + Vector2.right * circleLength) + Vector3.forward * z, radius);
            Gizmos.DrawWireSphere((Vector3)(offset - Vector2.right * circleLength) + Vector3.forward * z, radius);
            Gizmos.DrawLine((Vector3)(offset + (Vector2.right * circleLength) + Vector2.up * radius) + Vector3.forward * z, (Vector3)(offset - (Vector2.right * circleLength) + Vector2.up * radius) + Vector3.forward * z);
            Gizmos.DrawLine((Vector3)(offset + (Vector2.right * circleLength) - Vector2.up * radius) + Vector3.forward * z, (Vector3)(offset - (Vector2.right * circleLength) - Vector2.up * radius) + Vector3.forward * z);
        }
    }
}
