using UnityEngine;

public class SoccerBallController : MonoBehaviour
{
    public GameObject area;
    [HideInInspector]
    public SoccerEnvController envController;
    public string purpleGoalTag; //will be used to check if collided with purple goal
    public string blueGoalTag; //will be used to check if collided with blue goal

    void Start()
    {
        if (area == null)
        {
            Debug.LogError("SoccerBallController: area GameObject is not assigned in the inspector.");
            return;
        }

        envController = area.GetComponent<SoccerEnvController>();
        if (envController == null)
        {
            Debug.LogError("SoccerBallController: area GameObject does not have a SoccerEnvController component.");
        }
    }

    void OnCollisionEnter(Collision col)
    {
        // Ensure envController is properly initialized
        if (envController == null)
        {
            if (area != null)
            {
                envController = area.GetComponent<SoccerEnvController>();
            }
            
            if (envController == null)
            {
                Debug.LogWarning("SoccerBallController: envController is null. Make sure the area GameObject has a SoccerEnvController component.");
                return;
            }
        }

        if (col.gameObject.CompareTag(purpleGoalTag)) //ball touched purple goal
        {
            envController.GoalTouched(Team.Blue);
        }
        if (col.gameObject.CompareTag(blueGoalTag)) //ball touched blue goal
        {
            envController.GoalTouched(Team.Purple);
        }
    }
}
