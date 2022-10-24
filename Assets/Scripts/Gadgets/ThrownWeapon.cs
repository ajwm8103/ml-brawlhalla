using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ThrownWeapon : MonoBehaviour
{
    [Header("Params")]
    public WeaponScriptableObject weaponScriptableObject;
    public int floorLife = 15;
    private int floorTime = 0;
    private Rigidbody2D m_rigidbody;
    private BrawlEnvController envController; // Set when instantiating
    private bool used = false;


    // Start is called before the first frame update
    void Start()
    {
        m_rigidbody = GetComponent<Rigidbody2D>();
    }

    public void FixedUpdate(){
        // Check for any player collision
        if (floorTime >= floorLife){
            Destroy(gameObject);
        }
        if (!used){
            Collider2D hitCollider = Physics2D.OverlapCapsule(transform.position, weaponScriptableObject.throwSize, CapsuleDirection2D.Horizontal, 0f, BrawlHitboxUtility.s_playerHurtboxLayerMask);

            if (hitCollider)
            {
                // Hit a Player
                used = true;
                LegendAgent agent = hitCollider.transform.parent.GetComponent<LegendAgent>();
                Debug.Log("Hit Player");
                agent.Damage(1.81f, 18, m_rigidbody.velocity);
                m_rigidbody.velocity *= -1;
            }
        }

        if (Vector2.Distance(m_rigidbody.velocity, Vector2.zero) <= 0.1f)
        {
            floorTime++;
        }
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, weaponScriptableObject.throwSize.x / 2f);
    }
}