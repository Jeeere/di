using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.Interaction.Toolkit;

public class XRAnchorManager : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        gameObject.AddComponent<ARAnchor>();

        // Get the XRGrabInteractable component
        XRGrabInteractable grab = gameObject.GetComponent<XRGrabInteractable>();

        // Check if the component has a XRGrabInteractable component
        if (grab != null)
        {
            // The component has to be enabled after starting to avoid issues with buttons
            grab.enabled = true;
        }
    }

    /// <summary>
    /// This method is called when the object is grabbed, removing the ARAnchor component.
    /// </summary>
    public void OnGrab()
    {
        Destroy(gameObject.GetComponent<ARAnchor>());
        Debug.Log("Anchor destroyed");
    }

    /// <summary>
    /// This method is called when the object is released, adding the ARAnchor component back.
    /// </summary>
    public void OnRelease()
    {
        gameObject.AddComponent<ARAnchor>();
        Debug.Log("Anchor added");
    }
}
