using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponSpawn : MonoBehaviour
{
    public int timeUntilDespawn = 360;

    // Start is called before the first frame update
    void Start()
    {
        Destroy(gameObject, timeUntilDespawn / 60f);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
