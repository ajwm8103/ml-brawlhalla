using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;

[CreateAssetMenu(fileName = "Move Configuration", menuName = "ScriptableObject/Move Configuration", order = 1)]
public class MoveScriptableObject : ScriptableObject
{
    public PowerScriptableObject initialPower;
    public bool gadgetThrow = false; // true if weapon throw or any gadget
    public ActionKey actionKey;

    public MoveManager GetMove()
    {
        MoveManager mm = new MoveManager(this);
        return mm;
    }
}

public class MoveManager
{
    public MoveScriptableObject moveData;

    // Running Data
    public Power currentPower;
    public int frame = 0;
    public int moveFacingDirection = 1;
    public LegendAgent hitAgent;
    public List<LegendAgent> allHitAgents;

    public MoveManager (MoveScriptableObject moveData)
    {
        this.moveData = moveData;
        allHitAgents = new List<LegendAgent>();
        currentPower = moveData.initialPower.GetPower();
        frame = 0;
    }

    public bool DoMove(ActionSegment<int> action, LegendAgent agent)
    {
        //Debug.Log("Doing move.");
        // check if charging from action
        
        bool holdingMoveKey = action[(int)moveData.actionKey] == 1;
        if (moveData.gadgetThrow){
            holdingMoveKey = action[(int)ActionKey.PICKUP] == 1 || action[(int)ActionKey.LIGHT] == 1 || action[(int)ActionKey.HEAVY] == 1;
        }

        Power nextPower = null;
        bool done = currentPower.DoPower(holdingMoveKey, agent, this, out nextPower);
        if (nextPower != null){
            currentPower = nextPower;
        }

        /*if (!holdingMoveKey && frame > currentPower.powerData.minCharge)
        {
            Power nextPower = null;
            if (currentPower.DoPower(out nextPower))
            {
                currentPower = nextPower;
                frame = 0;
            }
        }*/

        frame++;
        return done;
    }
}