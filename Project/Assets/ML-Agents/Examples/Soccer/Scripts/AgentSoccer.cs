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
    [Tooltip("Cuánta stamina se pierde por segundo cuando se mueve")]
    public float staminaDrainRate = 5f;
    [Tooltip("Cuánta stamina se recupera por segundo cuando está quieto")]
    public float staminaRecoveryRate = 2f;
    
    [Header("Stamina Performance Zones")]
    [Tooltip("Por debajo de este valor (%), la velocidad comienza a reducirse")]
    [Range(0f, 100f)]
    public float fatigueThreshold = 40f;     // Umbral a partir del cual comienza la penalización (%)
    [Tooltip("Por debajo de este valor (%), la velocidad se reduce drásticamente")]
    [Range(0f, 50f)]
    public float exhaustionThreshold = 15f;  // Umbral de agotamiento severo (%)
    [Tooltip("Factor mínimo de velocidad cuando la stamina llega a cero")]
    [Range(0f, 0.5f)]
    public float minSpeedFactor = 0.3f;      // Velocidad mínima (30% de la normal) cuando stamina = 0
    
    // Variables internas
    private float staminaSpeedFactor = 1f;
    private float fatiguedThresholdValue;    // Valor absoluto calculado desde el porcentaje
    private float exhaustedThresholdValue;   // Valor absoluto calculado desde el porcentaje
    
    // Debug variables
    [Header("Debug Settings")]
    public bool debugStamina = false;       // Activar/desactivar depuración
    public float debugInterval = 1.0f;      // Intervalo en segundos para mostrar debug
    private float debugTimer = 0f;          // Contador para controlar el intervalo
    private Vector3 lastPosition;           // Posición anterior para calcular velocidad real
    private float currentSpeed = 0f;        // Velocidad actual del agente
    private Vector3 dirToGoDebug = Vector3.zero; // Guardamos dirección para debug

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

        // Calcular valores absolutos para los umbrales de rendimiento
        fatiguedThresholdValue = maxStamina * (fatigueThreshold / 100f);
        exhaustedThresholdValue = maxStamina * (exhaustionThreshold / 100f);
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
        // Detectamos si se está moviendo
        bool isMoving = dirToGo.magnitude > 0.1f;
        // Guardar dirección para debug
        dirToGoDebug = dirToGo;
        
        if (isMoving)
        {
            // Consumo basado en intensidad del movimiento
            float movementIntensity = Mathf.Clamp01(dirToGo.magnitude);
            currentStamina -= staminaDrainRate * movementIntensity * Time.deltaTime;
        }
        else
        {
            currentStamina += staminaRecoveryRate * Time.deltaTime;
        }
        
        // Clamp entre 0 y max
        currentStamina = Mathf.Clamp(currentStamina, 0f, maxStamina);
        
        // Sistema de zonas de rendimiento
        if (currentStamina < exhaustedThresholdValue)
        {
            // Zona de agotamiento (por debajo del umbral de agotamiento)
            // Velocidad reducida a minSpeedFactor cuando stamina = 0,
            // y aumenta linealmente hasta llegar al umbral de agotamiento
            float normalizedStamina = currentStamina / exhaustedThresholdValue;
            staminaSpeedFactor = Mathf.Lerp(minSpeedFactor, 0.7f, normalizedStamina);
        }
        else if (currentStamina < fatiguedThresholdValue)
        {
            // Zona de fatiga (entre el umbral de agotamiento y el de fatiga)
            // Velocidad reducida entre 0.7 y 1.0 proporcionalmente 
            float normalizedStamina = (currentStamina - exhaustedThresholdValue) / 
                                      (fatiguedThresholdValue - exhaustedThresholdValue);
            staminaSpeedFactor = Mathf.Lerp(0.7f, 1.0f, normalizedStamina);
        }
        else
        {
            // Zona óptima (por encima del umbral de fatiga)
            staminaSpeedFactor = 1f;
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
        MoveAgent(actionBuffers.DiscreteActions);
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
            AddReward(.2f * m_BallTouch);
            var dir = c.contacts[0].point - transform.position;
            dir = dir.normalized;
            c.gameObject.GetComponent<Rigidbody>().AddForce(dir * force);
        }
    }

    public override void OnEpisodeBegin()
    {
        m_BallTouch = m_ResetParams.GetWithDefault("ball_touch", 0);
        currentStamina = maxStamina;
        lastPosition = transform.position;
        debugTimer = 0f;
    }

    private void Update()
    {
        // Calcular velocidad actual basada en la magnitud de la velocidad real del Rigidbody
        currentSpeed = agentRb.linearVelocity.magnitude;
        
        // Debug de stamina si está activado
        if (debugStamina)
        {
            debugTimer += Time.deltaTime;
            if (debugTimer >= debugInterval)
            {
                string teamStr = (team == Team.Blue) ? "Azul" : "Púrpura";
                string posStr = position.ToString();
                
                // Añadir más información para debug
                Vector3 velocity = agentRb.linearVelocity;
                bool isMoving = dirToGoDebug.magnitude > 0.1f; // Referencia a la variable usada en MoveAgent
                
                Debug.Log($"[{teamStr}][{posStr}] Stamina: {currentStamina:F1}/{maxStamina} " +
                          $"Factor: {staminaSpeedFactor:F2} " +
                          $"Velocidad: {currentSpeed:F2} u/s " +
                          $"isMoving: {isMoving} " +
                          $"Zonas: F={fatiguedThresholdValue:F1}/E={exhaustedThresholdValue:F1} " +
                          $"Vel.Raw: ({velocity.x:F2}, {velocity.y:F2}, {velocity.z:F2})");
                
                debugTimer = 0f;
            }
        }
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        float normalizedStamina = maxStamina > 0f ? currentStamina / maxStamina : 0f;
        sensor.AddObservation(normalizedStamina);
    }
}
