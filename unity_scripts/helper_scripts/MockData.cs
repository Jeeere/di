using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json.Linq;

[System.Serializable]
public class SimulationParameter
{
    public string key;
    public float minValue;
    public float maxValue;
    public float updateInterval;
    public float stepSizePercentage = 0.05f;
}

public class MockData : MonoBehaviour
{
    public JArray mockData;

    [SerializeField]
    private List<SimulationParameter> simulationParameters;

    // Start is called before the first frame update
    void Start()
    {
        mockData = new JArray();
        mockData.Add(new JObject());

        foreach (var param in simulationParameters)
        {
            StartSimulatingData(param.key, param.minValue, param.maxValue, param.updateInterval, param.stepSizePercentage);
        }
    }

    public void StartSimulatingData(string key, float minValue, float maxValue, float updateInterval, float stepSizePercentage)
    {
        StartCoroutine(SimulateData(key, minValue, maxValue, updateInterval, stepSizePercentage));
    }

    /// <summary>
    /// Simulates data for a given key with a specified range and update interval.
    /// </summary>
    /// <param name="key">Name of the simualted value</param>
    /// <param name="minValue">Minimum value of the simulation range</param>
    /// <param name="maxValue">Maximum value of the simulation range</param>
    /// <param name="updateInterval">Seconds between updates</param>
    private IEnumerator SimulateData(string key, float minValue, float maxValue, float updateInterval, float stepSizePercentage)
    {
        bool increasing = true;
        float currentValue = minValue;
        float range = maxValue - minValue;
        float stepSize = range * stepSizePercentage;

        while (true)
        {
            // Update the value
            if (increasing)
            {
                currentValue += stepSize;
                if (currentValue >= maxValue)
                {
                    currentValue = maxValue;
                    increasing = false;
                }
            }
            else
            {
                currentValue -= stepSize;
                if (currentValue <= minValue)
                {
                    currentValue = minValue;
                    increasing = true;
                }
            }

            JObject dataObject = (JObject)mockData[0];
            dataObject[key] = currentValue;

            Debug.Log($"{key}: {currentValue}");
            Debug.Log(mockData);

            // Wait for the specified interval before updating again
            yield return new WaitForSeconds(updateInterval);
        }
    }
}
