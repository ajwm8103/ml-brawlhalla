using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Stance Configuration", menuName = "ScriptableObject/Stance Configuration", order = 1)]
public class StanceScriptableObject : ScriptableObject
{
    [Header("Stats")]
    public int attack = 6; // 1-10
    public int defense = 5; // 1-10
    public int dexterity = 6; // 1-10
    public int speed = 5; // 1-10
}