using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BrawlSettings : MonoBehaviour
{
    [Header("Team Settings")]
    public int teamPositionCount = 2;
    public int teamCount = 2;

    [Header("Game Settings")]
    public int stockCount = 3;
    public int respawnDelayReset = 180;
    public int spawnInvincibilityRemainingReset = 180;
    public int respawnNoControlsDelay = 60;
    public int gadgetDespawnTime = 60 * 8;
    public int maxTimeToWeaponSpawn = 60 * 5;
    public int minTimeToWeaponSpawn = 60 * 2;

    [Header("Visual Settings")]
    public bool useEffects = true;
    public Dictionary<Team, Color> m_teamColors = new Dictionary<Team, Color>() {
    { Team.RED, Color.red },
    { Team.BLUE, Color.blue }
    };
    public Color inactiveColor = Color.white;
    public Color activeColor = Color.green;
}
