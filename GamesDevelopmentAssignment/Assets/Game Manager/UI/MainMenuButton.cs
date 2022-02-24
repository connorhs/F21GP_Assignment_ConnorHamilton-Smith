using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuButton : MonoBehaviour
{
    public void OnButtonPress()
    {
        // Load main menu at scene index 0
        SceneManager.LoadScene(0);
    }
}
