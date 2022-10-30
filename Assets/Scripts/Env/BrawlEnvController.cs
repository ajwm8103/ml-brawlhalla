using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;

public enum Weapon { UNARMED, HAMMER, SWORD };
public enum Move { NLIGHT, SLIGHT, DLIGHT, NAIR, SAIR, DAIR, NSIG, SSIG, DSIG, RECOVERY, GROUNDPOUND, NONE };
public enum ActionKey { AIMUP, AIMDOWN, LEFT, RIGHT, JUMP, PICKUP, LIGHT, HEAVY, DASH };
public enum Stance { DEFAULT, ATTACK, DEFENSE, DEXTERITY, SPEED };
public enum Team { RED, BLUE };
public enum LegendType { BODVAR };
public enum StageType { SMALL_BRAWLHAVEN };
public enum PlayerType { PLAYER1, PLAYER2, STAND, BOT, BOT_JUMP_TO_STAGE };

public class BrawlEnvController : MonoBehaviour
{
    // External references
    [HideInInspector]
    public Transform effectHolder;
    [HideInInspector]
    public Transform gadgetHolder;
    public Stage stage;

    public event System.Action OnMatchEnd;

    [System.Serializable]
    public class AgentInfo
    {
        public int stocks = 3;
        public LegendAgent agent;
        [HideInInspector]
        public Rigidbody2D rigidbody;
        [HideInInspector]
        public float totalReward;
        public int totalWins = 0;

        public void AddReward(float x){
            agent.AddReward(x);
            totalReward += x;
        }
    }
    private AgentInfo m_blueAgent;
    private AgentInfo m_redAgent;

    [HideInInspector]
    public float z {
        get { return transform.position.z;  }
    }

    // Match Params
    [Header("Match Params")]
    public int maxSteps = 1000;

    // Match details
    [Header("Match Stats")]
    public List<AgentInfo> agents;
    public Team firstTeam;
    [SerializeField]
    public int totalSteps
    {
        get {
            return m_totalSteps;
        }
        set {
            m_totalSteps = value;
            m_stepsRemaining = maxSteps - value;
        }
    }
    public int m_totalSteps;
    [SerializeField]
    public int stepsRemaining
    {
        get
        {
            return m_stepsRemaining;
        }
        set
        {
            m_stepsRemaining = value;
            m_totalSteps = maxSteps - value;
        }
    }
    public int m_stepsRemaining;

    //private SoccerSettings m_SoccerSettings;

    private SimpleMultiAgentGroup m_BlueAgentGroup;
    private SimpleMultiAgentGroup m_RedAgentGroup;

    private bool hasEnvCanvas;
    private Canvas envCanvas;
    private KeyboardUIManager keyboardUIManager;
    private StockViewManager stockViewManager;
    private Text m_totalWinsText;
    private Text m_roundTimer;
    BrawlSettings m_brawlSettings;

    // Weapon spawn stuff
    public WeaponSpawn weaponSpawnPrefab;
    private int m_initialWeaponSpawnTime = 60;
    private int m_timeUntilNextSpawn = 60;
    private bool hasSpawnedInitialWeapon = false;

    // Start is called before the first frame update
    void Start()
    {
        m_brawlSettings = FindObjectOfType<BrawlSettings>();
        effectHolder = transform.Find("Effect Holder");
        gadgetHolder = transform.Find("Gadget Holder");
        m_BlueAgentGroup = new SimpleMultiAgentGroup();
        m_RedAgentGroup = new SimpleMultiAgentGroup();

        Transform envCanvasTransform = transform.Find("EnvCanvas");
        hasEnvCanvas = envCanvasTransform != null;
        if (hasEnvCanvas){
            envCanvas = transform.Find("EnvCanvas").GetComponent<Canvas>();
            keyboardUIManager = envCanvas.GetComponentInChildren<KeyboardUIManager>();
            stockViewManager = envCanvas.GetComponentInChildren<StockViewManager>();
            m_totalWinsText = envCanvas.transform.Find("TotalWinsText").GetComponent<Text>();
            m_roundTimer = envCanvas.transform.Find("RoundTimer").GetComponent<Text>();
        }

        // Add Agents to groups
        foreach (AgentInfo agentInfo in agents)
        {
            agentInfo.rigidbody = agentInfo.agent.GetComponent<Rigidbody2D>();
            agentInfo.totalWins = 0;
            if (agentInfo.agent.team == Team.BLUE)
            {
                m_BlueAgentGroup.RegisterAgent(agentInfo.agent);
                m_blueAgent = agentInfo;
            }
            else
            {
                m_RedAgentGroup.RegisterAgent(agentInfo.agent);
                m_redAgent = agentInfo;
            }
        }
        ResetEnv();
    }

    void FixedUpdate()
    {
        totalSteps++;
        if (totalSteps >= maxSteps && maxSteps > 0)
        {
            //m_BlueAgentGroup.GroupEpisodeInterrupted();
            //m_RedAgentGroup.GroupEpisodeInterrupted();

            m_blueAgent.agent.EpisodeInterrupted();
            m_redAgent.agent.EpisodeInterrupted();
            ResetEnv();
        }
        m_roundTimer.text = string.Format("{0}/{1}", totalSteps, maxSteps);

        // Weapon spawn logic
        if (m_timeUntilNextSpawn == 0){
            // Spawn a weapon
            if (hasSpawnedInitialWeapon)
            {
                WeaponSpawn ws = Instantiate(weaponSpawnPrefab, stage.initialWeaponSpawnOnes.position, Quaternion.identity);
                ws.transform.parent = gadgetHolder;
            }
            else
            {
                Vector3 spawnPos = new Vector3(Random.Range(-1f,1f)*stage.WeaponSpawnBounds.size.x/2 + stage.WeaponSpawnBounds.center.x,
                Random.Range(-1f, 1f) * stage.WeaponSpawnBounds.size.y/2 + stage.WeaponSpawnBounds.center.y,
                transform.position.z);
                WeaponSpawn ws = Instantiate(weaponSpawnPrefab, transform.position + spawnPos, Quaternion.identity);
                ws.transform.parent = gadgetHolder;
            }

            // Reset Spawn Time
            m_timeUntilNextSpawn = Random.Range(m_brawlSettings.minTimeToWeaponSpawn, m_brawlSettings.maxTimeToWeaponSpawn);
        }
        

        // Update health
        if (hasEnvCanvas)
        {
            stockViewManager.DisplayStocks(agents);
        }

        // Tick ticks!
        m_timeUntilNextSpawn = Mathf.Max(0, m_timeUntilNextSpawn - 1);
    }

    public void AttemptPostAction(){
        bool valid = true;
        foreach (AgentInfo agentInfo in agents)
        {
            if (!agentInfo.agent.hasActed){
                valid = false;
                break;
            }
        }
        if (valid){
            foreach (AgentInfo agentInfo in agents)
            {
                //Debug.Log(string.Format("{0} {1}", agentInfo.agent.team, agentInfo.totalReward));
                agentInfo.agent.PostAction();
                agentInfo.agent.hasActed = false;
            }
        }
    }

    public void DisplayAction(Team team, int teamPosition, ActionSegment<int> action)
    {
        if (hasEnvCanvas){
            keyboardUIManager.DisplayAction(team, teamPosition, action);
        }
    }

    public Vector3 GetSpawnPos(Team team, int teamPosition){
        int teamNumber = team == firstTeam ? 1 : 0;
        foreach (Stage.TeamPositionSpawn spawn in stage.spawns)
        {
            if (spawn.team == teamNumber && spawn.position == teamPosition){
                return spawn.spawnTransform.localPosition;
            }
        }
        return new Vector3(0, 1.516f, 0);
    }

    public void KO(LegendAgent agent, bool groundedSinceLastBeenHit)
    {
        AgentInfo kodAgent = agent.team == Team.RED ? m_redAgent : m_blueAgent;
        AgentInfo opponent = agent.team == Team.RED ? m_blueAgent : m_redAgent;
        SimpleMultiAgentGroup agentGroup = agent.team == Team.RED ? m_RedAgentGroup : m_BlueAgentGroup;
        SimpleMultiAgentGroup opponentGroup = agent.team == Team.RED ? m_BlueAgentGroup : m_RedAgentGroup;
        foreach (AgentInfo agentInfo in agents)
        {
            if (agentInfo.agent == agent){
                agentInfo.stocks--;

                if (groundedSinceLastBeenHit){
                    // Extra negative reward for falling off on your own
                    kodAgent.AddReward(-0.6f);
                    opponent.AddReward(0.6f);

                    // magic stuff to counteract existential boost
                }

                // Cancel out time penalty for losing
                kodAgent.AddReward(-(float)stepsRemaining / maxSteps);

                // Damage reward
                float damageToReward = 1f / 200f; // 1 point for the damage required to get to darker red health
                float reward = Mathf.Max(0,200 - kodAgent.agent.damage) * damageToReward;
                kodAgent.AddReward(-reward);
                opponent.AddReward(reward);

                if (agentInfo.stocks == 0){
                    // End game
                    kodAgent.AddReward(-5);
                    opponent.AddReward(5);
                    opponent.totalWins++;

                    // Visual
                    m_totalWinsText.text = string.Format("{0} - {1}", m_redAgent.totalWins, m_blueAgent.totalWins);

                    OnMatchEnd();

                    kodAgent.agent.EndEpisode();
                    opponent.agent.EndEpisode();

                    //agentGroup.AddGroupReward(-5);
                    //opponentGroup.AddGroupReward(5);

                    //m_BlueAgentGroup.EndGroupEpisode();
                    //m_RedAgentGroup.EndGroupEpisode();
                    ResetEnv();
                } else {
                    kodAgent.AddReward(-1.5f);
                    opponent.AddReward(1.5f);

                    //agentGroup.AddGroupReward(-1);
                    //opponentGroup.AddGroupReward(1);
                }
            }
        }
    }

    public void AgentDamaged(Team team, float damage){
        /*SimpleMultiAgentGroup agentGroup = team == Team.RED ? m_RedAgentGroup : m_BlueAgentGroup;
        SimpleMultiAgentGroup opponentGroup = team == Team.RED ? m_BlueAgentGroup : m_RedAgentGroup;
        float damageToReward = 1f / 700f;
        float reward = damage * damageToReward;
        agentGroup.AddGroupReward(-reward);
        opponentGroup.AddGroupReward(reward);*/
        float damageToReward = 1f / 200f; // 1 point for the damage required to get to darker red health
        float reward = damage * damageToReward;
        if (team == Team.RED){
            m_redAgent.AddReward(-reward);
            m_blueAgent.AddReward(reward);
        } else {
            m_redAgent.AddReward(reward);
            m_blueAgent.AddReward(-reward);
        }
    }

    public void TargetReached(Team scoredTeam){
        if (scoredTeam == Team.BLUE){
            //m_BlueAgentGroup.AddGroupReward(1 - (float)totalSteps / maxSteps);
            m_BlueAgentGroup.AddGroupReward(5);
            m_RedAgentGroup.AddGroupReward(-1);
        } else {
            //m_RedAgentGroup.AddGroupReward(1 - (float)totalSteps / maxSteps);
            m_RedAgentGroup.AddGroupReward(5);
            m_BlueAgentGroup.AddGroupReward(-1);
        }
        m_BlueAgentGroup.EndGroupEpisode();
        m_RedAgentGroup.EndGroupEpisode();
        ResetEnv();
    }

    public void ResetEnv(){
        totalSteps = 0;
        firstTeam = Random.Range(0, 2) == 1 ? Team.RED : Team.BLUE;

        // Reset Agents
        foreach (AgentInfo agentInfo in agents)
        {
            //Debug.Log(string.Format("{0} {1}", agentInfo.agent.name, agentInfo.totalReward));
            //var randomPosX = Random.Range(-5f, 5f);
            agentInfo.stocks = m_brawlSettings.stockCount;
            agentInfo.totalReward = 0f;
            agentInfo.agent.SetAgentInfo(agentInfo);
            agentInfo.agent.ResetLegend();
            agentInfo.agent.isAlive = true;
            // Do something for respawning, randomize spawn points + instant spawn at start

            agentInfo.rigidbody.velocity = Vector3.zero;
            agentInfo.rigidbody.angularVelocity = 0f;
        }

        // Reset Weapon Spawns
        hasSpawnedInitialWeapon = false;
        m_timeUntilNextSpawn = m_initialWeaponSpawnTime;

        /*float randomAngle = Random.Range(0, 2 * Mathf.PI);
        island.position = new Vector3(Mathf.Cos(randomAngle), Mathf.Sin(randomAngle), 0);
        island.position *= 9.2f;*/
    }
}