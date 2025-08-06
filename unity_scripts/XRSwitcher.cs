using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.XR.ARFoundation;

public class XRSwitcher : MonoBehaviour
{
    [SerializeField] private Scrollbar SwitchScrollBar;
    [SerializeField] private GameObject VR;
    [SerializeField] private GameObject AR;
    [SerializeField] private GameObject camera;

    private int currentXR = 0;

    // Start is called before the first frame update
    void Start()
    {
        Debug.Log("[END] Scene");
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    /// <summary>
    /// This method is called when the value of the ScrollBar is changed.
    /// </summary>
    public void OnValueChanged()
    {
        Debug.Log("Value: " + SwitchScrollBar.value);
        if (SwitchScrollBar.value > 0.5 && currentXR == 0)
        {
            VR.SetActive(false);
            AR.SetActive(true);
            camera.GetComponent<ARCameraManager>().enabled = true;
            currentXR = 1;

            Debug.Log("Switched to AR");
        }
        else if (SwitchScrollBar.value <= 0.5 && currentXR == 1)
        {
            AR.SetActive(false);
            camera.GetComponent<ARCameraManager>().enabled = false;
            VR.SetActive(true);
            currentXR = 0;

            Debug.Log("Switched to VR");
        }
    }

    public void SwitchScene()
    {
        // Get current scene
        Scene currentScene = SceneManager.GetActiveScene();

        // Get the other scene
        //string otherScene = currentScene.name == "2 Game Scene" ? "3 AR Scene" : "2 Game Scene";
        string otherScene = currentScene.name == "2 Game Scene" ? "3 AR Scene" : "2 Game Scene";

        Debug.Log("[START] Scene");

        // Load the other scene
        SceneManager.LoadScene(otherScene);
    }

    public void ReloadScene()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
