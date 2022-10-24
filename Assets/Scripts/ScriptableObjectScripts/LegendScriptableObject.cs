using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using static BrawlEnvController;

[CreateAssetMenu(fileName = "Legend Configuration", menuName = "ScriptableObject/Legend Configuration", order = 1)]
public class LegendScriptableObject : ScriptableObject
{
    public string legendName = "Bodvar";
    public LegendType legendType = LegendType.BODVAR;
    public List<InputOutputSprite> legendSprites;

    [System.Serializable]
    public class InputOutputSprite
    {
        public Team team;
        public Sprite sprite;
    }

    public StanceScriptableObject[] stances = new StanceScriptableObject[] {
        null, // DEFAULT
        null, // ATTACK
        null, // DEFENSE
        null, // DEXTERITY
        null, // SPEED
    };
    [Header("Unarmed Weapon")]
    public WeaponScriptableObject unarmedWeapon;
    public MoveScriptableObject unarmedNSig;
    public MoveScriptableObject unarmedSSig;
    public MoveScriptableObject unarmedDSig;

    [Header("Primary Weapon")]
    public WeaponScriptableObject primaryWeapon;
    public MoveScriptableObject primaryNSig;
    public MoveScriptableObject primarySSig;
    public MoveScriptableObject primaryDSig;

    [Header("Secondary Weapon")]
    public WeaponScriptableObject secondaryWeapon;
    public MoveScriptableObject secondaryNSig;
    public MoveScriptableObject secondarySSig;
    public MoveScriptableObject secondaryDSig;
}
