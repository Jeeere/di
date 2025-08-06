using UnityEngine;
using UnityEngine.UI;

public class DebugConsole : MonoBehaviour
{
    public static DebugConsole instance;

    [SerializeField] private bool enableDebug = false;
    [SerializeField] private RectTransform displayRect;
    [SerializeField] private Text displayText;

    void Awake()
    {
        // If an instance of DebugConsole already exists, destroy this instance
        if (DebugConsole.instance != null)
        {
            DestroyImmediate(gameObject);
        }
        // Otherwise, set the instance to this object
        else
        {
            DebugConsole.instance = this;
        }

        // Disable the debug console if it is not enabled
        if (enableDebug == false)
        {
            gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Add a message to the debug console
    /// </summary>
    /// <param name="message"> Message to be logged</param>
    public void Log(string message)
    {
        try
        {
            displayText.text += message + "\n" + displayText.text;
        }
        catch
        {
            // Do nothing
        }
    }
}
