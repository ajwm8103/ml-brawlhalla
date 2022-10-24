using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;

public class LegendAgent : Agent
{
    public int k_maxFallSpeed = 22;
    public LegendAgent opponent;

    [Header("Prefabs")]
    public GameObject groundedPrefab;
    public GameObject groundedJumpPrefab;
    public GameObject walledPrefab;
    public GameObject wallJumpPrefab;
    public GameObject airJumpPrefab;
    public GameObject airJumpBurntPrefab;
    public GameObject dashPrefab;
    public GameObject backDashPrefab;
    public GameObject respawnPrefab;

    // Child Objects
    private Text m_damageText;
    private Transform m_graphicsTransform;
    //private SpriteRenderer m_capsuleSprite;
    private SpriteRenderer m_legendSprite;
    private ParticleSystem m_KOTrail;
    [Header("Cosmetic Stuff")]
    public Color defaultColor;
    public Color stunColor;
    public Color sprintingColor;

    [Header("Legend Stats")]
    public LegendScriptableObject legend;
    public int attack = 6; // 1-10
    public int defense = 5; // 1-10
    public int dexterity = 6; // 1-10
    public int speed = 5; // 1-10

    [Header("Game Stats")]
    [SerializeField]
    public PlayerType playerType = PlayerType.PLAYER1;
    [SerializeField]
    private int funValue = 0; // 0-99
    public Team team;
    private Dictionary<Team, Color> m_teamColors = new Dictionary<Team, Color>() { // For gizmos only
    { Team.RED, Color.red },
    { Team.BLUE, Color.blue }
    };
    public int teamPosition; // 0, 1, 2 : determines spawnPos
    public float damage = 0f; // 0-700
    public Weapon weapon = Weapon.UNARMED;

    [Header("Movement Parameters")]
    [SerializeField]
    private float maxFallSpeed = 22;
    [SerializeField]
    private float moveSpeed = 6.75f;
    [SerializeField]
    private float sprintSpeed = 9.8f;
    [SerializeField]
    private float smoothTimeX = 0.33f;
    [SerializeField]
    private float airSmoothTimeX = 0.5f;

    [SerializeField]
    private int minSprintLength = 10;
    [SerializeField]
    private int backDashLengthInFrames = 5;

    [SerializeField]
    private float jumpSpeed = 8.904f;
    [Range(0.0f, 1.0f)]
    [SerializeField]
    private float jumpStopDamp = 1f;

    [SerializeField]
    private float wallJumpSpeed = 15;
    [SerializeField]
    private float wallSlideMaxSpeed = 5;
    [SerializeField]
    private float wallSlideFactor = 0.25f;
    [SerializeField]
    private float fastFallAcc = 50;
    [SerializeField]
    private float fastFallAccDash = 90;
    [SerializeField]
    private int m_framesUntilFastFallReset = 16;
    [SerializeField]
    private int m_framesUntilFastFallDashedReset = 3;
    [SerializeField]
    private int m_framesUntilExitDashJumpReset = 10;
    [SerializeField]
    private int m_framesUntilJumpReset = 14;
    [SerializeField]
    private float m_Existential;

    // Private vars

    // References to components
    BrawlEnvController envController;
    BrawlEnvController.AgentInfo m_agentInfo;
    Rigidbody2D m_rigidbody;
    ContactFilter2D m_contactFilter;
    BrawlSettings m_brawlSettings;
    Collider2D m_playerCollider;

    // States for movement
    Vector2 m_velocity;
    Vector2 m_dashVelocity;
    Vector2 m_damageVelocity;
    Vector2 m_targetVelocity;
    Vector2 m_setVelocity;
    bool m_setVelocityBool = false;
    bool m_justGotHit = false;
    bool m_grounded, m_walled, m_aerial, m_jumping, m_sprinting, m_backDashing;
    bool m_canDash = false;
    bool m_canDashJumpFastFall = false;
    [HideInInspector]
    public int facingDirection = 1; // 1 right, -1 left
    float m_wallFacing;
    int m_framesUntilJump = 0;
    int m_framesUntilFastFall = 0;
    int m_framesUntilExitDashJump = 0;
    int m_sprintCooldown = 0;
    int m_jumpsLeft = 2;
    int m_recoveriesLeft = 1;
    bool m_jumpKeyLast = false;
    bool m_groundedLast = false;
    bool m_walledLast = false;
    bool m_aerialLast = true;
    bool m_activatedJumpLast = false;
    float m_smoothXVel;

    // States for damage
    [HideInInspector]
    public int stunFrames;

    public CapsuleCollider2D hurtboxCollider
    {
        get
        {
            return m_hurtboxCollider;
        }
    }
    public CapsuleCollider2D m_hurtboxCollider;

    // States for a move
    bool m_doingMove = false;
    [HideInInspector]
    public MoveManager currentMove = null;
    private Move m_currentMoveType = Move.NLIGHT;
    bool m_gravityCancelled = false;
    private bool m_gravityDisabled = false;

    // m_grounded, m_heavy, neutral down side
    struct CompactMoveState
    {
        public bool grounded;
        public bool heavy;
        public int directionType;

        public CompactMoveState(bool grounded, bool heavy, int directionType){
            this.grounded = grounded;
            this.heavy = heavy;
            this.directionType = directionType;
        }
    }

    private Dictionary<CompactMoveState, Move> m_stateToMove = new Dictionary<CompactMoveState, Move>(){
        {new CompactMoveState(true, false, 0), Move.NLIGHT}, // grounded light neutral
        {new CompactMoveState(true, false, 1), Move.DLIGHT}, // grounded light down
        {new CompactMoveState(true, false, 2), Move.SLIGHT}, // grounded light side
        {new CompactMoveState(true, true, 0), Move.NSIG}, // grounded heavy neutral
        {new CompactMoveState(true, true, 1), Move.DSIG}, // grounded heavy down
        {new CompactMoveState(true, true, 2), Move.SSIG}, // grounded heavy side
        {new CompactMoveState(false, false, 0), Move.NAIR}, // aerial light neutral
        {new CompactMoveState(false, false, 1), Move.DAIR}, // aerial light down
        {new CompactMoveState(false, false, 2), Move.SAIR}, // aerial light side
        {new CompactMoveState(false, true, 0), Move.RECOVERY}, // aerial heavy neutral
        {new CompactMoveState(false, true, 1), Move.GROUNDPOUND}, // aerial heavy down
        {new CompactMoveState(false, true, 2), Move.RECOVERY}, // aerial heavy side
    };

    // States for respawn
    public bool isAlive = true;
    private int m_timeUntilRespawn = 0;
    public int spawnInvincibilityRemaining = 0;
    public int spawnNoControlRemaining = 0;
    public Vector3 spawnPos;

    // States for action
    public bool hasActed = false;

    // States for damage
    private bool groundedSinceLastBeenHit = true;

    // Visual states
    public Hitbox[] m_hitboxesToDraw;
    private int m_moveFacingDirection;
    private bool flyingFast = false;

    EnvironmentParameters m_ResetParams;

    public override void Initialize()
    {
        base.Initialize();
        m_brawlSettings = FindObjectOfType<BrawlSettings>();

        envController = transform.parent.GetComponentInParent<BrawlEnvController>();
        m_graphicsTransform = transform.Find("Graphics");
        m_damageText = m_graphicsTransform.Find("Canvas").Find("DamageText").GetComponent<Text>();
        m_KOTrail = m_graphicsTransform.Find("KOTrail").GetComponent<ParticleSystem>();
        m_rigidbody = GetComponent<Rigidbody2D>();
        m_playerCollider = GetComponent<BoxCollider2D>();
        m_hurtboxCollider = GetComponentInChildren<CapsuleCollider2D>();
        //m_capsuleSprite = m_graphicsTransform.GetComponent<SpriteRenderer>();
        m_legendSprite = m_graphicsTransform.Find("Bodvar").GetComponent<SpriteRenderer>();
        m_contactFilter.SetLayerMask(Physics2D.GetLayerCollisionMask(gameObject.layer));
        m_contactFilter.useTriggers = false;
        sprintSpeed = moveSpeed * (4f / 3f);
        m_Existential = 1f / envController.maxSteps;

        // Set params ?

        m_ResetParams = Academy.Instance.EnvironmentParameters;

        ResetLegend();
    }

    public void SetAgentInfo(BrawlEnvController.AgentInfo x){
        m_agentInfo = x;
    }

    public void ResetLegend(){
        // Select Random Legend
        // TBD

        // Set stats
        StanceScriptableObject stance = legend.stances[(int)Stance.DEFAULT];
        attack = stance.attack;
        defense = stance.defense;
        dexterity = stance.dexterity;
        speed = stance.speed;

        // Set visuals
        foreach (LegendScriptableObject.InputOutputSprite ioSprite in legend.legendSprites)
        {
            if (ioSprite.team == team){
                m_legendSprite.sprite = ioSprite.sprite;
            }
        }

        Respawn();
    }

    public struct VisibleState
    {
        public Vector3 localPosition;
        public Vector2 m_velocity;
        
        public int facingDirection; // 1 right, -1 left
        public float m_wallFacing;
        public bool m_grounded, m_groundedLast, m_walled, m_walledLast, m_aerial, m_aerialLast, m_jumping, m_activatedJumpLast, m_sprinting, m_backDashing;
        public bool m_canDash;
        public bool m_canDashJumpFastFall;
        public bool m_jumpKeyLast;
        public int m_framesUntilJump;
        public int m_framesUntilFastFall;
        public int m_framesUntilExitDashJump;
        public int m_timeUntilRespawn;
        public int spawnInvincibilityRemaining;
        public int m_sprintCooldown;
        public int m_jumpsLeft;
        public int m_recoveriesLeft;

        public bool isAlive;
        public bool m_gravityCancelled;
        public bool m_doingMove;
        public int stunFrames;
        public float damage;
        public Weapon weapon;
        public LegendType legendType;
        public int attack;
        public int defense;
        public int dexterity;
        public int speed;
        public Move m_currentMoveType;

        public VisibleState(Vector3 localPosition, Vector2 velocity, int facingDirection, float wallFacing, bool grounded, bool groundedLast, bool walled, bool walledLast, bool aerial, bool aerialLast, bool jumping, bool activatedJumpLast, bool sprinting, bool backDashing, bool canDash, bool canDashJumpFastFall, bool jumpKeyLast, int framesUntilJump, int framesUntilFastFall, int framesUntilExitDashJump, int timeUntilRespawn, int spawnInvincibilityRemaining, int sprintCooldown, int jumpsLeft, int recoveriesLeft,
        bool isAlive, bool gravityCancelled, bool doingMove, int stunFrames, float damage, Weapon weapon, LegendType legendType, int attack, int defense, int dexterity, int speed, Move currentMoveType
        
        )
        {
            this.localPosition = localPosition;
            m_velocity = velocity;
            this.facingDirection = facingDirection;
            m_wallFacing = wallFacing;
            m_grounded = grounded;
            m_groundedLast = groundedLast;
            m_walled = walled;
            m_walledLast = walledLast;
            m_aerial = aerial;
            m_aerialLast = aerialLast;
            m_jumping = jumping;
            m_activatedJumpLast = activatedJumpLast;
            m_sprinting = sprinting;
            m_backDashing = backDashing;
            m_canDash = canDash;
            m_canDashJumpFastFall = canDashJumpFastFall;
            m_jumpKeyLast = jumpKeyLast;
            m_framesUntilJump = framesUntilJump;
            m_framesUntilFastFall = framesUntilFastFall;
            m_framesUntilExitDashJump = framesUntilExitDashJump;
            m_timeUntilRespawn = timeUntilRespawn;
            this.spawnInvincibilityRemaining = spawnInvincibilityRemaining;
            m_sprintCooldown = sprintCooldown;
            m_jumpsLeft = jumpsLeft;
            m_recoveriesLeft = recoveriesLeft;

            this.isAlive = isAlive;
            m_gravityCancelled = gravityCancelled;
            m_doingMove = doingMove;
            this.stunFrames = stunFrames;
            this.damage = damage;
            this.weapon = weapon;
            this.legendType = legendType;
            this.attack = attack;
            this.defense = defense;
            this.dexterity = dexterity;
            this.speed = speed;
            m_currentMoveType = currentMoveType;
    }

        public void AddObservations(VectorSensor sensor){
            // 31
            //Debug.Log(localPosition.x);
            //Debug.Log(localPosition.y);
            sensor.AddObservation(localPosition.x);
            sensor.AddObservation(localPosition.y);
            sensor.AddObservation(m_velocity.x);
            sensor.AddObservation(m_velocity.y);
            sensor.AddObservation(facingDirection);
            sensor.AddObservation(m_wallFacing);
            sensor.AddObservation(m_grounded);
            //sensor.AddObservation(m_groundedLast);
            sensor.AddObservation(m_walled);
            //sensor.AddObservation(m_walledLast);
            //sensor.AddObservation(m_aerial);
            //sensor.AddObservation(m_aerialLast);
            sensor.AddObservation(m_jumping);
            //sensor.AddObservation(m_activatedJumpLast);
            sensor.AddObservation(m_sprinting);
            sensor.AddObservation(m_backDashing);
            sensor.AddObservation(m_canDash);
            //sensor.AddObservation(m_canDashJumpFastFall);
            //sensor.AddObservation(m_jumpKeyLast);
            sensor.AddObservation(m_framesUntilJump);
            sensor.AddObservation(m_framesUntilFastFall);
            //sensor.AddObservation(m_framesUntilExitDashJump);
            sensor.AddObservation(m_timeUntilRespawn);
            sensor.AddObservation(spawnInvincibilityRemaining);

            sensor.AddObservation(m_sprintCooldown);
            sensor.AddObservation(m_jumpsLeft);
            sensor.AddObservation(m_recoveriesLeft);
            sensor.AddObservation(isAlive);
            sensor.AddObservation(m_gravityCancelled);
            sensor.AddObservation(m_doingMove);
            sensor.AddObservation(stunFrames);
            sensor.AddObservation(damage);
            sensor.AddObservation((int)weapon);
            sensor.AddObservation((int)legendType);
            sensor.AddObservation(attack);
            sensor.AddObservation(defense);
            sensor.AddObservation(dexterity);
            sensor.AddObservation(speed);
            sensor.AddObservation((int)m_currentMoveType);
        }
    }

    private void ResetState(){
        // Reset Damage
        damage = 0;
        funValue = UnityEngine.Random.Range(0,100);
        weapon = Weapon.UNARMED;

        // Reset MovementState
        m_rigidbody.velocity = Vector2.zero;
        spawnPos = envController.GetSpawnPos(team, teamPosition);
        transform.localPosition = spawnPos;
        m_velocity = Vector2.zero;
        m_dashVelocity = Vector2.zero;
        m_damageVelocity = Vector2.zero;
        m_targetVelocity = Vector2.zero;
        m_setVelocity = Vector2.zero;
        m_setVelocityBool = false;
        m_justGotHit = false;
        facingDirection = 1;
        m_wallFacing = 0f;
        m_grounded = false;
        m_groundedLast = false;
        m_walled = false;
        m_walledLast = false;
        m_aerial = false;
        m_aerialLast = true;
        m_jumping = false;
        m_activatedJumpLast = false;
        m_sprinting = false;
        m_backDashing = false;
        m_canDash = false;
        m_canDashJumpFastFall = false;
        m_jumpKeyLast = false;
        m_framesUntilJump = 0;
        m_framesUntilFastFall = 0;
        m_framesUntilExitDashJump = 0;
        m_timeUntilRespawn = 0;
        spawnInvincibilityRemaining = 0;
        m_sprintCooldown = 0;
        m_smoothXVel = 0f;
        m_jumpsLeft = 2;
        m_recoveriesLeft = 1;

        // Reset move state
        m_doingMove = false;
        m_currentMoveType = Move.NONE;
        currentMove = null;
        m_gravityCancelled = false;
        m_gravityDisabled = false;

        flyingFast = false;
        hasActed = false;

        DoCastFrameChanges();
        SetHitboxesToDraw();
    }

    public string GetFullName(){
        return string.Format("{0} {1}", Enum.GetName(typeof(Team), team), legend.legendName);
    }

    public void Damage(float damageDealt, int stunDealt, Vector2 velocityDealt){
        // Temporary damage function
        damage = Mathf.Min(700, damage + damageDealt);
        stunFrames = stunDealt;
        m_damageVelocity = velocityDealt * 0.165f;

        // Set all movement stuff off
        m_backDashing = false;
        m_jumping = false;
        m_sprintCooldown = 0;
        m_jumpsLeft = m_jumpsLeft == 0 ? 1 : 2;

        envController.AgentDamaged(team, damageDealt);
    }

    public void Respawn(){
        transform.position = spawnPos;
        spawnNoControlRemaining = m_brawlSettings.respawnNoControlsDelay;
        ResetState();
    }

    // Called to start the game
    public override void OnEpisodeBegin()
    {
        // Reset the legend (this is the most useful comment ever)
        isAlive = true;
        ResetLegend();
    }

    // Called to see the same
    // josh don't touch cause it sees AI
    public override void CollectObservations(VectorSensor sensor)
    {
        // Target and Agent positions

        // Agent movement
        if (m_brawlSettings.teamPositionCount == 1){
            BrawlEnvController.AgentInfo myAgentInfo = null;
            BrawlEnvController.AgentInfo opponentAgentInfo = null;
            foreach (BrawlEnvController.AgentInfo agentInfo in envController.agents)
            {
                // Build VisibleState
                // Add it's observations
                if (agentInfo.agent == this)
                {
                    myAgentInfo = agentInfo;
                }
                else
                {
                    opponentAgentInfo = agentInfo;
                }
            }

            // My visible state
            VisibleState myVisibleState = GetVisibleState();
            myVisibleState.AddObservations(sensor);
            sensor.AddObservation(myAgentInfo.stocks);

            // Opponent visible state
            VisibleState opponentVisibleState = opponentAgentInfo.agent.GetVisibleState();
            opponentVisibleState.AddObservations(sensor);
            sensor.AddObservation(opponentAgentInfo.stocks);

            // Global
            sensor.AddObservation((int)envController.stage.stageType);
            sensor.AddObservation(envController.totalSteps / envController.maxSteps);
            sensor.AddObservation(envController.maxSteps);
        }

    }

    public VisibleState GetVisibleState() {
        VisibleState vs = new VisibleState(transform.localPosition, m_velocity, facingDirection, m_wallFacing, m_grounded, m_groundedLast, m_walled, m_walledLast, m_aerial, m_aerialLast, m_jumping, m_activatedJumpLast, m_sprinting, m_backDashing, m_canDash, m_canDashJumpFastFall, m_jumpKeyLast, m_framesUntilJump, m_framesUntilFastFall, m_framesUntilExitDashJump, m_timeUntilRespawn, spawnInvincibilityRemaining, m_sprintCooldown, m_jumpsLeft, m_recoveriesLeft,
        isAlive, m_gravityCancelled, m_doingMove, stunFrames, damage, weapon, legend.legendType, attack, defense, dexterity, speed, m_currentMoveType
        );
        return vs;
    }
    void MoveAgent(ActionSegment<int> action){
        //Debug.Log("Frame!");

        // Make a move every step if we're training, or we're overriding models in CI.
        var useFast = Academy.Instance.IsCommunicatorOn;

        if (!isAlive){
            // Check if respawned
            // Change this to access global env settings class
            if (m_timeUntilRespawn == 0){
                isAlive = true;
                m_timeUntilRespawn = 0;
                spawnInvincibilityRemaining = m_brawlSettings.spawnInvincibilityRemainingReset;
                Respawn();
            } else {
                m_timeUntilRespawn--;
            }
        }


        // Set m_velocity
        m_velocity = m_rigidbody.velocity;

        // Tick ticks!
        stunFrames = Mathf.Max(0, stunFrames - 1);
        spawnInvincibilityRemaining = Mathf.Max(0, spawnInvincibilityRemaining - 1);
        spawnNoControlRemaining = Mathf.Max(0, spawnNoControlRemaining - 1);

        if (stunFrames == 0){
            m_gravityDisabled = false;
            flyingFast = false;
        } else {
            // In stun!

            // Visual
            if (m_velocity.magnitude > 10.24f){
                flyingFast = true;
            } else if (m_velocity.magnitude < 1f) {
                flyingFast = false;
            }
        }

        if (flyingFast)
        {
            m_KOTrail.Play();
        }
        else
        {
            m_KOTrail.Stop();
        }

        if (isAlive){
            // Check Move Manager
            ManageMoves(action);

            // Movement
            Movement(action);
        }

        hasActed = true;
        envController.AttemptPostAction();
    }

    public void PostAction(){
        // Collision stuff
        m_grounded = false;
        m_walled = false;

        if(m_justGotHit){
            groundedSinceLastBeenHit = false;
            m_velocity = Vector2.zero;
        }

        // Add damage velocity
        m_velocity += m_damageVelocity;
        m_velocity += m_targetVelocity;

        if (m_setVelocityBool)
        {
            m_velocity = m_setVelocity;
        }

        // Rigidbody casting
        RaycastHit2D[] hits = new RaycastHit2D[16];
        //int count = m_rigidbody.Cast(m_velocity, m_contactFilter, hits, m_velocity.magnitude * Time.fixedDeltaTime);
        int count = m_playerCollider.Cast(m_velocity, m_contactFilter, hits, m_velocity.magnitude * Time.fixedDeltaTime, true);
        for (int i = 0; i < count; i++)
        {
            var hit = hits[i];

            // Check if grounded
            if (hit.normal.y > 0 && stunFrames == 0 && !m_justGotHit)
            {
                m_grounded = true;
                groundedSinceLastBeenHit = true;
            }
            if (hit.normal.x != 0 & stunFrames == 0 && !m_justGotHit)
            {
                m_walled = true;
                m_wallFacing = hit.normal.x;
                groundedSinceLastBeenHit = true;
            }

            // Collision correction
            float dot2 = Vector2.Dot(m_velocity, hit.normal);
            if (dot2 < 0 && stunFrames != 0)
            {
                m_velocity = m_velocity - 2 * Vector2.Dot(m_velocity, hit.normal) * hit.normal;
            }
            float dot = Vector2.Dot(m_velocity - m_velocity.normalized * hit.distance / Time.fixedDeltaTime, hit.normal);
            if (dot < 0)
            {
                Vector2 collision = hit.normal * dot;
                m_velocity -= collision;
            }
        }
        // Fix ground wall thing
        if (m_grounded)
        {
            m_walled = false;
        }

        m_aerial = !m_grounded && !m_walled;

        // Effects
        if (!m_groundedLast && m_grounded)
        {
            // Just Grounded
            GameObject effectObject = Instantiate(groundedPrefab, transform.position, Quaternion.identity);
            effectObject.transform.parent = envController.effectHolder;
            Destroy(effectObject, 1f);
        }
        else if (!m_walledLast && m_walled)
        {
            // Just walled
            GameObject effectObject = Instantiate(walledPrefab, transform.position, Quaternion.identity);
            Vector3 theScale = effectObject.transform.localScale;
            theScale.x = -facingDirection;
            effectObject.transform.localScale = theScale;
            effectObject.transform.parent = envController.effectHolder;
            Destroy(effectObject, 1f);
        }

        // Setting velocity
        m_rigidbody.velocity = m_velocity;

        // Reset stuff
        m_setVelocity = Vector2.zero;
        m_setVelocityBool = false;
        m_justGotHit = false;
        m_damageVelocity = Vector2.zero;
        m_targetVelocity = Vector2.zero;
    }

    void ManageMoves(ActionSegment<int> action){
        // If not doing a move, search weapon scriptableobject and load the current move
        bool heavyMove = action[(int)ActionKey.HEAVY] == 1;
        bool lightMove = !heavyMove && action[(int)ActionKey.LIGHT] == 1;
        bool throwMove = !heavyMove && !lightMove && action[(int)ActionKey.PICKUP] == 1;

        bool leftKey = action[(int)ActionKey.LEFT] == 1;
        bool rightKey = action[(int)ActionKey.RIGHT] == 1;

        bool upKey = action[(int)ActionKey.AIMUP] == 1;
        bool sideKey = leftKey || rightKey;
        bool downKey = action[(int)ActionKey.AIMDOWN] == 1;

        bool neutralMove = (!sideKey && !downKey) || upKey;
        bool downMove = !neutralMove && downKey;
        bool sideMove = !neutralMove && !downKey && sideKey;
        bool hittingAnyMoveKey = lightMove || heavyMove || throwMove;

        //int cooldownOut = -1;
        if (!throwMove){
            CompactMoveState cms = new CompactMoveState(m_grounded, heavyMove, neutralMove ? 0 : (downMove ? 1 : 2));
            if (!m_doingMove && hittingAnyMoveKey && stunFrames == 0 && spawnNoControlRemaining == 0)
            { // check some sort of move cooldown here too...
              // About to do a move
                Move moveType = m_stateToMove[cms];
                bool moveWorked = false;
                if (m_grounded || m_aerial)
                {
                    //Debug.Log("wtf?");
                    // Special consideration for recovery
                    if (moveType == Move.RECOVERY) {
                        if (m_aerial && m_recoveriesLeft == 1)
                        {
                            moveWorked = true;
                            m_recoveriesLeft = 0;
                            currentMove = GetMoveManager(moveType);
                        }
                    } else {
                        moveWorked = true;
                        currentMove = GetMoveManager(moveType);
                    }
                    if (moveWorked && currentMove != null)
                    {
                        if (sideKey)
                        {
                            // If pressing any direction
                            facingDirection = rightKey ? 1 : -1;
                            currentMove.moveFacingDirection = facingDirection;
                        }
                        else
                        {
                            // Set to current direction
                            currentMove.moveFacingDirection = facingDirection;
                        }
                    } else {
                        moveWorked = false;
                    }
                }
                else if (m_gravityCancelled)
                {

                }

                if (moveWorked){
                    m_doingMove = true;
                    m_currentMoveType = moveType;
                } else {
                    m_currentMoveType = Move.NONE;
                }
                //Debug.Log(string.Format("{0} did a {1}", GetFullName(), Enum.GetName(typeof(Move), moveType)));
            }
        }
        
        
        if (m_doingMove) {
            bool done = stunFrames != 0;
            if (!done){
                done = currentMove.DoMove(action, this);
            }
            if (done){
                m_doingMove = false;
                m_currentMoveType = Move.NONE;

                m_gravityDisabled = false;
                SetHitboxesToDraw();
                DoCastFrameChanges();
            }
        }
    }

    public void SetPositionTargetVel(Vector2 vel){
        m_targetVelocity = vel;
    }

    public void SetGravityDisabled(bool val){
        m_gravityDisabled = val;
    }

    public void JustGotHit(bool still){
        m_justGotHit = true;
    }

    public void SetVelocity(Vector2 vel){
        m_setVelocityBool = true;
        m_setVelocity = vel;
    }

    public void DoCastFrameChanges(){
        CastFrameChangeHolder resetHolder = new CastFrameChangeHolder();
        resetHolder.hurtboxPositionChange.active = true;

        HurtboxPositionChange hpc = resetHolder.hurtboxPositionChange;
        Vector2 hurtboxOffset = BrawlHitboxUtility.GetHurtboxOffset(hpc.xOffset, hpc.yOffset);
        hurtboxOffset.x *= facingDirection;
        hurtboxCollider.offset = hurtboxOffset;
        hurtboxCollider.size = 2f * BrawlHitboxUtility.GetHurtboxSize(hpc.width, hpc.height);
    }

    public void DoCastFrameChanges(CastFrameChangeHolder changes, MoveManager mm){
        if (changes == null){
            return;
        }

        HurtboxPositionChange hpc = changes.hurtboxPositionChange;
        if (hpc != null && hpc.active){
            Vector2 hurtboxOffset = BrawlHitboxUtility.GetHurtboxOffset(hpc.xOffset, hpc.yOffset);
            hurtboxOffset.x *= mm.moveFacingDirection;
            hurtboxCollider.direction = hpc.width < hpc.height ? CapsuleDirection2D.Vertical : CapsuleDirection2D.Horizontal;
            hurtboxCollider.offset = hurtboxOffset;
            hurtboxCollider.size = 2f*BrawlHitboxUtility.GetHurtboxSize(hpc.width, hpc.height);
        }

        CasterPositionChange cpc = changes.casterPositionChange;
        if (cpc != null && cpc.active){

        }

        /*DealtPositionTarget dpt = changes.dealtPositionTarget;
        if (dpt != null && dpt.active)
        {
            if (mm.currentPower.powerData.targetAllHitAgents){
                Vector3 targetPos = BrawlHitboxUtility.GetHitboxOffset(mm.currentPower.currentDealtPositionTarget.x, mm.currentPower.currentDealtPositionTarget.y);
                targetPos.x *= mm.moveFacingDirection;
                foreach (LegendAgent agent in mm.allHitAgents)
                {
                    Vector2 vel = 30f * (transform.position + targetPos - agent.transform.position);
                    agent.SetPositionTargetVel(vel);
                }
            } else if (mm.hitAgent != null) {
                Vector3 targetPos = BrawlHitboxUtility.GetHitboxOffset(mm.currentPower.currentDealtPositionTarget.x, mm.currentPower.currentDealtPositionTarget.y);
                targetPos.x *= mm.moveFacingDirection;
                Vector2 vel = 30f * (transform.position + targetPos - mm.hitAgent.transform.position);
                mm.hitAgent.SetPositionTargetVel(vel);
            }
        }*/
        if (mm.currentPower.dealtPositionTargetExists){
            if (mm.currentPower.powerData.targetAllHitAgents)
            {
                Vector3 targetPos = BrawlHitboxUtility.GetHitboxOffset(mm.currentPower.currentDealtPositionTarget.x, mm.currentPower.currentDealtPositionTarget.y);
                targetPos.x *= mm.moveFacingDirection;
                foreach (LegendAgent agent in mm.allHitAgents)
                {
                    Vector2 vel = 0.5f * (transform.position + targetPos - agent.transform.position);
                    agent.SetPositionTargetVel(vel);
                }
            }
            else if (mm.hitAgent != null)
            {
                Vector3 targetPos = BrawlHitboxUtility.GetHitboxOffset(mm.currentPower.currentDealtPositionTarget.x, mm.currentPower.currentDealtPositionTarget.y);
                targetPos.x *= mm.moveFacingDirection;
                Vector2 vel = 0.5f * (transform.position + targetPos - mm.hitAgent.transform.position);
                mm.hitAgent.SetPositionTargetVel(vel);
            }
        }

        CasterVelocitySet cvs = changes.casterVelocitySet;
        if (cvs != null && cvs.active)
        {
            Vector2 vel = new Vector2(Mathf.Cos(Mathf.Deg2Rad * cvs.directionDeg), Mathf.Sin(Mathf.Deg2Rad * cvs.directionDeg)) * cvs.magnitude;
            vel.x *= mm.moveFacingDirection;
            m_velocity = vel;
        }

        CasterVelocitySetXY cvsxy = changes.casterVelocitySetXY;
        if (cvsxy != null)
        {
            if (cvsxy.activeX){
                m_velocity.x = cvsxy.magnitudeX * mm.moveFacingDirection;
            }
            if (cvsxy.activeY){
                m_velocity.y = cvsxy.magnitudeY;
            }
        }

        CasterVelocityDampXY cvdxy = changes.casterVelocityDampXY;
        if (cvdxy != null)
        {
            if (cvdxy.activeX)
            {
                m_velocity.x *= cvdxy.dampX;
            }
            if (cvdxy.activeY)
            {
                m_velocity.y *= cvdxy.dampY;
            }
        }
    }


    public void SetHitboxesToDraw()
    {
        m_hitboxesToDraw = null;
    }
    public void SetHitboxesToDraw(Hitbox[] hitboxes, int moveFacingDirection){
        m_moveFacingDirection = moveFacingDirection;
        m_hitboxesToDraw = hitboxes;
    }

    MoveManager GetMoveManager(Move move){        
        // Decision Data
        bool isSig = move == Move.NSIG || move == Move.DSIG || move == Move.SSIG;
        bool weaponIsPrimary = legend.primaryWeapon.weaponType == weapon;

        //Debug.Log(weaponIsPrimary);
        //Debug.Log(isSig);

        // Setting correct move vars
        WeaponScriptableObject myWeaponScriptableObject;
        MoveScriptableObject myNSig, myDSig, mySSig;

        if (weapon == Weapon.UNARMED){
            myWeaponScriptableObject = legend.unarmedWeapon;
            myNSig = legend.unarmedNSig;
            myDSig = legend.unarmedDSig;
            mySSig = legend.unarmedSSig;
        } else if (weaponIsPrimary){
            myWeaponScriptableObject = legend.primaryWeapon;
            myNSig = legend.primaryNSig;
            myDSig = legend.primaryDSig;
            mySSig = legend.primarySSig;
        } else {
            myWeaponScriptableObject = legend.secondaryWeapon;
            myNSig = legend.secondaryNSig;
            myDSig = legend.secondaryDSig;
            mySSig = legend.secondarySSig;
        }
        // Get Move
        if (isSig){
            switch (move)
            {
                case Move.NSIG:
                    return myNSig ? myNSig.GetMove() : null;
                case Move.DSIG:
                    return myDSig ? myDSig.GetMove() : null;
                default:
                    return mySSig ? mySSig.GetMove() : null;
            }
        } else
        {
            return myWeaponScriptableObject.GetMoveManager(move);
        }
    }

    void Movement(ActionSegment<int> action)
    {
        // 0 1 2 3 4     5 6 7 8
        // W S A D Space H J K L
        // i.e. action[0] gives 0 if no W and 1 if W
        // Process inputs
        Vector2 inputAxes = new Vector2(action[3] - action[2], action[0] - action[1]);
        bool downKey = action[1] == 1;
        bool jumpKey = action[4] == 1;
        bool dashKey = action[8] == 1;

        // This frame trackers
        bool activatedSprint = false;
        bool activatedJump = false;
        if (stunFrames > 0 || m_doingMove)
        {
            inputAxes = Vector2.zero;
            downKey = false;
            jumpKey = false;
            dashKey = false;
            m_sprintCooldown = 0;
            m_framesUntilExitDashJump = 0;
            m_framesUntilFastFall = 0;
            m_framesUntilJump = 0;
        } else {
            // Tick values
            m_framesUntilExitDashJump = Mathf.Max(0, m_framesUntilExitDashJump - 1);
            m_framesUntilFastFall = Mathf.Max(0, m_framesUntilFastFall - 1);
            m_framesUntilJump = Mathf.Max(0, m_framesUntilJump - 1);
            m_sprintCooldown = Mathf.Max(0, m_sprintCooldown - 1);

            // Fast falling
            if (inputAxes.y < 0 && m_framesUntilFastFall == 0)
            {
                float fallSpeedFF = Vector2.Dot(m_velocity, Physics2D.gravity.normalized);
                if (fallSpeedFF <= k_maxFallSpeed)
                {
                    float ffacc = m_canDashJumpFastFall ? fastFallAccDash : fastFallAcc;
                    float newFallSpeedFF = Mathf.Min(maxFallSpeed, fallSpeedFF + ffacc * Time.fixedDeltaTime);
                    m_velocity += (newFallSpeedFF - fallSpeedFF) * Physics2D.gravity.normalized;
                }
            }

            // Dashing and Sprinting
            if (!dashKey && m_grounded && m_sprintCooldown == 0) m_canDash = true;

            /*if (m_dashFrames > 0)
            {
                m_velocity = m_dashVelocity;
            }
            else */

            if (m_sprintCooldown == 0)
            {
                m_backDashing = false;
            }

            if (m_sprintCooldown == 0 && dashKey && m_canDash && m_grounded && inputAxes.x != 0)
            {
                // Dashing, but forward or back?
                // Needs updating for dodge and buffer system...
                m_framesUntilExitDashJump = m_framesUntilExitDashJumpReset;
                m_canDash = false;
                m_sprinting = true;
                m_sprintCooldown = minSprintLength;


                activatedSprint = true;
                if (inputAxes.x == facingDirection)
                {
                    // Forward Dash
                    m_dashVelocity.x = inputAxes.x * sprintSpeed;

                    // Rudimentary effect
                    GameObject effectObject = Instantiate(dashPrefab, transform.position, Quaternion.identity);
                    Vector3 theScale = effectObject.transform.localScale;
                    theScale.x = inputAxes.x;
                    effectObject.transform.localScale = theScale;
                    effectObject.transform.parent = envController.effectHolder;
                    Destroy(effectObject, 1f);
                }
                else
                {
                    // Back Dash
                    m_dashVelocity.x = inputAxes.x * sprintSpeed;
                    m_sprintCooldown = backDashLengthInFrames;
                    m_backDashing = true;

                    // Rudimentary effect
                    GameObject effectObject = Instantiate(backDashPrefab, transform.position, Quaternion.identity);
                    Vector3 theScale = effectObject.transform.localScale;
                    theScale.x = inputAxes.x;
                    effectObject.transform.localScale = theScale;
                    effectObject.transform.parent = envController.effectHolder;
                    Destroy(effectObject, 1f);
                }
                m_velocity += m_dashVelocity;
            }

            if (inputAxes.x == 0 && !m_backDashing) m_sprinting = false;

            // Jumping
            if (!jumpKey)
            {
                if (m_jumping && m_velocity.y > 0)
                {
                    m_velocity.y *= jumpStopDamp;
                }
                m_jumping = false;
            }

            // Dash jump fast fall stuff
            if (m_canDashJumpFastFall && m_velocity.y <= 0)
            {
                m_canDashJumpFastFall = false;
            }

            // Check position
            if (m_grounded)
            {
                // Grounded
                m_jumpsLeft = 2;
                m_recoveriesLeft = 1;
                m_framesUntilFastFall = m_framesUntilExitDashJump == 0 ? m_framesUntilFastFallReset : m_framesUntilFastFallDashedReset;
                m_framesUntilJump = m_framesUntilJumpReset;
                m_canDashJumpFastFall = false;
                if (jumpKey && !m_jumping)
                {
                    // Grounded Jump
                    if (m_framesUntilExitDashJump > 0)
                    {
                        // Dash jumping
                        m_canDashJumpFastFall = true;
                    }
                    else
                    {
                        // Not dash jumping
                    }
                    GameObject effectObject = Instantiate(groundedJumpPrefab, transform.position, Quaternion.identity);
                    effectObject.transform.parent = envController.effectHolder;
                    Destroy(effectObject, 1f);

                    m_velocity.y = jumpSpeed;
                    m_jumping = true;
                    activatedJump = true;
                }
                else
                {
                    m_jumping = false;
                }
            }
            else if (m_walled)
            {
                // Walled
                m_canDashJumpFastFall = false;
                m_sprintCooldown = 0;
                m_sprinting = false;
                m_jumpsLeft = 2;
                m_recoveriesLeft = 1;
                m_framesUntilFastFall = m_framesUntilFastFallReset;
                m_framesUntilJump = m_framesUntilJumpReset;
                if (jumpKey && !m_jumping)
                {
                    // Wall Jump
                    GameObject effectObject = Instantiate(wallJumpPrefab, transform.position, Quaternion.identity);
                    Vector3 theScale = effectObject.transform.localScale;
                    theScale.x = m_wallFacing;
                    effectObject.transform.localScale = theScale;
                    effectObject.transform.parent = envController.effectHolder;
                    Destroy(effectObject, 1f);

                    m_velocity.y = jumpSpeed;
                    m_velocity.x = wallJumpSpeed * m_wallFacing;
                    m_jumping = true;
                    activatedJump = true;
                }
            }
            else
            {
                // Aerial
                m_sprintCooldown = 0;
                m_sprinting = false;
                if (!m_activatedJumpLast && (m_groundedLast || m_walledLast))
                {
                    // Just came off of a wall or ground without jumping
                    m_framesUntilFastFall = 0;
                    m_framesUntilJump = 0;
                }
                if (!m_jumpKeyLast && jumpKey && m_jumpsLeft > 0 && m_framesUntilJump == 0)
                {
                    m_framesUntilFastFall = m_framesUntilFastFallReset;
                    m_framesUntilJump = m_framesUntilJumpReset;
                    // Aerial Jump
                    GameObject effectPrefab = airJumpPrefab;
                    if (m_jumpsLeft == 1)
                    {
                        effectPrefab = airJumpBurntPrefab;
                    }
                    GameObject effectObject = Instantiate(effectPrefab, transform.position, Quaternion.identity);
                    effectObject.transform.parent = envController.effectHolder;
                    Destroy(effectObject, 1f);
                    m_jumpsLeft--;
                    m_velocity.y = jumpSpeed;
                    m_jumping = true;
                    activatedJump = true;
                }
            }

            // Wall Sliding
            if (!m_walledLast && m_walled)
            {
                m_velocity.y = 0f;
            }
            if (m_walled && m_wallFacing * inputAxes.x < 0)
            {
                m_velocity.y = Mathf.Max(m_velocity.y, -wallSlideMaxSpeed);
            }

            // X Movement
            // If not dashing
            //if (m_dashFrames == 0)
            //{
            if (activatedSprint)
            {
                m_velocity.x = inputAxes.x * sprintSpeed;
            }
            else
            {
                if (m_grounded)
                {
                    if (m_sprinting)
                    {
                        m_velocity.x = Mathf.SmoothDamp(m_velocity.x, inputAxes.x * sprintSpeed, ref m_smoothXVel, smoothTimeX);
                    }
                    else
                    {
                        m_velocity.x = Mathf.SmoothDamp(m_velocity.x, inputAxes.x * moveSpeed, ref m_smoothXVel, smoothTimeX);
                    }
                }
                else
                {
                    m_velocity.x = Mathf.SmoothDamp(m_velocity.x, inputAxes.x * moveSpeed, ref m_smoothXVel, airSmoothTimeX);
                    /*if (m_sprinting)
                    {
                        m_velocity.x = Mathf.SmoothDamp(m_velocity.x, inputAxes.x * sprintSpeed, ref m_smoothXVel, airSmoothTimeX);
                    }
                    else
                    {
                        m_velocity.x = Mathf.SmoothDamp(m_velocity.x, inputAxes.x * moveSpeed, ref m_smoothXVel, airSmoothTimeX);
                    }*/
                }
            }
        }

        // Set facing..?
        if (!m_backDashing && !m_doingMove)
        {
            if (m_velocity.x < 0)
            {
                facingDirection = -1;
            }
            else
            {
                facingDirection = 1;
            }
        }

        // Probably disable depending on move
        // Gravity
        if (!m_gravityDisabled){
            float gravityMagnitude = Physics2D.gravity.magnitude;
            if (m_walled && m_wallFacing * inputAxes.x < 0)
            {
                gravityMagnitude *= wallSlideFactor;
            }
            float fallSpeed = Vector2.Dot(m_velocity, Physics2D.gravity.normalized);
            if (fallSpeed < k_maxFallSpeed)
            {
                float newFallSpeed = Mathf.Min(k_maxFallSpeed, fallSpeed + gravityMagnitude * Time.fixedDeltaTime);
                m_velocity += (newFallSpeed - fallSpeed) * Physics2D.gravity.normalized;
            }
        }

        // Set lasts
        m_jumpKeyLast = jumpKey;
        m_groundedLast = m_grounded;
        m_walledLast = m_walled;
        m_aerialLast = m_aerial;
        m_activatedJumpLast = activatedJump;
    }
    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        // Actions, size = 9

        //m_agentInfo.AddReward(-m_Existential);
        //AddReward(-m_Existential);
        if (Mathf.Abs(transform.position.x) > 5f) {
            m_agentInfo.AddReward(-m_Existential);
        }

        MoveAgent(actionBuffers.DiscreteActions);

        // Graphics
        if (m_brawlSettings.useEffects){
            m_damageText.text = damage.ToString();
            if (stunFrames > 0)
            {
                m_legendSprite.color = stunColor;
                //m_capsuleSprite.color = stunColor;
            }
            else if (m_sprinting)
            {
                m_legendSprite.color = sprintingColor;
                //m_capsuleSprite.color = sprintingColor;
            }
            else 
            {
                m_legendSprite.color = defaultColor;
                //m_capsuleSprite.color = defaultColor;
            }

            // Facing
            m_legendSprite.flipX = facingDirection == -1;

            envController.DisplayAction(team, teamPosition, actionBuffers.DiscreteActions);
        }
        //bool timeout = envController.StepEnv();

        // Everything below this comment in this override should be removed!!! TODO

        // Rewards
        bool topKO = transform.localPosition.y > envController.stage.KOBounds.center.y + envController.stage.KOBounds.size.y / 2;
        bool bottomKO = transform.localPosition.y <  envController.stage.KOBounds.center.y - envController.stage.KOBounds.size.y / 2;
        bool leftKO = transform.localPosition.x < envController.stage.KOBounds.center.x - envController.stage.KOBounds.size.x / 2;
        bool rightKO = transform.localPosition.x > envController.stage.KOBounds.center.x + envController.stage.KOBounds.size.x / 2;
        
        if (isAlive){
            if (bottomKO || leftKO || rightKO)
            {
                // Fell off of bottom
                KO();
            }
            else if (topKO && stunFrames != 0)
            {
                KO();
            }
        }
    }

    public void KO(){
        isAlive = false;
        m_timeUntilRespawn = m_brawlSettings.respawnDelayReset;
        envController.KO(this, groundedSinceLastBeenHit);
        damage = 0;

        // KO Visual

        // Respawn Visual
        GameObject effectObject = Instantiate(respawnPrefab, spawnPos, Quaternion.identity);
        effectObject.transform.parent = envController.effectHolder;
        Destroy(effectObject, 3f);
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var discreteActionsOut = actionsOut.DiscreteActions;
        if (playerType == PlayerType.PLAYER1){
            if (Input.GetKey(KeyCode.W))
            {
                discreteActionsOut[(int)ActionKey.AIMUP] = 1; // Aim up
            }
            if (Input.GetKey(KeyCode.S))
            {
                discreteActionsOut[(int)ActionKey.AIMDOWN] = 1; // Aim down
            }
            if (Input.GetKey(KeyCode.A))
            {
                discreteActionsOut[(int)ActionKey.LEFT] = 1; // Move left
            }
            if (Input.GetKey(KeyCode.D))
            {
                discreteActionsOut[(int)ActionKey.RIGHT] = 1; // Move right
            }
            if (Input.GetKey(KeyCode.Space))
            {
                discreteActionsOut[(int)ActionKey.JUMP] = 1; // Jump
            }
            if (Input.GetKey(KeyCode.H))
            {
                discreteActionsOut[(int)ActionKey.PICKUP] = 1; // Pickup
            }
            if (Input.GetKey(KeyCode.J))
            {
                discreteActionsOut[(int)ActionKey.LIGHT] = 1; // Light attack
            }
            if (Input.GetKey(KeyCode.K))
            {
                discreteActionsOut[(int)ActionKey.HEAVY] = 1; // Heavy attack
            }
            if (Input.GetKey(KeyCode.L))
            {
                discreteActionsOut[(int)ActionKey.DASH] = 1; // Dash
            }
        } else if (playerType == PlayerType.PLAYER2){
            // idk controller support?
        } else if (playerType == PlayerType.STAND){
            // do nothing hurrah
        } else if (playerType == PlayerType.BOT){
            if (transform.position.x > 4f){
                discreteActionsOut[(int)ActionKey.LEFT] = 1;
            } else if (transform.position.x < -4f){
                discreteActionsOut[(int)ActionKey.RIGHT] = 1;
            } else {
                if (opponent.transform.position.x - transform.position.x > 0)
                {
                    discreteActionsOut[(int)ActionKey.RIGHT] = 1;
                }
                else if (opponent.transform.position.x - transform.position.x < 0)
                {
                    discreteActionsOut[(int)ActionKey.LEFT] = 1;
                }
            }
            
            if (transform.position.y < -1 || opponent.transform.position.y - transform.position.y > 0)
            {
                discreteActionsOut[(int)ActionKey.JUMP] = 1;
            }
            if (Vector3.Distance(opponent.transform.position, transform.position) < 2 && (funValue % 2 == 0 ? m_grounded : m_aerial))
            {
                discreteActionsOut[(int)ActionKey.LIGHT] = 1;
            }
        } else if (playerType == PlayerType.BOT_JUMP_TO_STAGE){
            if (transform.position.x > 4f)
            {
                discreteActionsOut[(int)ActionKey.LEFT] = 1;
            }
            else if (transform.position.x < -4f)
            {
                discreteActionsOut[(int)ActionKey.RIGHT] = 1;
            }
            else
            {
                if (opponent.transform.position.x - transform.position.x > 0)
                {
                    discreteActionsOut[(int)ActionKey.RIGHT] = 1;
                }
                else if (opponent.transform.position.x - transform.position.x < 0)
                {
                    discreteActionsOut[(int)ActionKey.LEFT] = 1;
                }
            }

            if (transform.position.y < -1)
            {
                discreteActionsOut[(int)ActionKey.JUMP] = 1;
            }
        }
    }

    void OnDrawGizmos()
    {
        // Team circle
        /*Gizmos.color = m_teamColors[team];
        Gizmos.DrawWireSphere(transform.position + Vector3.up * 0.5f, 0.5f);*/

        // Sprinting box
        if (m_sprinting){
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(transform.position, new Vector3(2, 2, 2));
        }

        // Facing line
        Gizmos.color = Color.red;
        Vector3 lineCenter = transform.position + Vector3.right * facingDirection;
        Gizmos.DrawLine(lineCenter + Vector3.up, lineCenter + Vector3.down);

        // Hurtbox
        CapsuleCollider2D capsule = hurtboxCollider;
        BrawlHitboxUtility.DrawCapsule2D((Vector2)transform.position + capsule.offset * 0.5f, capsule.size * 0.5f, 0f, Color.yellow);

        // Hitboxes
        if (m_hitboxesToDraw != null){
            Gizmos.color = Color.blue;
            foreach (Hitbox hitbox in m_hitboxesToDraw)
            {
                Vector3 hitboxOffset = BrawlHitboxUtility.GetHitboxOffset(hitbox.xOffset, hitbox.yOffset);
                hitboxOffset.x *= m_moveFacingDirection;
                Vector3 hitboxPos = transform.position + hitboxOffset;
                //Debug.Log(hitboxPos);
                BrawlHitboxUtility.DrawHitbox(hitbox, hitboxPos, 0f);
            }
        }
    }
}
