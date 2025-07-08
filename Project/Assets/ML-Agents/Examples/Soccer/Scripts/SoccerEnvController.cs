using System.Collections.Generic;
using Unity.MLAgents;
using UnityEngine;

public class SoccerEnvController : MonoBehaviour
{
    [System.Serializable]
    public class PlayerInfo
    {
        public AgentSoccer Agent;
        [HideInInspector]
        public Vector3 StartingPos;
        [HideInInspector]
        public Quaternion StartingRot;
        [HideInInspector]
        public Rigidbody Rb;
    }


    /// <summary>
    /// Max Academy steps before this platform resets
    /// </summary>
    [Tooltip("Max Environment Steps")] public int MaxEnvironmentSteps = 25000;

    /// <summary>
    /// The area bounds.
    /// </summary>

    /// <summary>
    /// We will be changing the ground material based on success/failue
    /// </summary>

    public GameObject ball;
    [HideInInspector]
    public Rigidbody ballRb;
    Vector3 m_BallStartingPos;

    //List of Agents On Platform
    public List<PlayerInfo> AgentsList = new List<PlayerInfo>();

    private SoccerSettings m_SoccerSettings;

    // Add references to goals for reward calculation
    public GameObject blueGoal;
    public GameObject purpleGoal;

    private SimpleMultiAgentGroup m_BlueAgentGroup;
    private SimpleMultiAgentGroup m_PurpleAgentGroup;

    private int m_ResetTimer;
    private const float AUTOGOAL_PENALTY_GROUP = -1.0f; // Group penalty for autogoal

    void Start()
    {

        m_SoccerSettings = FindFirstObjectByType<SoccerSettings>();
        // Initialize TeamManager
        m_BlueAgentGroup = new SimpleMultiAgentGroup();
        m_PurpleAgentGroup = new SimpleMultiAgentGroup();
        ballRb = ball.GetComponent<Rigidbody>();
        m_BallStartingPos = new Vector3(ball.transform.position.x, ball.transform.position.y, ball.transform.position.z);
        foreach (var item in AgentsList)
        {
            if (item == null || item.Agent == null)
            {
                Debug.LogError("SoccerEnvController: Found null item or agent in AgentsList during initialization.");
                continue;
            }

            item.StartingPos = item.Agent.transform.position;
            item.StartingRot = item.Agent.transform.rotation;
            item.Rb = item.Agent.GetComponent<Rigidbody>();
            
            if (item.Rb == null)
            {
                Debug.LogError($"SoccerEnvController: Agent {item.Agent.name} is missing a Rigidbody component.");
                continue;
            }

            if (item.Agent.team == Team.Blue)
            {
                m_BlueAgentGroup.RegisterAgent(item.Agent);
            }
            else
            {
                m_PurpleAgentGroup.RegisterAgent(item.Agent);
            }
        }
        ResetScene();
    }

    void FixedUpdate()
    {
        m_ResetTimer += 1;
        if (m_ResetTimer >= MaxEnvironmentSteps && MaxEnvironmentSteps > 0)
        {
            m_BlueAgentGroup.GroupEpisodeInterrupted();
            m_PurpleAgentGroup.GroupEpisodeInterrupted();
            ResetScene();
        }
    }


    public void ResetBall()
    {
        var randomPosX = Random.Range(-2.5f, 2.5f);
        var randomPosZ = Random.Range(-2.5f, 2.5f);

        ball.transform.position = m_BallStartingPos + new Vector3(randomPosX, 0f, randomPosZ);
        ballRb.linearVelocity = Vector3.zero;
        ballRb.angularVelocity = Vector3.zero;

    }

    public void GoalTouched(Team scoredTeam)
    {
        AgentSoccer lastAgentToTouch = AgentSoccer.lastTouchedBallAgent;

        if (scoredTeam == Team.Blue) // Blue scored on Purple goal
        {
            // Check for autogoal by Purple team
            if (lastAgentToTouch != null && lastAgentToTouch.team == Team.Purple)
            {
                m_PurpleAgentGroup.AddGroupReward(AUTOGOAL_PENALTY_GROUP); // Penalize Purple team for autogoal
                lastAgentToTouch.AddReward(AgentSoccer.AUTOGOAL_PENALTY_AGENT); // Penalize the specific agent
                Debug.Log("Autogoal by Purple Player: " + lastAgentToTouch.name);
                // Blue still gets standard goal reward
                m_BlueAgentGroup.AddGroupReward(1 - (float)m_ResetTimer / MaxEnvironmentSteps);
            }
            else
            {
                m_BlueAgentGroup.AddGroupReward(1 - (float)m_ResetTimer / MaxEnvironmentSteps);
                m_PurpleAgentGroup.AddGroupReward(-1); // Standard penalty for conceded goal
            }
        }
        else // Purple scored on Blue goal
        {
            // Check for autogoal by Blue team
            if (lastAgentToTouch != null && lastAgentToTouch.team == Team.Blue)
            {
                m_BlueAgentGroup.AddGroupReward(AUTOGOAL_PENALTY_GROUP); // Penalize Blue team for autogoal
                lastAgentToTouch.AddReward(AgentSoccer.AUTOGOAL_PENALTY_AGENT); // Penalize the specific agent
                Debug.Log("Autogoal by Blue Player: " + lastAgentToTouch.name);
                // Purple still gets standard goal reward
                m_PurpleAgentGroup.AddGroupReward(1 - (float)m_ResetTimer / MaxEnvironmentSteps);
            }
            else
            {
                m_PurpleAgentGroup.AddGroupReward(1 - (float)m_ResetTimer / MaxEnvironmentSteps);
                m_BlueAgentGroup.AddGroupReward(-1); // Standard penalty for conceded goal
            }
        }
        m_PurpleAgentGroup.EndGroupEpisode();
        m_BlueAgentGroup.EndGroupEpisode();
        ResetScene();

    }


    public void ResetScene()
    {
        m_ResetTimer = 0;

        //Reset Agents
        foreach (var item in AgentsList)
        {
            // Check for null references
            if (item == null || item.Agent == null || item.Rb == null)
            {
                Debug.LogWarning("SoccerEnvController: Found null agent or rigidbody in AgentsList during ResetScene.");
                continue;
            }

            var randomPosX = Random.Range(-5f, 5f);
            var newStartPos = item.Agent.initialPos + new Vector3(randomPosX, 0f, 0f);
            var rot = item.Agent.rotSign * Random.Range(80.0f, 100.0f);
            var newRot = Quaternion.Euler(0, rot, 0);
            item.Agent.transform.SetPositionAndRotation(newStartPos, newRot);

            item.Rb.linearVelocity = Vector3.zero;
            item.Rb.angularVelocity = Vector3.zero;
        }

        //Reset Ball
        ResetBall();
    }
}
