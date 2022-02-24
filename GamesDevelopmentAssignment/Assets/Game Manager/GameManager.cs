using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public float treasureToWin;
    private float treasureCollected;
    public float maximumPlayerHealth;
    [SerializeField] private float playerHealth;

    public float maximumBreathTimer;
    [SerializeField] private float playerBreathTimer;

    public float maximumDrownTimer;
    [SerializeField] private float playerDrownTimer;

    public Image bloodEffect, drowningEffect, drowningVignette;
    private float bloodEffectAlpha, drowningEffectAlpha, drowningVignetteAlpha;
    public Text breathCounterUI;

    private bool levelComplete = false;
    public GameObject ui;
    public GameObject victoryScreen;
    public GameObject defeatScreen;

    private void Start()
    {
        treasureCollected = 0f;
        playerHealth = maximumPlayerHealth;
        playerBreathTimer = maximumBreathTimer;
        playerDrownTimer = maximumDrownTimer;

        // Activate the UI, but not the end level screens, these get activated as required later
        ui.SetActive(true);
        victoryScreen.SetActive(false);
        defeatScreen.SetActive(false);
    }

    private void Update()
    {
        // A simple conditional to prevent the game manager from doing anything once the level is complete
        if (!levelComplete)
        {
            // Update UI elements
            UpdateUI();

            // A timer to handle the amount of breath the player has left. Once expired, the drown timer begins to kill the player
            if (playerBreathTimer <= 0)
            {
                Drown();
            }

            // Victory condition
            if (treasureCollected >= treasureToWin)
            {
                Win();
            }

            // Defeat conditions
            if (playerHealth <= 0)
            {
                Lose();
            }
            if (playerDrownTimer <= 0)
            {
                Lose();
            }
        }
    }

    private void UpdateUI()
    {
        // Change the alpha of UI elements to make them appear as certain values decrease/increase
        // Remap the player health from 1->maxHealth to 0->1. Take 1 - answer to get an increasing alpha value as health decreases
        bloodEffectAlpha = 1 - ((playerHealth - 1) / (maximumPlayerHealth - 1));
        drowningEffectAlpha = 1 - ((playerDrownTimer - 1) / (maximumDrownTimer -  1));
        drowningVignetteAlpha = 1 - ((playerDrownTimer + 3) / (maximumDrownTimer + 3));

        // Change the alpha of the UI element to the determined value
        Color colour = bloodEffect.color;
        colour.a = bloodEffectAlpha;
        bloodEffect.color = colour;

        colour = drowningEffect.color;
        colour.a = drowningEffectAlpha;
        drowningEffect.color = colour;

        colour = drowningVignette.color;
        colour.a = drowningVignetteAlpha;
        drowningVignette.color = colour;

        // Change the UI breath counter (without decimal points) to reflect the breath remaining. 
        breathCounterUI.text = playerBreathTimer.ToString("0");
    }

    // A function to be called from the OpenChest script. Adds the treasure value to the total collected
    public void CollectTreasure(float value)
    {
        treasureCollected += value;
        Debug.Log("Treasure collected, value: " + value + ". New total: " + treasureCollected);
    }

    // A function to be called from the SharkFSM script. Reduces player health by damage value
    public void DamagePlayer(float damage)
    {
        playerHealth -= damage;
    }

    // A function to be called each frame from the player script (only called when the player is out of the submarine)
    public void HoldBreath()
    {
        // Decrease the breath timer
        if (playerBreathTimer > 0)
            playerBreathTimer -= Time.deltaTime;
        // Limit the breath timer to zero to avoid negative numbers in the UI
        if (playerBreathTimer < 0)
            playerBreathTimer = 0;
    }

    // A function to be called when the submarine controller script is enabled. When the player is in the submarine, increase the breath counter
    public void RefreshBreath(float breathRefreshRate)
    {
        // Increase breath counter
        if (playerBreathTimer < maximumBreathTimer)
            playerBreathTimer += breathRefreshRate * Time.deltaTime;
        // Limit the breath timer to its maximum value
        else if (playerBreathTimer > maximumBreathTimer)
            playerBreathTimer = maximumBreathTimer;
        // Reset the drown timer if the player reaches the submarine
        if (playerDrownTimer < maximumDrownTimer)
            playerDrownTimer = maximumDrownTimer;
    }

    // If the drown timer expires, the player loses
    private void Drown()
    {
        playerDrownTimer -= Time.deltaTime;
        if (playerDrownTimer <= 0)
        {
            Lose();
        }
    }
    
    // Enable the victory screen, disable the UI
    private void Win()
    {
        Debug.Log("Win");
        // Allow mouse movement for clicking buttons
        Cursor.lockState = CursorLockMode.Confined;
        levelComplete = true;
        ui.SetActive(false);
        victoryScreen.SetActive(true);
        defeatScreen.SetActive(false);
    }

    // Enable the defeat screen, disable the UI
    private void Lose()
    {
        Cursor.lockState = CursorLockMode.Confined;
        Debug.Log("Lose");
        levelComplete = true;
        ui.SetActive(false);
        victoryScreen.SetActive(false);
        defeatScreen.SetActive(true);
    }
}
