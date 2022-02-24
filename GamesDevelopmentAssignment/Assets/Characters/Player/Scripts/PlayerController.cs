using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public GameManager gameManager;
    public GameObject player;
    private Transform playerTransform;
    public Transform playerHand;

    public Rigidbody playerRigidbody;

    public float maximuimActivationDistance;

    public float lookSensitivity = 8f;
    private float rotationYaw = 0f, rotationPitch = 0f, rotationRoll = 0f;

    public float swimSpeed = 750f;
    public float strafingMultiplier = 0.75f;
    public float ascensionMultiplier = 0.5f;
    public float descensionMultiplier = 0.5f;

    private float horizontalInput = 0f, verticalInput = 0f;
    private bool ascensionInput = false;
    private bool descensionInput = false;
    private bool activationInput = false;

    private Vector3 playerMovement = Vector3.zero;

    void Start()
    {
        playerTransform = player.transform;
        // Lock the cursor to the middle of the screen, for the mouse following camera
        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        // Get the user inputs using the Unity Input Manager
        GetInputs();
        // Call the activate function only if the corresponding inupt flag has been set
        if (activationInput)
            Activate();
        // Turn the player to look at the mouse
        TurnPlayer();
        // Move the player
        MovePlayer();
        // Call the hold breath function from the game manager to deplete the player breath timer while not in the submarine
        gameManager.HoldBreath();
    }

    // A function to get user inputs
    private void GetInputs()
    {
        horizontalInput = Input.GetAxis("Horizontal");
        verticalInput = Input.GetAxis("Vertical");
        ascensionInput = Input.GetButton("Ascend");
        descensionInput = Input.GetButton("Descend");
        activationInput = Input.GetButtonDown("Activate");
    }

    // A function to activate interactables
    private void Activate()
    {
        // Raycast in the forward position from the player's hand game object (located in front of player)
        RaycastHit hit;
        if (Physics.Raycast(playerHand.position, playerTransform.forward, out hit, maximuimActivationDistance))
        {
            // If the raycast hits. Check the tag of the collider against preset cases
            Gizmos.color = Color.green;
            switch(hit.collider.tag) 
            {
                // If interaction is with the submarine, enter the submarine
                case "Submarine":
                    activationInput = false;
                    // Enable the submarine control script and diable the player game object. Switch the audio listerner to the new camera
                    hit.collider.gameObject.GetComponent<SubmarineController>().enabled = true;
                    hit.collider.gameObject.GetComponentInChildren<AudioListener>().enabled = true;
                    this.GetComponentInChildren<AudioListener>().enabled = false;
                    player.SetActive(false);
                    break;
                // If the interaction is with a chest, open the chest
                case "Chest":
                    activationInput = false;
                    // Call the open function from the OpenChest script component
                    hit.collider.gameObject.GetComponent<OpenChest>().Open();
                    break;
            }
        }
        else
        {
            Gizmos.color = Color.red;
        }
    }

    // Turn the playerto look toward the mouse
    private void TurnPlayer()
    {
        // Get the change in mouse movement since last update
        rotationYaw += Input.GetAxis("Mouse X") * lookSensitivity;
        rotationPitch -= Input.GetAxis("Mouse Y") * lookSensitivity;
        // Unused code to control the roll axis of the player
            // rotationRoll += -Input.GetAxis("RollAxis") * look sensitivity * Time.deltaTime;

        // Rotate the player accoridng to the changes in mouse movement
        playerTransform.localRotation = Quaternion.Euler(rotationPitch, rotationYaw, 0);
    }

    // Move the player
    private void MovePlayer()
    {
        // If holding the forward input, move the player in the newly rotated forwards direction with some swim speed
        if (verticalInput >= 0) { playerMovement = playerTransform.forward * verticalInput * swimSpeed * Time.deltaTime; }
        // If holding the backward input, move backwards with some swim speed affected by a strafing penalty
        else { playerMovement = playerTransform.forward * verticalInput * swimSpeed * strafingMultiplier * Time.deltaTime; }
        // Left/right movement is always affected by the strafing penalty
        playerMovement += playerTransform.right * horizontalInput * swimSpeed * strafingMultiplier * Time.deltaTime;
        // If holding the ascension input, ascend with some ascension speed penalty
        if (ascensionInput && !descensionInput)
            playerMovement += playerTransform.up * swimSpeed * ascensionMultiplier * Time.deltaTime;
        // If holding the descension input, descend with some descension speed penalty (ascension and descension cannot occur simultaneously)
        if (descensionInput && !ascensionInput)
            playerMovement -= playerTransform.up * swimSpeed * descensionMultiplier * Time.deltaTime;
        
        // Update the player rigidbody with the new movement vector
        playerRigidbody.velocity = playerMovement;
    }

    private void OnDrawGizmos()
    {
        Gizmos.DrawRay(playerHand.position, playerTransform.forward.normalized * maximuimActivationDistance);
    }
}
