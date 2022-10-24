using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static BrawlEnvController;

[CreateAssetMenu(fileName = "Weapon Configuration", menuName = "ScriptableObject/Weapon Configuration", order = 1)]
public class WeaponScriptableObject : ScriptableObject
{
    public Weapon weaponType;
    public Vector2 throwSize;

    [SerializeField]
    [NamedArray(typeof(Move), new int[] { 0, 1, 2, 3, 4, 5, 9, 10 })]
    MoveScriptableObject[] m_moves = new MoveScriptableObject[]{
        null, // NLIGHT 0
        null, // SLIGHT 1
        null, // DLIGHT 2
        null, // NAIR 3
        null, // SAIR 4
        null, // DAIR 5
        null, // RECOVERY 9
        null, // GROUNDPOUND 10
    };

    public MoveManager GetMoveManager(Move move){
        if ((int)move >= 9){ // Recovery or Groundpound
            MoveScriptableObject moveObj = m_moves[(int)move - 3];
            if (moveObj){
                return moveObj.GetMove();
            } else {
                return null;
            }
        } else {
            MoveScriptableObject moveObj = m_moves[(int)move];
            if (moveObj)
            {
                return moveObj.GetMove();
            }
            else
            {
                return null;
            }
        }
    }
}
