using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoidParameters : MonoBehaviour
{
    // A script full of values used by the boid agent and boid flock scripts

    [Header("Speed Setup")]
    [Range(0, 10)]
    public float minimumSpeed;

    [Range(0, 10)]
    public float maximumSpeed;

    [Range(0, 10)]
    public float turningSpeed;


    [Header("Detection Distances")]
    [Range(0, 360)]
    public float fieldOfView;

    [Range(0, 10)]
    public float perceptionRadius;

    [Range(0, 10)]
    public float separationRadius;

    [Range(0, 10)]
    public float avoidanceRange;

    [Range(0, 100)]
    public float boundsRadius;


    [Header("Behaviour Weights")]
    [Range(0, 10)]
    public float seekingWeight;

    [Range(0, 10)]
    public float wanderingWeight;

    [Range(0, 10)]
    public float cohesionWeight;

    [Range(0, 10)]
    public float separationWeight;

    [Range(0, 10)]
    public float alignmentWeight;

    [Range(0, 100)]
    public float avoidanceWeight;

    [Range(0, 10)]
    public float boundsWeight;


    [Header("Miscellaneous")]
    public string agentType;

    [HideInInspector]
    public Transform target;

    public LayerMask obstacleMask;
    public Vector3[] avoidanceRayDirections;

    [Range(0, 5)]
    public float wanderDistanceMultiplier;
}
