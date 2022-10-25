using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FinalRewardNumberController : MonoBehaviour
{
    // Update is called once per frame
    void Update()
    {
        transform.position -= Vector3.up * 1 * Time.deltaTime;
    }
}
