using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Cast Configuration", menuName = "ScriptableObject/Cast Configuration", order = 1)]
public class CastScriptableObject : ScriptableObject
{
    [Header("Power Cast Data")]
    public int startupFrames = 0; // in each of these the animation and hit enemy position can change hurtbox of the player can change
    // array per frame of data and stuff
    public int attackFrames = 0; // stun time for the power is measured starting 1 after the start of attack frames
    public int baseDamage = 0;
    public float variableForce = 0;
    public float fixedForce = 0;
    [Header("Extra Data")]
    public Hitbox[] hitboxes;
    public List<CastFrameChangeHolder> frameChanges;

    public Cast GetCast(){
        Cast c = new Cast(this);
        return c;
    }
}

public class Cast {
    public CastScriptableObject castData;

    // Running data
    public int frameIdx = 0;

    public Cast(CastScriptableObject castData){
        this.castData = castData;
    }

    public CastFrameChangeHolder GetFrameData(int idx){
        foreach (CastFrameChangeHolder x in castData.frameChanges)
        {
            if (x.frame == idx){
                return x;
            }
        }
        return null;
    }
}

[System.Serializable]
public class CastFrameChangeHolder {
    public int frame;
    public CasterPositionChange casterPositionChange;
    public CasterVelocitySet casterVelocitySet;
    public CasterVelocitySetXY casterVelocitySetXY;
    public CasterVelocityDampXY casterVelocityDampXY;
    public DealtPositionTarget dealtPositionTarget;
    public HurtboxPositionChange hurtboxPositionChange;
    public CastFrameChangeHolder(){
        casterPositionChange = new CasterPositionChange();
        dealtPositionTarget = new DealtPositionTarget();
        casterVelocitySet = null;
        casterVelocitySetXY = null;
        casterVelocityDampXY = null;
        hurtboxPositionChange = new HurtboxPositionChange();
    }
}

[System.Serializable]
public class CasterPositionChange {
    public bool active = false;

    public float x = 0f;
    public float y = 0f;
}

[System.Serializable]
public class CasterVelocitySet {
    public bool active = false;

    public float magnitude = 0f;
    public float directionDeg = 0f;
}

[System.Serializable]
public class CasterVelocitySetXY
{
    public bool activeX = false;
    public float magnitudeX = 0f;
    public bool activeY = false;
    public float magnitudeY = 0f;
}

[System.Serializable]
public class CasterVelocityDampXY
{
    public bool activeX = false;
    public float dampX = 0f;
    public bool activeY = false;
    public float dampY = 0f;
}

[System.Serializable]
public class DealtPositionTarget
{
    public bool active = false;

    public int xOffset, yOffset;
}
[System.Serializable]
public class HurtboxPositionChange {
    public bool active = false;

    public int xOffset, yOffset;
    public int width = 290;
    public int height = 320;
}

[System.Serializable]
public class Hitbox
{
    public int xOffset, yOffset;
    public int width = 290;
    public int height = 320;
}