using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class RelativeTemperatureVisualizer : MonoBehaviour
{
    public Material coldMaterial;
    public Material hotMaterial;
    public float thermostatTemperature = 24.5f;
    public float maxDifference = 2.0f;
    public GameObject thermostatDisplay;
    public float maxAlpha = 0.9f;
    public ARPlaneManager planeManager;
    public PlaneClassification visualizationPlane;
    public GameObject visualizationObject;

    [SerializeField] private bool enableDebug = false;
    [SerializeField] private GameObject temperatureDisplay;

    private float currentAlpha = 0;
    private bool warmer = true;
    private ApiManager apiManager;
    private MockData mockData;

    IEnumerator Start()
    {
        apiManager = FindObjectOfType<ApiManager>();
        mockData = FindAnyObjectByType<MockData>();

        UpdateThermostatText();

        // Wait for the API data to be fetched before starting the temperature comparison
        while (true)
        {
            if (apiManager != null && apiManager.todayData != null || mockData != null)
            {
                Debug.Log("MOI");
                Debug.Log(apiManager.todayData);
                break;
            }
            else
            {
                Debug.Log("Data not yet fetched or ApiManager not found");
            }
            yield return new WaitForSeconds(5);
        }

        // Start the temperature comparison coroutine
        StartCoroutine(RepeatCompareThermostatTemperature());
    }

    /// <summary>
    /// Subscribes to the ARPlaneManager's planesChanged event to detect when new planes are added to the scene.
    /// </summary>
    private void OnEnable()
    {
        planeManager.planesChanged += OnPlanesChanged;
    }

    /// <summary>
    /// Unsubscribes from the ARPlaneManager's planesChanged event when the script is disabled.
    /// </summary>
    private void OnDisable()
    {
        planeManager.planesChanged -= OnPlanesChanged;
    }

    /// <summary>
    /// Handles the ARPlaneManager's planesChanged event, which is triggered when planes change.
    /// </summary>
    /// <param name="obj"></param>
    private void OnPlanesChanged(ARPlanesChangedEventArgs obj)
    {
        List<ARPlane> newPlane = obj.added;
        //DebugConsole debugConsole = FindObjectOfType<DebugConsole>();

        // Loop over new planes
        foreach (var plane in newPlane)
        {
            //DebugConsole.instance.Log("Plane classification: " + plane.classification);

            // Check if the plane's classification matches the visualizationPlane
            if (plane.classification == visualizationPlane)
            {
                visualizationObject = plane.gameObject;
                DebugConsole.instance.Log("Found matching plane with classification: " + visualizationPlane);
            }
            // If the plane's classification does not match the visualizationPlane,
            // destroy the plane's renderer if debug mode is disabled
            else
            {
                if (enableDebug == false)
                {
                    Destroy(plane.GetComponent<Renderer>());
                }
            }
        }
    }

    /// <summary>
    /// Repeatedly invokes the CompareThermostatTemperature coroutine to continuously compare the latest temperature data
    /// fetched from the API with the set thermostat temperature. This method ensures that temperature comparisons and
    /// adjustments to the GameObject's material are made periodically.
    /// </summary>
    private IEnumerator RepeatCompareThermostatTemperature()
    {
        while (true)
        {
            yield return StartCoroutine(CompareThermostatTemperature());
            yield return new WaitForSeconds(0.25f);
        }
    }

    /// <summary>
    /// Handler for the thermostat up button click event.
    /// </summary>
    public void ThermostatUp(float value=0.5f)
    {
        thermostatTemperature += value;
        UpdateThermostatText();
    }

    /// <summary>
    /// Handler for the thermostat down button click event.
    /// </summary>
    public void ThermostatDown(float value = 0.5f)
    {
        thermostatTemperature -= value;
        UpdateThermostatText();
    }

    /// <summary>
    /// Updates the thermostat text display with the current thermostat temperature.
    /// </summary>
    public void UpdateThermostatText()
    {
        thermostatDisplay.GetComponent<TextMeshPro>().text = thermostatTemperature + " °C";
    }

    /// <summary>
    /// Compares the latest temperature data fetched from the API with a set temperature,
    /// calculates the difference, and adjusts the GameObject's material alpha or changes the material
    /// based on the temperature difference.
    /// </summary>
    private IEnumerator CompareThermostatTemperature()
    {
        Debug.Log(apiManager.todayData);
        float latestTemperature = apiManager.todayData.Last["temperature"].ToObject<float>();
        //float latestTemperature = mockData.mockData.Last["temperature"].ToObject<float>();
        float temperatureDifference = latestTemperature - thermostatTemperature;
        bool isTemperatureDifferencePositive = temperatureDifference >= 0;
        float alpha = Mathf.Clamp01(Mathf.Abs(temperatureDifference / maxDifference)) * maxAlpha;
        Debug.Log("Temp: " + latestTemperature + ", temperature difference: " + temperatureDifference + ", alpha: " + alpha);

        if (temperatureDisplay != null)
        {
            temperatureDisplay.GetComponent<TextMeshPro>().text = latestTemperature + " °C";
        }

        // Check if material or alpha change is needed
        if (alpha != currentAlpha || warmer != isTemperatureDifferencePositive)
        {
            // If the sign of the previous and current temperature difference is the same, only fade the alpha
            if (warmer == isTemperatureDifferencePositive)
            {
                yield return Fade(currentAlpha, alpha);
            }
            // If the sign of the previous and current temperature difference is different,
            // change the material and fade the alpha
            else
            {
                Material nextMaterial = isTemperatureDifferencePositive ? hotMaterial : coldMaterial;

                // Fade out the current material, change the material, and fade in the new material
                yield return Fade(currentAlpha, 0);
                visualizationObject.GetComponent<Renderer>().material = nextMaterial;
                yield return Fade(0, alpha);

                warmer = isTemperatureDifferencePositive;
            }
        }
    }

    /// <summary>
    /// Initiates a fade effect on the GameObject's material,
    /// gradually changing the alpha value of the GameObject's material
    /// from a starting alpha to an ending alpha over a specified duration.
    /// </summary>
    /// <param name="startAlpha">Starting alpha value</param>
    /// <param name="endAlpha">Ending alpha value</param>
    /// <param name="fadeTime">Fade time in secondst</param>
    public Coroutine Fade(float startAlpha, float endAlpha, float fadeTime = 3)
    {
        return StartCoroutine(FadeCoroutine(startAlpha, endAlpha, fadeTime));
    }

    /// <summary>
    /// Gradually changes the alpha value of the GameObject's material
    /// from a starting alpha to an ending alpha over a specified duration.
    /// </summary>
    /// <param name="startAlpha">Starting alpha value</param>
    /// <param name="endAlpha">Ending alpha value</param>
    /// <param name="fadeTime">Fade time in secondst</param>
    private IEnumerator FadeCoroutine(float startAlpha, float endAlpha, float fadeTime)
    {
        Debug.Log("[START] Fade");

        float elapsedTime = 0;
        Color theColorToAdjust = visualizationObject.GetComponent<Renderer>().material.color;

        // Fade the alpha value of the material over time
        while (elapsedTime < fadeTime)
        {
            theColorToAdjust.a = Mathf.Lerp(startAlpha, endAlpha, (elapsedTime / fadeTime));
            visualizationObject.GetComponent<Renderer>().material.color = theColorToAdjust;
            elapsedTime += Time.deltaTime;

            yield return null;
        }

        // Ensure the alpha value is set to the end value
        theColorToAdjust.a = endAlpha;
        visualizationObject.GetComponent<Renderer>().material.color = theColorToAdjust;

        // Update the new alpha value to a class variable
        currentAlpha = endAlpha;

        Debug.Log("[END] Fade");
    }
}
