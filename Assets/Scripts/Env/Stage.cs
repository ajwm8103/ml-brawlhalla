using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Stage : MonoBehaviour
{
    [System.Serializable]
    public struct BrawlBound
    {
        public Vector2 center;
        public Vector2 size;
        public BrawlBound(Vector2 center, Vector2 size){
            this.center = center;
            this.size = size;
        }

        public Vector3 GetSize(){
            return new Vector3(size.x, size.y, 0);
        }
    }

    public BrawlBound KOBounds;
    public BrawlBound WeaponSpawnBounds;
    public Transform initialWeaponSpawnOnes;
    public string stageName = "Small Brawlhaven";
    public StageType stageType = StageType.SMALL_BRAWLHAVEN;

    public List<TeamPositionSpawn> spawns;
    [System.Serializable]
    public struct TeamPositionSpawn
    {
        public int team;
        public int position;
        public Transform spawnTransform;
        public TeamPositionSpawn(int team, int position, Transform spawnTransform){
            this.team = team;
            this.position = position;
            this.spawnTransform = spawnTransform;
        }
    }
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube((Vector3)(KOBounds.center) + transform.position, KOBounds.GetSize());

        Gizmos.color = Color.green;
        Gizmos.DrawWireCube((Vector3)(WeaponSpawnBounds.center) + transform.position, WeaponSpawnBounds.GetSize());
    }
}
