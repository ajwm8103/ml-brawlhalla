using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Power Configuration", menuName = "ScriptableObject/Power Configuration", order = 1)]
public class PowerScriptableObject : ScriptableObject
{
    [Header("Power ID")]
    public int powerID = -1;
    [Header("Power Data")]
    public int fixedRecovery = 0;
    public int recovery = 0;
    public int cooldown = 0;
    public int minCharge = 0; // For the entire power, evidenced by axe gp
    public int stunTime = 0;
    [Header("Hit Angle")]
    public float hitAngleDeg = 0f; // right then counterclockwise
    [Header("Damage and Charge")]
    public bool isCharge = false; // if true, last cast is designated charge and cuts off after minCharge time
    public bool damageOverLifeOfHitbox = false; // i.e. teros hammer nsig
    [Header("Gravity")]
    public bool disableCasterGravity = false;
    public bool disableHitGravity = false;
    [Header("Hit Targeting  and Transitions")]
    public bool targetAllHitAgents = false; // true for unarmed nair, false for bodvar hammer dsig
    public bool transitionOnInstantHit = false; // i.e. unarmed dlight
    [Header("On Hit Velocity Set")]
    public bool onHitVelocitySetActive = false; // i.e. unarmed nair hit stops velocity
    public float onHitVelocitySetMagnitude = 0f;
    public float onHitVelocitySetDirectionDeg = 0f;
    [Header("Instantly On Ground Collide")]
    public PowerScriptableObject onGroundNextPower;
    [Header("If Hit, or Instantly On Hit")]
    public PowerScriptableObject onHitNextPower;
    [Header("If Missed")]
    public PowerScriptableObject onMissNextPower; // if doesn't matter, place next here :)
    [Header("Casts")]
    public CastScriptableObject[] casts;

    public Power GetPower(){
        Cast[] castsToAdd = new Cast[casts.Length];
        for (int i = 0; i < casts.Length; i++)
        {
            castsToAdd[i] = casts[i].GetCast();
        }
        Power p = new Power(this, castsToAdd);
        return p;
    }
}
// Need transitions betwen powers
// Need casts that know how to charge w/ respect to min charge time
// Need on miss
// Need force per cast? Or dragging stuff? Idk
// Need increased damage based on charge time, but also based on stats somehow, look into that

public class Power {
    public PowerScriptableObject powerData;
    public int castIdx = 0;
    public int totalFrameCount = 0;
    public int framesIntoRecovery = 0;
    public int recoveryFrames = 0;
    public bool hitAnyone = false;
    public bool dealtPositionTargetExists = false;
    public Vector2 currentDealtPositionTarget;
    public List<LegendAgent> agentsInMove;
    public Cast[] casts;
    private bool isSwitchingCasts = true;
    private List<Vector2> pastPointPositions;
    private LegendAgent m_Agent;

    // Calculated things
    private bool lastPower = false;

    public Power(PowerScriptableObject powerData, Cast[] casts){
        this.powerData = powerData;
        this.casts = casts;
        currentDealtPositionTarget = Vector2.zero;
        lastPower = powerData.onHitNextPower == null && powerData.onMissNextPower == null;
        agentsInMove = new List<LegendAgent>();

        if (casts != null && casts.Length != 0)
        {
            Cast lastCast = casts[casts.Length - 1];
            recoveryFrames = powerData.recovery + powerData.fixedRecovery;
        }
    }

    public bool DoPower(bool holdingKey, bool isHoldingMoveType, LegendAgent agent, MoveManager mm, out Power nextPower)
    {
        m_Agent = agent;
        //Debug.Log(string.Format("Doing power {0}.", powerData.powerID));

        bool done = false;
        bool transitioningDueToInstantHit = false;
        bool transitioningToNextPower = false;
        nextPower = null;

        agent.SetGravityDisabled(powerData.disableCasterGravity);

        // Check if done charging
        bool isPastMinCharge = totalFrameCount > powerData.minCharge;
        bool isPastMaxCharge = totalFrameCount > casts[casts.Length - 1].castData.startupFrames;
        if (powerData.isCharge && ((!holdingKey && isPastMinCharge) || isPastMaxCharge)) {
            //Debug.Log("Done charge!");
            nextPower = powerData.onMissNextPower.GetPower();
        } else {
            // Not done
            Cast currentCast = casts[castIdx];
            CastFrameChangeHolder cfch = currentCast.GetFrameData(currentCast.frameIdx);
            //Debug.Log(string.Format("Doing castIdx {0}, frameIdx {1}.", castIdx, currentCast.frameIdx));

            // Calculate force
            Vector3 hitVector = Vector3.zero;
            if (cfch != null && cfch.dealtPositionTarget != null && cfch.dealtPositionTarget.active){
                dealtPositionTargetExists = true;
                currentDealtPositionTarget = new Vector2(cfch.dealtPositionTarget.xOffset, cfch.dealtPositionTarget.yOffset);
            } else {
                dealtPositionTargetExists = false;
                currentDealtPositionTarget = Vector2.zero;
            }
            if (!dealtPositionTargetExists){
                // No target, so deal force instead
                float hitAngleDeg = currentCast.castData.hitAngleDeg.HasValue ? currentCast.castData.hitAngleDeg.Value  : powerData.hitAngleDeg;
                hitVector = new Vector3(Mathf.Cos(Mathf.Deg2Rad * hitAngleDeg), Mathf.Sin(Mathf.Deg2Rad * hitAngleDeg), 0f);
                hitVector.x *= mm.moveFacingDirection;
            }
            
            bool inStartup = currentCast.frameIdx < currentCast.castData.startupFrames;
            bool isInAttackFrames = currentCast.frameIdx < (currentCast.castData.startupFrames + currentCast.castData.attackFrames);
            bool inAttack = !inStartup && (isInAttackFrames || currentCast.castData.mustBeHeld);
            if (inStartup) {
                agent.DoCastFrameChanges(cfch, mm);
                // Visuals
                m_Agent.SetHitboxesToDraw();
            } else if (inAttack) {
                agent.DoCastFrameChanges(cfch, mm);
                // Visuals
                m_Agent.SetHitboxesToDraw(currentCast.castData.hitboxes, currentCast.castData.collisionCheckPoints, mm.moveFacingDirection);

                float castDamage = currentCast.castData.baseDamage; // update this for
                float damageToDeal = powerData.damageOverLifeOfHitbox ? castDamage / currentCast.castData.attackFrames : castDamage;

                // Check collision
                bool collided = false;
                if (isSwitchingCasts){
                    isSwitchingCasts = false;
                } else {
                    // Has past info to work with
                    
                    for (int i = 0; i < currentCast.castData.collisionCheckPoints.Length; i++)
                    {
                        CollisionCheckPoint point = currentCast.castData.collisionCheckPoints[i];
                        Vector3 pointOffset = BrawlHitboxUtility.GetHitboxOffset(point.xOffset, point.yOffset);
                        pointOffset.x *= mm.moveFacingDirection;
                        Vector3 pointPos = agent.transform.position + pointOffset;
                        Vector3 oldPointPos = pastPointPositions[i];
                        /*Collider[] hitColliders = Physics.OverlapSphere(pointOffset, 0f, LayerMask.GetMask("Stage"));
                        if (hitColliders == null || hitColliders.Length == 0){
                            collided = true;
                            break;
                        }*/
                        //Debug.Log(oldPointPos);
                        //Debug.Log(pointPos);
                        foreach (GroundSegment groundSegment in agent.envController.stage.groundSegments)
                        {
                            if (groundSegment.CheckIntersection(oldPointPos, pointPos)){
                                collided = true;
                                //Debug.Log("Collided with ground!");
                                break;
                            }
                        }
                    }
                }

                // Initialize past point info
                pastPointPositions = new List<Vector2>();
                foreach (CollisionCheckPoint point in currentCast.castData.collisionCheckPoints)
                {
                    Vector3 pointOffset = BrawlHitboxUtility.GetHitboxOffset(point.xOffset, point.yOffset);
                    pointOffset.x *= mm.moveFacingDirection;
                    Vector3 pointPos = agent.transform.position + pointOffset;
                    pastPointPositions.Add(pointPos);
                }

                if (currentCast.castData.mustBeHeld && !isHoldingMoveType)
                {
                    transitioningToNextPower = true;
                    nextPower = powerData.onMissNextPower.GetPower();
                }

                if (collided)
                {
                    transitioningToNextPower = true;
                    nextPower = (powerData.onGroundNextPower ? powerData.onGroundNextPower : powerData.onMissNextPower).GetPower();
                }

                


                // Check hitboxes
                bool hitboxHit = false;
                List<LegendAgent> hitAgents = new List<LegendAgent>();
                ContactFilter2D playerContactFilter = new ContactFilter2D();
                //playerContactFilter.SetLayerMask(BrawlHitboxUtility.s_playerHurtboxLayerMask);
                playerContactFilter = playerContactFilter.NoFilter();
                foreach (Hitbox hitbox in currentCast.castData.hitboxes)
                {
                    Vector3 hitboxOffset = BrawlHitboxUtility.GetHitboxOffset(hitbox.xOffset, hitbox.yOffset);
                    hitboxOffset.x *= mm.moveFacingDirection;
                    Vector3 hitboxPos = agent.transform.position + hitboxOffset;
                    //Debug.Log(hitboxPos);
                    //BrawlHitboxUtility.DrawHitbox(hitbox, hitboxPos);
                    Collider2D[] results = new Collider2D[10];
                    int hitNumber = Physics2D.OverlapCapsule(hitboxPos, BrawlHitboxUtility.GetHitboxSize(hitbox.width, hitbox.height),
                    CapsuleDirection2D.Horizontal, 0f, playerContactFilter, results);

                    if (hitNumber != 0)
                    {
                        //Debug.Log("waga waga boom boom");
                        foreach (Collider2D hitCollider in results)
                        {
                            if (hitCollider == null || hitCollider.gameObject.layer != 9) continue;
                            LegendAgent hitAgent = hitCollider.transform.parent.GetComponent<LegendAgent>();
                            if (hitAgent != agent && hitAgent.team != agent.team && hitAgent.spawnInvincibilityRemaining == 0)
                            {
                                // Hit a valid opponent
                                hitboxHit = true;

                                // Apply onHitVelocitySet
                                if (!hitAnyone)
                                {
                                    // First hit!
                                    if (powerData.onHitVelocitySetActive){
                                        Vector2 onHitVel = new Vector2(Mathf.Cos(Mathf.Deg2Rad * powerData.onHitVelocitySetDirectionDeg), Mathf.Sin(Mathf.Deg2Rad * powerData.onHitVelocitySetDirectionDeg)) * powerData.onHitVelocitySetMagnitude;
                                        agent.SetVelocity(onHitVel);
                                    }
                                }

                                hitAnyone = true;

                                
                                float forceMagnitude = (currentCast.castData.fixedForce + currentCast.castData.variableForce * hitAgent.damage * 0.02622f);


                                if (!hitAgents.Contains(hitAgent))
                                {
                                    // Hit this frame
                                    //Debug.Log(string.Format("Hit {0} with {1}", hitAgent.GetFullName(), "a move"));

                                    if (powerData.damageOverLifeOfHitbox){
                                        hitAgent.Damage(damageToDeal, powerData.stunTime, hitVector * (forceMagnitude / currentCast.castData.attackFrames));
                                    }
                                    hitAgents.Add(hitAgent);
                                }
                                if (!agentsInMove.Contains(hitAgent))
                                {
                                    // Newly hit by this power
                                    if (mm.hitAgent == null){
                                        mm.hitAgent = hitAgent;
                                    }
                                    if (!powerData.damageOverLifeOfHitbox){
                                        hitAgent.Damage(damageToDeal, powerData.stunTime, hitVector * forceMagnitude);
                                    }
                                    hitAgent.SetGravityDisabled(powerData.disableHitGravity);
                                    agentsInMove.Add(hitAgent);
                                }
                                if (!mm.allHitAgents.Contains(hitAgent)){
                                    hitAgent.JustGotHit(true);
                                    mm.allHitAgents.Add(hitAgent);
                                }
                            }
                        }
                    }
                }
                if (hitboxHit && powerData.transitionOnInstantHit) {
                    nextPower = (powerData.onHitNextPower ? powerData.onHitNextPower : powerData.onMissNextPower).GetPower();
                }
                if (castIdx == casts.Length-1 && lastPower){
                    framesIntoRecovery++; // If last cast, start counting recovery
                }
            }
            currentCast.frameIdx++;

            // Recovery stuff
            if (!transitioningToNextPower & !inAttack && !inStartup)
            {
                // Visuals
                m_Agent.SetHitboxesToDraw();
                // Switch to next cast or count recovery
                if (castIdx == casts.Length - 1)
                {
                    //Debug.Log("Last cast.");
                    if (framesIntoRecovery >= recoveryFrames)
                    {
                        //Debug.Log("You can jump now lol");
                        // Done power
                        if (lastPower){
                            //Debug.Log("ok, fr, jump!!!");
                            done = true;
                        } else {
                            if (hitAnyone)
                            {
                                nextPower = (powerData.onHitNextPower ? powerData.onHitNextPower : powerData.onMissNextPower).GetPower();
                            }
                            else
                            {
                                nextPower = powerData.onMissNextPower.GetPower();
                            }
                        }
                    } else {
                        framesIntoRecovery++;
                    }
                }
                else
                {
                    // Move to next cast
                    castIdx++;
                    isSwitchingCasts = true;
                }
            }
        }

        totalFrameCount++;

        return done;
    }
}
