using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SharkFSM : MonoBehaviour
{
    public GameManager gameManager;
    public GameObject player;

    // The shark enemy AI is implemented in a finite state machine with states defined in the enum below
    public enum state
    {
        Wandering,
        Chasing,
        Hunting,
        Attacking
    }

    [SerializeField] private state currentState = state.Wandering;
    private state nextState = state.Wandering;

    public float perceptionRadius;
    public float fieldOfView;
    public float maximumTurningTime;
    public float wanderTurningTime;
    public float huntingTurningTime;
    public float attackTurningTime;
    private float turningTime;

    public float avoidanceSteeringWeight;
    public float stateBehaviourSteeringWeight;

    public Transform[] wanderingWaypoints;
    public float wanderingRadius;
    public float waypointSensitivity;
    [SerializeField] private int waypointIndex = 0;
    [SerializeField] private Vector3 currentWaypoint;


    public float wanderMovementPenalty;
    public float attackMovementPenalty;
    public float attackRecoveryTime;
    public float attackDamage;
    public float attackRange;
    public float attackCooldown;
    private float attackCooldownTimer = 0f;

    public float maximumHuntingTimer;
    private float huntingTimer;
    public float huntingRadius;

    [SerializeField] private Vector3 position, velocity, acceleration;
    private float currentSpeed, speed;
    public float maximumSpeed;

    public float maximumCheckObstacleTimer;
    [SerializeField] private float checkObstacleTimer;
    public LayerMask obstacleMask;
    public Vector3[] avoidanceRayDirections;
    private Vector3 avoidanceVector = Vector3.zero;
    private Vector3 previousAvoidanceVector = Vector3.zero;

    void Start()
    {
        currentSpeed = maximumSpeed;
        position = transform.position;
        velocity = transform.forward * currentSpeed;
        acceleration = Vector3.zero;

        waypointIndex = 1;
        currentWaypoint = wanderingWaypoints[waypointIndex].position + (Random.insideUnitSphere * wanderingRadius);

        huntingTimer = maximumHuntingTimer;
    }

    void Update()
    {
        Vector3 desiredVelocity = Vector3.zero;

        // Obstacle avoidance
        // Do not check for obstacles every frame, check on a short timer to increase performance
        if (checkObstacleTimer <= 0)
        {
            checkObstacleTimer = maximumCheckObstacleTimer;
            // Update the obstacle avoidance desired velocity
            avoidanceVector = CheckObstacles() * avoidanceSteeringWeight;
        }
        checkObstacleTimer -= Time.deltaTime;

        // Combine steering contributions from the finite state machine and the obstacle avoidance which is handled independently of the state logic
        desiredVelocity += avoidanceVector;
        // Update the states to get the desired velocity of the behaviour implemented in the current state
        desiredVelocity += UpdateStates() * stateBehaviourSteeringWeight;
        // move the agent, steering towards the desiredVelocity
        MoveAgent(desiredVelocity);
    }

    // A function to handle the state execution and transitions
    private Vector3 UpdateStates()
    {
        Vector3 desiredVelocity = Vector3.zero;
        Vector3 lastKnownPosition = Vector3.zero;

        // Execute different logic depending on the current state
        switch (currentState)
        {
            // If in the wandering state, execute the behaviour in the Wander() function, then check for state transitions
            case state.Wandering:
                // A waypoint-based pathfinding behaviour
                desiredVelocity = Wander();
                // If a player is detected, transition to the chasing state
                if (SensePlayer(perceptionRadius)) { nextState = state.Chasing; }
                break;
            // If currently chasing, execute the appropriate behaviour. Transition to the hunting state if the player can no longer be detected, or the attacking state if the player is close enough
            case state.Chasing:
                // Seeking behaviour, move towards target (player)
                desiredVelocity = Chase();
                if (!SensePlayer(perceptionRadius)) { lastKnownPosition = player.transform.position;  nextState = state.Hunting; }
                if (SensePlayer(attackRange)) { nextState = state.Attacking; }
                break;
            // If currently hunting. Transition to chasing if the target is relocated, or wandering if sufficinet time has passed without a target
            case state.Hunting:
                // Search vicinity of the target's last known position
                desiredVelocity = Hunt(lastKnownPosition);
                if (SensePlayer(perceptionRadius)) { huntingTimer = maximumHuntingTimer; nextState = state.Chasing; }
                if (huntingTimer <= 0f) { huntingTimer = maximumHuntingTimer; nextState = state.Wandering; }
                else { huntingTimer -= Time.deltaTime; }
                break;
            // If currently attacking, perform the attack, then immediately return to the chasing state
            case state.Attacking:
                // perform an attack against the target
                Attack();
                nextState = state.Chasing;
                break;
            default:
                Debug.Log("Error in state transition. State not recognised");
                nextState = state.Wandering;
                break;
        }
        attackCooldownTimer += Time.deltaTime;

        // Set the new currentState to the nextState determined by the FSM
        currentState = nextState;
        // Return the desired velocity of the FSM behaviour executed
        return desiredVelocity;
    }


    // An FSM behaviour: Loop through an array of waypoints. Travelling between them
    private Vector3 Wander()
    {
        // Set the movement and turning speed of the agent while wandering
        turningTime = wanderTurningTime;
        ChangeSpeed(maximumSpeed * wanderMovementPenalty);

        Vector3 desiredVelocity = Vector3.zero;

        // If close to the current waypoint, switch to the next waypoint in the array
        if (Vector3.SqrMagnitude(transform.position - currentWaypoint) < waypointSensitivity * waypointSensitivity)
        {
            // Wrap around the waypoint array
            waypointIndex++;
            if (waypointIndex >= wanderingWaypoints.Length)
                waypointIndex = 0;

            // Set the current waypoint position to a random point in the vicinity of the desired waypoint
            currentWaypoint = wanderingWaypoints[waypointIndex].position + (Random.insideUnitSphere * wanderingRadius);
        }

        // Steer towards waypoint
        desiredVelocity = currentWaypoint - transform.position;
        return desiredVelocity.normalized;
    }

    // An FSM behaviour: move towards target
    private Vector3 Chase()
    {
        // Maximum movement and turning speed while chasing
        turningTime = maximumTurningTime;
        ChangeSpeed(maximumSpeed);

        Vector3 desiredVelocity = Vector3.zero;

        // Basic seeking behaviour. Return normalised direction vector to target
        desiredVelocity = player.transform.position - position;
        return desiredVelocity.normalized;
    }

    // An FSM behaviour. Search the last known position of the target
    private Vector3 Hunt(Vector3 lastKnownPosition)
    {
        turningTime = huntingTurningTime;
        ChangeSpeed(maximumSpeed * wanderMovementPenalty);
        
        Vector3 desiredVelocity = Vector3.zero;
        
        // Get the cross product of the direction vector to the last known position and the up vector to get a heading tangential to the position
        desiredVelocity = Vector3.Cross(lastKnownPosition - position, transform.up);
        //Steer slightly away from centre point
        desiredVelocity += new Vector3((position.x - lastKnownPosition.x), 0f, (position.z - lastKnownPosition.z)) * 0.1f;

        return desiredVelocity.normalized;
    }

    // A 1-time FSM behaviour: Attack the target
    private void Attack()
    {
        // Apply a speed penalty to give the playera  chance to escape after the attack has landed
        turningTime = attackTurningTime;
        ChangeSpeed(maximumSpeed * attackMovementPenalty);

        // Only execute an attack if the cooldown has expired
        if (attackCooldownTimer >= attackCooldown)
        {
            attackCooldownTimer = 0f;
            // Call the game manager function to damage the player
            gameManager.DamagePlayer(attackDamage);
        }
    }

    // A function to steer the agent away from obstacles. This is the same as the funciton from the BoidAgent script
    private Vector3 CheckObstacles()
    {
        Vector3 desiredVelocity = Vector3.zero;

        // Raycast to check for a collision in the forwards direction
        RaycastHit hit;
        if (Physics.Raycast(position, transform.forward, out hit, perceptionRadius, obstacleMask))
        {
            Debug.Log(hit.collider.name);
            // Don't try to avoid the player
            if (hit.collider.tag != "Player")
                desiredVelocity = AvoidObstacle();
        }
        previousAvoidanceVector = desiredVelocity;

        return desiredVelocity;
    }

    private Vector3 AvoidObstacle()
    {
        Vector3 selectedDirection = Vector3.zero;

        // If a non-zero avoidance vector has already been calculated check whether this one is still good before recalculating
        if (previousAvoidanceVector != Vector3.zero)
        {
            RaycastHit hit;
            if (!Physics.Raycast(position, transform.forward, out hit, perceptionRadius, obstacleMask))
            {
                selectedDirection = previousAvoidanceVector;
                return selectedDirection;
            }
        }

        float furthestDistance = int.MinValue;
        // Go through each direction in avoidanceRayDirections[] to find the ray that travels the furthest without collision
        for (int i = 0; i < avoidanceRayDirections.Length; i++)
        {
            RaycastHit hit;
            Vector3 currentDirection = transform.TransformDirection(avoidanceRayDirections[i].normalized);
            if (Physics.Raycast(position, currentDirection, out hit, perceptionRadius, obstacleMask))
            {
                // If the hit is further away than the previous furthest hit, select this direction
                float currentDistance = (hit.point - position).sqrMagnitude;
                if (currentDistance > furthestDistance)
                {
                    furthestDistance = currentDistance;
                    selectedDirection = currentDirection;
                }
            }
            else
            {
                // If the raycast hits nothing, select this direction and do not check any others
                selectedDirection = currentDirection;
                return selectedDirection.normalized;
            }
        }
        return selectedDirection.normalized;
    }

    // A function to smoothly interpolate between speeds
    private void ChangeSpeed(float newSpeed)
    {
        currentSpeed = Mathf.Lerp(currentSpeed, newSpeed, attackRecoveryTime);
    }

    // A function to move the agent according to the steering behaviours above
    private void MoveAgent(Vector3 desiredVelocity)
    {
        // Use the SmoothDamp() function to steer in the direction of desiredVelocity
        velocity = Vector3.SmoothDamp(transform.forward, desiredVelocity, ref acceleration, turningTime);
        // Set the magnitude of the velocity to the current speed
        velocity = velocity.normalized * currentSpeed;
        // If there is no velocity, set it to the forwards direction
        if (velocity == Vector3.zero) { velocity = transform.forward; }
        // Set the forwards vector to the direction of velocity
        transform.forward = velocity;
        // Update the position according to the current velocity
        position += velocity * Time.deltaTime;
        // Update the agent transform position
        transform.position = position;
    }

    // A funciton to determine whether the player is visible to the agent
    private bool SensePlayer(float radius)
    {
        // Check whether the player is within the FOV
        if (IsInFOV(player.transform.position))
        {
            // Check whether the player is close enough to be detected
            if (Vector3.SqrMagnitude(player.transform.position - transform.position) < radius * radius)
            {
                return true;
            }
        }
        return false;
    }

    // A function to determine whether a target is within som efield of view. The smae function as is used by the BoidAgent script
    private bool IsInFOV(Vector3 targetPosition)
    {
        float angle = Vector3.Angle(transform.forward, (targetPosition - transform.position));
        return (angle <= fieldOfView / 2);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawRay(transform.position, transform.forward * perceptionRadius);
        Gizmos.color = Color.green;
        Gizmos.DrawRay(transform.position, transform.forward * attackRange);
    }
}
