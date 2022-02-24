using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SubmarineController : MonoBehaviour
{
    public GameManager gameManager;

    public GameObject player;
    public GameObject submarine;
    public Transform playerExit;
    public Transform propeller;
    private Transform submarineTransform;

    public Rigidbody submarineRigidbody;

    public float breathRefreshRate;

    public float movementSpeed;
    public float yawTurnSpeed, pitchTurnSpeed;
    public float maximumPropellerSpeed;
    public float propellerDampenerStrength;
    private float propellerSpeed = 0f;
    private float currentPropellerSpeed = 0f;

    private float yawInput = 0f, pitchInput = 0f;
    private bool forwardInput = false, backwardInput = false;
    private bool activationInput = false;

    public float bankingStrength;
    [Range(0, 1)]
    public float bankingSpeed;
    public float maximumBank;
    private float bankingRoll = 0f;
    private Vector3 totalRotation = Vector3.zero;

    [Header("Camera")]
    public Transform submarineCamera;
    public Transform cameraFollowPoint;
    public Vector3 cameraOffset;

    void Start()
    {
        submarineTransform = submarine.transform;
    }

    void Update()
    {
        // Get the user inputs using the Unity Input Manager
        GetInputs();

        // If the activation input has been triggered, exit the submarine
        if (activationInput)
        {
            // Clear the activation input flag
            activationInput = false;
            // Set the player position to just behind the submarine
            player.transform.position = playerExit.position;
            // Activate the player game object and player camera listener
            player.SetActive(true);
            player.GetComponentInChildren<AudioListener>().enabled = true;
            // Disable the submarine camera listner and submarine controller script (the submarine remains in world space, but will no longer recieve inputs)
            this.GetComponentInChildren<AudioListener>().enabled = false;
            this.enabled = false;
        }
        // Move the submarine according to the user inputs
        MoveSubmarine();
        // Set the rotation of the camera
        MoveCamera();
        // Spin the propeller game object
        SpinPropeller();
        // Call the RefreshBreath() function within the game manager to replenish the player breath counter while in the submarine
        gameManager.RefreshBreath(breathRefreshRate);
    }

    // A function to get the user inputs with the Unity Input Manager
    private void GetInputs()
    {
        // Get the yaw input (rotation around the x-axis)
        yawInput = Input.GetAxis("Horizontal") * yawTurnSpeed * Time.deltaTime;
        // Get the pitch input (rotation around the y-axis)
        pitchInput = Input.GetAxis("Vertical") * pitchTurnSpeed * Time.deltaTime;
        // Forward and backward inputs are boolean variables which are true as long as the corresponding inputs are high
        forwardInput = Input.GetButton("Ascend");
        backwardInput = Input.GetButton("Descend");
        // The activation input is a boolean variable only set true when the activate key is pressed
        activationInput = Input.GetButtonDown("Activate");
    }

    // The function to move the submarine according to user inputs
    private void MoveSubmarine()
    {
        // If either the forward or backward keys are pressed
        if (forwardInput || backwardInput)
        {
            // Unused code for banking the submarine in the roll direction as it turned in the yaw direction
                //if (yawInput < -0.1 || yawInput > 0.1)
                //    bankingRoll = -yawInput * bankingStrength;
                //else
                //    bankingRoll = 0f;
                //bankingRoll = Mathf.Clamp(bankingRoll, -maximumBank, maximumBank);

            // Rotate the submarine in yaw and pitch according to their respective input values
            totalRotation += new Vector3(pitchInput, yawInput, 0);

            // Unused code for rotating in the roll direction
                // totalRotation = Vector3.Lerp(totalRotation, new Vector3(pitchInput, yawInput, bankingRoll), bankingSpeed);

            // Apply the submarine rotation
            submarineTransform.rotation = Quaternion.Euler(totalRotation);
            submarineTransform.rotation = Quaternion.Euler(totalRotation.x, totalRotation.y, 0);
            
            // Move submarine in the newly rotated transform.forward direction
            submarineRigidbody.velocity = (forwardInput) ? (transform.forward * movementSpeed) : (-transform.forward * movementSpeed);
        }
        // Unused code to reset the roll rotation when the submarine stops moving forward
            //bankingRoll = 0f;
            //totalRotation = Vector3.Lerp(totalRotation, new Vector3(pitchInput, yawInput, bankingRoll), bankingSpeed);
            //submarineTransform.rotation = Quaternion.Euler(totalRotation);
    }

    // A function to rotate the camera
    private void MoveCamera()
    {
        // Point the camera towards a transform located just above the submarine
        submarineCamera.LookAt(cameraFollowPoint);
    }

    // A function to spin the propeller
    private void SpinPropeller()
    {
        // Spin in opposide directions depending on submarine direction
        if (forwardInput)
            propellerSpeed = maximumPropellerSpeed;
        else if (backwardInput)
            propellerSpeed = -maximumPropellerSpeed;
        // If the submarine is not moving, slowly bring the rotation to a stop
        else
            propellerSpeed = Mathf.SmoothDamp(propellerSpeed, 0, ref currentPropellerSpeed, propellerDampenerStrength);

        // Rotate the propeller around the forward axis, according to the speed specified above
        propeller.RotateAround(propeller.position, propeller.forward, propellerSpeed);

    }
}
