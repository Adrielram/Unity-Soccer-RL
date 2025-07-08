using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Policies;

public enum Team
{
    Blue = 0,
    Purple = 1
}

public class AgentSoccer : Agent
{
    // Note that that the detectable tags are different for the blue and purple teams. The order is
    // * ball
    // * own goal
    // * opposing goal
    // * wall
    // * own teammate
    // * opposing player

    public enum Position
    {
        Striker,
        Goalie,
        Generic
    }

    [HideInInspector]
    public Team team;
    float m_KickPower;
    // The coefficient for the reward for colliding with a ball. Set using curriculum.
    float m_BallTouch;
    public Position position;

    const float k_Power = 2000f;
    float m_Existential;
    float m_LateralSpeed;
    float m_ForwardSpeed;

    // New variables for reward shaping
    private Transform m_BallTransform;
    private Transform m_OpponentGoalTransform;
    private Transform m_OwnGoalTransform;
    private Rigidbody m_BallRigidbody;
    private float m_PreviousDistanceToBall = float.MaxValue;
    private const float PROXIMITY_REWARD_SCALE = 0.01f;
    private const float BALL_PROGRESS_REWARD_SCALE = 0.1f;
    private const float GOALIE_SAVE_REWARD = 0.5f;
    public const float AUTOGOAL_PENALTY_AGENT = -0.5f; // Individual penalty for agent scoring autogoal

    public static AgentSoccer lastTouchedBallAgent; // To track who last touched the ball


    [HideInInspector]
    public Rigidbody agentRb;
    SoccerSettings m_SoccerSettings;
    BehaviorParameters m_BehaviorParameters;
    public Vector3 initialPos;
    public float rotSign;

    EnvironmentParameters m_ResetParams;
    SoccerEnvController m_EnvController;

    public override void Initialize()
    {
        m_EnvController = GetComponentInParent<SoccerEnvController>();
        if (m_EnvController != null)
        {
            m_Existential = 1f / m_EnvController.MaxEnvironmentSteps;
            m_BallTransform = m_EnvController.ball.transform; // Get ball transform
            m_BallRigidbody = m_EnvController.ballRb; // Get ball rigidbody
        }
        else
        {
            m_Existential = 1f / MaxStep;
            // Fallback if not in a full environment setup, though less ideal
            GameObject ballObj = GameObject.FindGameObjectWithTag("ball");
            if (ballObj) {
                m_BallTransform = ballObj.transform;
                m_BallRigidbody = ballObj.GetComponent<Rigidbody>();
            }
        }

        m_BehaviorParameters = gameObject.GetComponent<BehaviorParameters>();
        if (m_BehaviorParameters.TeamId == (int)Team.Blue)
        {
            team = Team.Blue;
            initialPos = new Vector3(transform.position.x - 5f, .5f, transform.position.z);
            rotSign = 1f;
            if (m_EnvController) {
                if (m_EnvController.purpleGoal != null)
                {
                    m_OpponentGoalTransform = m_EnvController.purpleGoal.transform;
                }
                else
                {
                    Debug.LogError("Purple Goal not assigned in SoccerEnvController inspector for Blue team.");
                }
                if (m_EnvController.blueGoal != null)
                {
                    m_OwnGoalTransform = m_EnvController.blueGoal.transform;
                }
                else
                {
                    Debug.LogError("Blue Goal not assigned in SoccerEnvController inspector for Blue team.");
                }
            }
        }
        else
        {
            team = Team.Purple;
            initialPos = new Vector3(transform.position.x + 5f, .5f, transform.position.z);
            rotSign = -1f;
            if (m_EnvController) {
                if (m_EnvController.blueGoal != null)
                {
                    m_OpponentGoalTransform = m_EnvController.blueGoal.transform;
                }
                else
                {
                    Debug.LogError("Blue Goal not assigned in SoccerEnvController inspector for Purple team.");
                }
                if (m_EnvController.purpleGoal != null)
                {
                    m_OwnGoalTransform = m_EnvController.purpleGoal.transform;
                }
                else
                {
                    Debug.LogError("Purple Goal not assigned in SoccerEnvController inspector for Purple team.");
                }
            }
        }
        if (position == Position.Goalie)
        {
            m_LateralSpeed = 1.0f;
            m_ForwardSpeed = 1.0f;
        }
        else if (position == Position.Striker)
        {
            m_LateralSpeed = 0.3f;
            m_ForwardSpeed = 1.3f;
        }
        else
        {
            m_LateralSpeed = 0.3f;
            m_ForwardSpeed = 1.0f;
        }
        m_SoccerSettings = FindFirstObjectByType<SoccerSettings>();
        agentRb = GetComponent<Rigidbody>();
        agentRb.maxAngularVelocity = 500;

        m_ResetParams = Academy.Instance.EnvironmentParameters;
    }

    public void MoveAgent(ActionSegment<int> act)
    {
        var dirToGo = Vector3.zero;
        var rotateDir = Vector3.zero;

        m_KickPower = 0f;

        var forwardAxis = act[0];
        var rightAxis = act[1];
        var rotateAxis = act[2];

        switch (forwardAxis)
        {
            case 1:
                dirToGo = transform.forward * m_ForwardSpeed;
                m_KickPower = 1f;
                break;
            case 2:
                dirToGo = transform.forward * -m_ForwardSpeed;
                break;
        }

        switch (rightAxis)
        {
            case 1:
                dirToGo = transform.right * m_LateralSpeed;
                break;
            case 2:
                dirToGo = transform.right * -m_LateralSpeed;
                break;
        }

        switch (rotateAxis)
        {
            case 1:
                rotateDir = transform.up * -1f;
                break;
            case 2:
                rotateDir = transform.up * 1f;
                break;
        }

        transform.Rotate(rotateDir, Time.deltaTime * 100f);
        agentRb.AddForce(dirToGo * m_SoccerSettings.agentRunSpeed,
            ForceMode.VelocityChange);
    }

    public void MoveAgentContinuous(ActionSegment<float> continuousActions)
    {
        var dirToGo = Vector3.zero;
        var rotateDir = Vector3.zero;

        m_KickPower = 0f;

        // Continuous actions: [0] = forward/backward, [1] = left/right, [2] = rotation
        var forwardAxis = continuousActions[0];  // Range: -1 to 1
        var rightAxis = continuousActions[1];    // Range: -1 to 1
        var rotateAxis = continuousActions[2];   // Range: -1 to 1

        // Forward/Backward movement
        dirToGo += transform.forward * forwardAxis * m_ForwardSpeed;
        
        // Left/Right movement
        dirToGo += transform.right * rightAxis * m_LateralSpeed;

        // Rotation
        rotateDir = transform.up * rotateAxis;

        // Kick power is activated when moving forward
        if (forwardAxis > 0.5f)
        {
            m_KickPower = forwardAxis; // Proportional kick power
        }

        transform.Rotate(rotateDir, Time.deltaTime * 100f);
        agentRb.AddForce(dirToGo * m_SoccerSettings.agentRunSpeed,
            ForceMode.VelocityChange);
    }

    public override void OnActionReceived(ActionBuffers actionBuffers)

    {
        // Reward for existing
        if (position == Position.Goalie)
        {
            AddReward(m_Existential);
        }
        else if (position == Position.Striker)
        {
            AddReward(-m_Existential);
        }

        // Handle both discrete and continuous actions based on behavior parameters
        if (m_BehaviorParameters.BrainParameters.ActionSpec.NumContinuousActions > 0)
        {
            // SAC - Continuous actions
            MoveAgentContinuous(actionBuffers.ContinuousActions);
        }
        else
        {
            // POCA/PPO - Discrete actions
            MoveAgent(actionBuffers.DiscreteActions);
        }

        if (m_BallTransform != null)
        {
            float currentDistanceToBall = Vector3.Distance(transform.position, m_BallTransform.position);

            // Reward for approaching the ball
            if (currentDistanceToBall < m_PreviousDistanceToBall)
            {
                AddReward(PROXIMITY_REWARD_SCALE * (m_PreviousDistanceToBall - currentDistanceToBall));
            }
            // Penalty for moving away from the ball if already close (e.g., within 5 units)
            else if (currentDistanceToBall > m_PreviousDistanceToBall && m_PreviousDistanceToBall < 5f)
            {
                AddReward(-PROXIMITY_REWARD_SCALE * (currentDistanceToBall - m_PreviousDistanceToBall));
            }
            m_PreviousDistanceToBall = currentDistanceToBall;
        }
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        // Check if we're using continuous or discrete actions
        if (m_BehaviorParameters.BrainParameters.ActionSpec.NumContinuousActions > 0)
        {
            // Continuous actions for SAC
            var continuousActionsOut = actionsOut.ContinuousActions;
            
            // Forward/Backward
            if (Input.GetKey(KeyCode.W))
            {
                continuousActionsOut[0] = 1.0f;
            }
            else if (Input.GetKey(KeyCode.S))
            {
                continuousActionsOut[0] = -1.0f;
            }
            else
            {
                continuousActionsOut[0] = 0.0f;
            }
            
            // Left/Right
            if (Input.GetKey(KeyCode.E))
            {
                continuousActionsOut[1] = 1.0f;
            }
            else if (Input.GetKey(KeyCode.Q))
            {
                continuousActionsOut[1] = -1.0f;
            }
            else
            {
                continuousActionsOut[1] = 0.0f;
            }
            
            // Rotation
            if (Input.GetKey(KeyCode.D))
            {
                continuousActionsOut[2] = 1.0f;
            }
            else if (Input.GetKey(KeyCode.A))
            {
                continuousActionsOut[2] = -1.0f;
            }
            else
            {
                continuousActionsOut[2] = 0.0f;
            }
        }
        else
        {
            // Discrete actions for POCA/PPO
            var discreteActionsOut = actionsOut.DiscreteActions;
            //forward
            if (Input.GetKey(KeyCode.W))
            {
                discreteActionsOut[0] = 1;
            }
            if (Input.GetKey(KeyCode.S))
            {
                discreteActionsOut[0] = 2;
            }
            //rotate
            if (Input.GetKey(KeyCode.A))
            {
                discreteActionsOut[2] = 1;
            }
            if (Input.GetKey(KeyCode.D))
            {
                discreteActionsOut[2] = 2;
            }
            //right
            if (Input.GetKey(KeyCode.E))
            {
                discreteActionsOut[1] = 1;
            }
            if (Input.GetKey(KeyCode.Q))
            {
                discreteActionsOut[1] = 2;
            }
        }
    }
    /// <summary>
    /// Used to provide a "kick" to the ball.
    /// </summary>
    void OnCollisionEnter(Collision c)
    {
        var force = k_Power * m_KickPower;
        if (position == Position.Goalie)
        {
            force = k_Power;
        }
        if (c.gameObject.CompareTag("ball"))
        {
            lastTouchedBallAgent = this; // Track last agent to touch the ball

            AddReward(.2f * m_BallTouch); // Existing ball touch reward

            Vector3 ballToOpponentGoalDir = Vector3.zero;
            if (m_OpponentGoalTransform != null) {
                ballToOpponentGoalDir = (m_OpponentGoalTransform.position - m_BallTransform.position).normalized;
            }

            // Apply kick force
            var dir = c.contacts[0].point - transform.position;
            dir = dir.normalized;
            c.gameObject.GetComponent<Rigidbody>().AddForce(dir * force);


            // Reward for kicking ball towards opponent's goal
            if (m_OpponentGoalTransform != null && m_BallRigidbody != null)
            {
                // Wait a fixed update to get the velocity after force application
                StartCoroutine(RewardForBallMovement(ballToOpponentGoalDir));
            }

            // Goalie save reward
            if (position == Position.Goalie && m_OwnGoalTransform != null && m_BallRigidbody != null)
            {
                Vector3 ballDir = m_BallRigidbody.linearVelocity.normalized;
                Vector3 ballToOwnGoalDir = (m_OwnGoalTransform.position - m_BallTransform.position).normalized;
                // If ball is moving towards own goal and goalie touches it
                if (Vector3.Dot(ballDir, ballToOwnGoalDir) > 0.5f) // Ball moving towards own goal
                {
                    AddReward(GOALIE_SAVE_REWARD);
                }
            }
        }
    }

    System.Collections.IEnumerator RewardForBallMovement(Vector3 ballToOpponentGoalDir)
    {
        yield return new WaitForFixedUpdate(); // Wait for physics update
        if (m_BallRigidbody != null && m_OpponentGoalTransform != null)
        {
            Vector3 ballMovementDir = m_BallRigidbody.linearVelocity.normalized;
            float progress = Vector3.Dot(ballMovementDir, ballToOpponentGoalDir);
            if (progress > 0)
            {
                AddReward(BALL_PROGRESS_REWARD_SCALE * progress);
            }
        }
    }


    public override void OnEpisodeBegin()
    {
        if (m_ResetParams != null)
        {
            m_BallTouch = m_ResetParams.GetWithDefault("ball_touch", 0);
        }
        else
        {
            m_BallTouch = 0; // Default value if m_ResetParams is null
            Debug.LogWarning("AgentSoccer: m_ResetParams was null in OnEpisodeBegin. Defaulting m_BallTouch to 0. Check Academy and EnvironmentParameters setup.");
        }
        m_PreviousDistanceToBall = float.MaxValue; // Reset previous distance
        if (m_BallTransform != null) // Ensure ball transform is available
        {
             m_PreviousDistanceToBall = Vector3.Distance(transform.position, m_BallTransform.position);
        }
        lastTouchedBallAgent = null;
    }

}
