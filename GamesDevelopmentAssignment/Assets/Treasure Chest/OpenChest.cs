using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Animator))]
public class OpenChest : MonoBehaviour
{
    // Enum of chest values
    public enum chestType
    {
        common,
        rare,
        epic
    }

    public chestType chestValue;

    public bool isOpen = false;

    public GameManager gameManager;
    private Animator animator;

    private void Start()
    {
        animator = GetComponent<Animator>();
    }

    public void Open()
    {
        // Only open the chest if not already open
        if (!isOpen)
        {
            float treasureValue = 0f;

            // Player the chest opening animation
            animator.SetBool("isChestOpen", true);

            // Set the value of the chest according to the enum value selected in inspector
            switch (chestValue)
            {
                case chestType.common:
                    treasureValue = 5f;
                    break;
                case chestType.rare:
                    treasureValue = 10f;
                    break;
                case chestType.epic:
                    treasureValue = 20f;
                    break;
                default:
                    treasureValue = 0f;
                    Debug.Log("Error in OpenChest.Open(): chest type not recognised");
                    break;
            }
            // Access the collect treasure function from the game manager to increase the total treasure collected
            gameManager.CollectTreasure(treasureValue);
            // Set isOpen to prevent reopening (line 32)
            isOpen = true;
        }
    }
}
