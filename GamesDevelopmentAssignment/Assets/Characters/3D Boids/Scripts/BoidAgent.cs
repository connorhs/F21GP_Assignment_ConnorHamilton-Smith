using System.Collections.Generic;
using UnityEngine;

public class BoidAgent : MonoBehaviour
{
    private BoidFlock flock;
    private BoidParameters parameters;

    [SerializeField] private List<BoidAgent> neighbours;

    private string agentType;
    public string AgentType() { return agentType; }

    private float speed;
    [SerializeField] private Vector3 position, velocity, acceleration;
    public Vector3 Position() { return position; }

    private Vector3 previousAvoidanceVector = Vector3.zero;

    // Initialse the boid agent to pass in values from about the flock and intialise some variables. Called from BoidFlock after each boid is spawned
    public void Initialise(BoidFlock _flock, BoidParameters _parameters)
    {
        flock = _flock;
        parameters = _parameters;

        neighbours = new List<BoidAgent>();

        agentType = parameters.agentType;

        speed = (parameters.maximumSpeed + parameters.minimumSpeed) / 2;
        position = transform.position;
        velocity = transform.forward * speed;
        acceleration = Vector3.zero;
    }

    // Calculate the steering vector and move the boid agent accordingly. Called once per frame from BoidFlock
    public void MoveAgent(List<BoidAgent> Neighbours)
    {
        // Get the current neighbours from BoidFlock.GetNeighbours()
        neighbours = Neighbours;

        // Try to match the speed of nearby boids
        MatchSpeed();

        // Get all contributions from steering behaviours
        // The seeking behaviour was implemented but not used. Since the boids often won't have a target, the seeking behaviour is automatically set to zero unless the behaviour is activaed by setting the weight equal to a non zero value
        Vector3 seekingVector = (parameters.seekingWeight > 0) ? (GetSeekingVector(parameters.target.position) * parameters.seekingWeight) : (Vector3.zero);
        Vector3 wanderingVector = GetWanderingVector() * parameters.wanderingWeight;
        Vector3 cohesionVector = GetCohesionVector() * parameters.cohesionWeight;
        Vector3 separationVector = GetAlignmentVector() * parameters.alignmentWeight;
        Vector3 alignmentVector = GetSeparationVector() * parameters.separationWeight;
        Vector3 avoidanceVector = GetAvoidanceVector() * parameters.avoidanceWeight;
        Vector3 boundsVector = GetBoundsVector() * parameters.boundsWeight;

        // Sum the steering behaviour contributions
        Vector3 desiredVelocity = seekingVector + wanderingVector + cohesionVector + alignmentVector + separationVector + avoidanceVector + boundsVector;
        // Use the SmoothDamp functions to smoothly steer from the current direction (transform.forward) to the desiredVelocity in some time invsersely proportional to the turningSpeed parameter
        velocity = Vector3.SmoothDamp(transform.forward, desiredVelocity, ref acceleration, 1 / parameters.turningSpeed);
        // Normalise the velocity and multiply by speed to set the magnitude to the matched speed
        velocity = velocity.normalized * speed;
        // If the velocity is zero, set it to transform.forward to keep the boids moving. This was necessary in testing but is now somewhat redundant
        if (velocity == Vector3.zero) { velocity = transform.forward; }
        // Set the new forward direction to the velocity vector
        transform.forward = velocity;
        // Move the agent position by some deltaPosition = velocity * time since last move
        position += velocity * Time.deltaTime;
        // Update the transform.position of the agent
        transform.position = position;
    }

    // The function used to match the speed of the current boid to that of its neighbours
    private void MatchSpeed()
    {
        // If there are no neighbours, do not match speed
        if (neighbours.Count == 0) { return; }

        // Set the speed to the average value of all neighbour speeds
        speed = 0;
        for (int i = 0; i < neighbours.Count; i++)
        {
            speed += neighbours[i].speed;
        }
        speed /= neighbours.Count;
        // Clamp the speed between the maximum and minimuum values
        speed = Mathf.Clamp(speed, parameters.minimumSpeed, parameters.maximumSpeed);
    }

    // A function to determine whether a target is within the field of view of the agent
    private bool IsInFOV(Vector3 targetPosition)
    {
        // Get the angle between the target and the forward transform. Unsigned angle is used to give a value between 0 and 180 degrees
        float angle = Vector3.Angle(transform.forward, (targetPosition - transform.position));
        // Return true if the angle is within the field of view
        return (angle <= parameters.fieldOfView / 2);
    }

    // Calculations for the steering vectors
    // Seeking behaviour returns the normalised direction vector pointing towards the target
    private Vector3 GetSeekingVector(Vector3 targetPosition)
    {
        Vector3 seekingVector = targetPosition - position;
        seekingVector = seekingVector.normalized;

        return seekingVector;
    }

    // The wandering behaviour adds some randomness to the boids behaviour. In testing this behaviour did not tend to break up groups of boids, but rather simply add some variation to the path of each boid within the flock
    // The effects of this functions were not all that noticable. However, it did seem to make the flock behave more like a shoal of fish than a school
    private Vector3 GetWanderingVector()
    {
        // Get a random point in a unit sphere around the current position
        Vector3 wanderTarget = position + Random.insideUnitSphere;
        // Return a normalised direction vector to this point, multiplied by some optional scaling distance to increase wandering strength (set to 1 by default)
        Vector3 wanderingVector = wanderTarget - position;
        wanderingVector = wanderingVector.normalized * parameters.wanderDistanceMultiplier;
        return wanderingVector;
    }

    // The cohesion behaviour steers the agent towards the centre of its neighbours
    private Vector3 GetCohesionVector()
    {
        // If there are no neighbours, return a zero vector
        Vector3 cohesionVector = Vector3.zero;
        if (neighbours.Count == 0) { return cohesionVector; }

        // Sum the positions of each neighobur within FOV. Count the number of points summed
        int neighboursInFOV = 0;
        for (int i = 0; i < neighbours.Count; i++)
        {
            if (IsInFOV(neighbours[i].position))
            {
                cohesionVector += neighbours[i].position;
                neighboursInFOV++;
            }
        }
        // Divide by number of points to get the average position
        cohesionVector /= neighboursInFOV;
        // Return the normalsed direction vector to the centre point
        cohesionVector -= position;
        cohesionVector = cohesionVector.normalized;
        return cohesionVector;
    }

    // The alignment behavour steers the agent toward th average heading of its neighbours
    private Vector3 GetAlignmentVector()
    {
        // If there are no neighoburs, return the forward vector to maintain current course
        Vector3 alignmentVector = transform.forward;
        if (neighbours.Count == 0) { return alignmentVector; };

        // Sum the transform.forward vectors of each neighbour within FOV and take average
        int neighboursInFOV = 0;
        for (int i = 0; i < neighbours.Count; i++)
        {
            if (IsInFOV(neighbours[i].position))
            {
                alignmentVector += neighbours[i].transform.forward;
                neighboursInFOV++;
            }
        }
        alignmentVector /= neighboursInFOV;
        // Return the normalised average heading
        alignmentVector = alignmentVector.normalized;
        return alignmentVector;
    }

    // The separation vector steers the agent away from its neigbhours if too close to maintain some separation distance
    private Vector3 GetSeparationVector()
    {
        // Return a zero vector if there are no neighbours to steer away from
        Vector3 separationVector = Vector3.zero;
        if (neighbours.Count == 0) { return separationVector; }

        // For each neighbour, get the direction vector pointing from the neighbour to the agent. Average these values
        int neighboursInFOV = 0;
        for (int i = 0; i < neighbours.Count; i++)
        {
            if (IsInFOV(neighbours[i].position))
            {
                separationVector += (position - neighbours[i].position);
                neighboursInFOV++;
            }
        }
        separationVector /= neighboursInFOV;
        // Return the normalised, average separation vector
        separationVector = separationVector.normalized;
        return separationVector;

    }

    // The avoidance behaviour steers the agent to avoid obstacles
    private Vector3 GetAvoidanceVector()
    {
        Vector3 avoidanceVector = Vector3.zero;

        // Check for obstacles with an 'avoidanceRange' length raycast over the obstacle mask
        RaycastHit hit;
        if (Physics.Raycast(position, transform.forward, out hit, parameters.avoidanceRange, parameters.obstacleMask))
        {
            // If an obstacle was detercted, return a steering vector poiting away from the obstacle
            avoidanceVector = AvoidObstacle();
        }
        // If no collision was detected, set the previousAvoidanceVector variable (used later) to the zero vector, then return the zero vector
        previousAvoidanceVector = avoidanceVector;

        return avoidanceVector;

    }

    // A function to determine the best direction to avoid an obstacle, then steer in that direction
    private Vector3 AvoidObstacle()
    {
        Vector3 selectedDirection = Vector3.zero;

        // If a non-zero avoidance vector has already been calculated check whether this one is still good before recalculating
        if (previousAvoidanceVector != Vector3.zero)
        {
            // Raycast along the previousAvoidanceVector. If nothing is hit, then the direction is still obstacle free. Return that direction
            RaycastHit hit;
            if (!Physics.Raycast(position, transform.forward, out hit, parameters.avoidanceRange, parameters.obstacleMask))
            {
                selectedDirection = previousAvoidanceVector;
                return selectedDirection;
            }
        }

        // The furthestDistance variable is used to determine which direction can be travelled in the furtherst before an obstacle is hit
        float furthestDistance = int.MinValue;
        // Go through each direction in avoidanceRayDirections[] to find the ray that travels the furthest without collision
        for (int i = 0; i < parameters.avoidanceRayDirections.Length; i++)
        {
            // Raycast in the [i]th direction
            RaycastHit hit;
            Vector3 currentDirection = transform.TransformDirection(parameters.avoidanceRayDirections[i].normalized);
            if (Physics.Raycast(position, currentDirection, out hit, parameters.avoidanceRange, parameters.obstacleMask))
            {
                // If the hit is further away than the previous furthest hit, select this direction as the new best direction
                float currentDistance = (hit.point - position).sqrMagnitude;
                if (currentDistance > furthestDistance)
                {
                    furthestDistance = currentDistance;
                    selectedDirection = currentDirection;
                }
            }
            else
            {
                // If the raycast hit nothing, select this direction and do not check any others
                selectedDirection = currentDirection;
                return selectedDirection.normalized;
            }
        }
        return selectedDirection.normalized;
    }

    // The bounds behaviour keeps the agents within some spherecial bounds specified by boundsRadius
    private Vector3 GetBoundsVector()
    {
        // Get the direction vector to the centre of the bounds
        Vector3 offsetToCentre = flock.transform.position - position;

        // If the agent is outwith the bounds, return the normalised direction vector to steer it back towards the centre
        if (offsetToCentre.sqrMagnitude > (parameters.boundsRadius * parameters.boundsRadius))
        {
            offsetToCentre = offsetToCentre.normalized;
            return offsetToCentre;
        }
        // If the agent is within the bounds, return a zero vector
        return Vector3.zero;
    }
}
