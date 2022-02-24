using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BoidParameters))]

public class BoidFlock : MonoBehaviour
{
    // A script to manage the boid agents each update

    private List<BoidAgent> agents;
    private BoidParameters parameters;

    [Header("Spawn Setup")]
    public BoidAgent agentPrefab;
    public int flockSize;
    public Vector3 spawnBounds;

    private void Start()
    {
        parameters = GetComponent<BoidParameters>();

        // Spawn boids
        agents = new List<BoidAgent>();
        for (int i = 0; i < flockSize; i++)
        {
            // Random position within the bounds radius
            Vector3 spawnPositionOffset = UnityEngine.Random.insideUnitSphere;
            spawnPositionOffset = new Vector3(spawnPositionOffset.x * spawnBounds.x, spawnPositionOffset.y * spawnBounds.y, spawnPositionOffset.z * spawnBounds.z);
            Vector3 spawnPosition = transform.position + spawnPositionOffset;
            // Random rotation around the y-axis and x-axis
            var rotation = Quaternion.Euler(0, UnityEngine.Random.Range(180, 180), 0);

            // Instantiate agent and add to agents list
            BoidAgent agent = Instantiate(agentPrefab, spawnPosition, rotation, transform);
            agent.Initialise(this, parameters);
            agent.name = parameters.agentType + " " + i;
            agents.Add(agent);
        }
    }

    private void Update()
    {
        // Each update, for each agent in the list, get that agent's neighbours and move it according to the boid agent script
        for (int i = 0; i < agents.Count; i++)
        {
            List<BoidAgent> neighbours = GetNeighbours(agents[i]);
            agents[i].MoveAgent(neighbours);
        }
    }

    // Get the neighbours of the current agent
    private List<BoidAgent> GetNeighbours(BoidAgent agent)
    {
        List<BoidAgent> neighbours = new List<BoidAgent>();

        // Loop through each possible neighbouring agent in the list
        for (int j = 0; j < agents.Count; j++)
        {
            // Prevent an agent from neighbouring itself
            if (agents[j] == agent) { continue; }
            // Get the square separation and compare it to the square of perception radius. Squaring perceptionRadius is more efficient that square rooting the separation to get magnitude
            float sqrSeparation = Vector3.SqrMagnitude(agents[j].transform.position - agent.transform.position);
            // If the agent is within the perception radius, add it as a neighbour
            if (sqrSeparation <= parameters.perceptionRadius * parameters.perceptionRadius)
            {
                neighbours.Add(agents[j]);
            }
        }
        return neighbours;
    }
}
