using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Policies;
using Unity.MLAgents.Sensors;

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


    [HideInInspector]
    public Rigidbody agentRb;
    SoccerSettings m_SoccerSettings;
    BehaviorParameters m_BehaviorParameters;
    public Vector3 initialPos;
    public float rotSign;

    EnvironmentParameters m_ResetParams;

    // Stamina variables
    [Header("Stamina Settings")]
    public float maxStamina = 100f;
    public float currentStamina;
    public float staminaDrainRate = 10f;      // Por segundo, cuando se mueve rápido
    public float staminaRecoveryRate = 2f;   // Por segundo, cuando se mueve poco
    public float staminaThreshold = 20f;     // Si baja de esto, pierde velocidad
    public float minSpeedFactorWhenExhausted = 0.5f; // Velocidad mínima cuando la stamina es 0 
    public float staminaSpeedFactor = 1f;


    public override void Initialize()
    {
        SoccerEnvController envController = GetComponentInParent<SoccerEnvController>();
        if (envController != null)
        {
            m_Existential = 1f / envController.MaxEnvironmentSteps;
        }
        else
        {
            m_Existential = 1f / MaxStep;
        }

        m_BehaviorParameters = gameObject.GetComponent<BehaviorParameters>();
        if (m_BehaviorParameters.TeamId == (int)Team.Blue)
        {
            team = Team.Blue;
            initialPos = new Vector3(transform.position.x - 5f, .5f, transform.position.z);
            rotSign = 1f;
        }
        else
        {
            team = Team.Purple;
            initialPos = new Vector3(transform.position.x + 5f, .5f, transform.position.z);
            rotSign = -1f;
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
        m_SoccerSettings = FindObjectOfType<SoccerSettings>();
        agentRb = GetComponent<Rigidbody>();
        agentRb.maxAngularVelocity = 500;

        m_ResetParams = Academy.Instance.EnvironmentParameters;
        currentStamina = maxStamina;
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

        // === STAMINA LOGIC ===
        bool isTryingToMove = dirToGo.magnitude > 0.1f;

        // Primero, calculamos el staminaSpeedFactor potencial BASADO EN LA STAMINA ACTUAL (antes de gastar/recuperar).
        // Esto nos dice a qué velocidad el agente INTENTARÍA moverse si la stamina no cambiara este frame.
        float potentialSpeedFactor;
        if (currentStamina < staminaThreshold)
        {
            float staminaRatio = (staminaThreshold > 0) ? Mathf.Clamp01(currentStamina / staminaThreshold) : 0f;
            potentialSpeedFactor = minSpeedFactorWhenExhausted + staminaRatio * (1f - minSpeedFactorWhenExhausted);
        }
        else
        {
            potentialSpeedFactor = 1f;
        }
        if (currentStamina <= 0f) // Asegurar que el factor potencial no sea menor que el mínimo
        {
            potentialSpeedFactor = minSpeedFactorWhenExhausted;
        }


        // Ahora, decidimos si gastar o recuperar stamina.
        if (isTryingToMove)
        {
            // Si el agente intenta moverse Y su velocidad potencial es MAYOR que la mínima, gasta stamina.
            if (potentialSpeedFactor > minSpeedFactorWhenExhausted)
            {
                currentStamina -= staminaDrainRate * Time.deltaTime;
            }
            // Si intenta moverse pero su velocidad potencial ES la mínima (o menos, aunque no debería ser menos), recupera.
            else // potentialSpeedFactor <= minSpeedFactorWhenExhausted
            {
                currentStamina += staminaRecoveryRate * Time.deltaTime;
            }
        }
        else // No está intentando moverse
        {
            currentStamina += staminaRecoveryRate * Time.deltaTime;
        }

        currentStamina = Mathf.Clamp(currentStamina, 0f, maxStamina);
        
        // --- Calcular y actualizar el staminaSpeedFactor FINAL para ESTE frame ---
        // Basado en la currentStamina recién actualizada (después de gastar/recuperar).
        if (currentStamina < staminaThreshold)
        {
            float staminaRatio = (staminaThreshold > 0) ? Mathf.Clamp01(currentStamina / staminaThreshold) : 0f;
            staminaSpeedFactor = minSpeedFactorWhenExhausted + staminaRatio * (1f - minSpeedFactorWhenExhausted);
        }
        else
        {
            staminaSpeedFactor = 1f;
        }
        
        // Asegurarse de que el factor no sea menor que el mínimo si la stamina es 0.
        // Esta es una salvaguarda importante y donde se aplica la velocidad mínima.
        if (currentStamina <= 0f)
        {
            staminaSpeedFactor = minSpeedFactorWhenExhausted;
            // La línea "AddReward(-0.001f);" que estaba aquí debe moverse a OnActionReceived
            // si deseas penalizar el agotamiento. MoveAgent no debería manejar recompensas directamente.
        }

        // Aplicar fuerza con penalización por cansancio
        agentRb.AddForce(dirToGo * m_SoccerSettings.agentRunSpeed * staminaSpeedFactor,
            ForceMode.VelocityChange);
    }

    public override void OnActionReceived(ActionBuffers actionBuffers)

    {

        if (position == Position.Goalie)
        {
            // Existential bonus for Goalies.
            AddReward(m_Existential);
        }
        else if (position == Position.Striker)
        {
            // Existential penalty for Strikers
            AddReward(-m_Existential);
            // probar restar penalizacion
        }
        MoveAgent(actionBuffers.DiscreteActions); // MoveAgent actualiza currentStamina y staminaSpeedFactor

        // --- Penalización por Agotamiento de Stamina ---
        // Se aplica después de que MoveAgent haya actualizado la stamina.
        // Usamos un valor ligeramente superior a 0 para la comparación para capturar casos de flotantes muy pequeños.
        if (currentStamina <= 0.01f) 
        {
            AddReward(-0.001f); // Penalización por estar exhausto. Ajusta este valor según sea necesario.
        }
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
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
            // Use group reward instead of individual reward
            var envController = GetComponentInParent<SoccerEnvController>();
            if (envController != null)
            {
                if (team == Team.Blue)
                {
                    envController.BlueAgentGroup.AddGroupReward(.2f * m_BallTouch);
                }
                else
                {
                    envController.PurpleAgentGroup.AddGroupReward(.2f * m_BallTouch);
                }
            }
            else
            {
                // fallback to individual reward if envController not found
                AddReward(.2f * m_BallTouch);
            }
            var dir = c.contacts[0].point - transform.position;
            dir = dir.normalized;
            c.gameObject.GetComponent<Rigidbody>().AddForce(dir * force);
        }
    }

    public override void OnEpisodeBegin()
    {
        m_BallTouch = m_ResetParams.GetWithDefault("ball_touch", 0);
        currentStamina = maxStamina;
    }

    public override void CollectObservations(VectorSensor sensor)
        {
            float normalizedStamina = maxStamina > 0f ? currentStamina / maxStamina : 0f;
            sensor.AddObservation(normalizedStamina);
        }
}
