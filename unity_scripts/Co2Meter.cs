using System.Collections;
using ChartAndGraph;
using TMPro;
using UnityEngine;

public class Co2Meter : MonoBehaviour
{
    public GameObject co2Display;
    public GameObject co2Chart;
    public int fadeTime = 3;
    public float updateInterval = 20;
    public Material co2LowMaterial;
    public int co2MediumThreshold = 1000;
    public Material co2MediumMaterial;
    public int co2HighThreshold = 2000;
    public Material co2HighMaterial;

    private ApiManager apiManager;
    private MockData mockData;
    private PieChart pieChart;

    [SerializeField] private bool autoStart = true;

    // Start is called before the first frame update
    IEnumerator Start()
    {
        apiManager = FindObjectOfType<ApiManager>();
        mockData = FindAnyObjectByType<MockData>();

        // Wait for the API manager to load the data
        while (true)
        {
            if (apiManager != null && apiManager.todayData != null || mockData != null)
            {
                break;
            }

            yield return new WaitForSeconds(5);
        }

        // Initialize the pie chart and start updating the CO2 data
        pieChart = co2Chart.GetComponent<PieChart>();

        if (autoStart)
        {
            StartUpdatingCo2();
        }
    }

    public void StartUpdatingCo2()
    {
        StartCoroutine(RepeatUpdateCo2());
    }

    /// <summary>
    /// Repeatedly updates the CO2 data.
    /// </summary>
    private IEnumerator RepeatUpdateCo2()
    {
        while (true)
        {
            Debug.Log("[START] CO2");
            yield return StartCoroutine(UpdateCo2());
            Debug.Log("[END] CO2");
            yield return new WaitForSeconds(updateInterval);
        }
    }

    /// <summary>
    /// Updates the CO2 data.
    /// </summary>
    private IEnumerator UpdateCo2()
    { 
        int minCo2 = 400;
        // float latestCo2 = apiManager.todayData.Last["co2"].ToObject<float>();
        float latestCo2 = mockData.mockData.Last["co2"].ToObject<float>();

        // Calculate the percentage of the CO2 value
        // Subtract the minimum CO2 value to shift the range
        float percentage = Mathf.Clamp01(Mathf.Abs(latestCo2 / co2HighThreshold));
        float invisible = 1 - percentage;

        // Update the CO2 display
        co2Display.GetComponent<TextMeshPro>().text = latestCo2 + " ppm";

        // Update the CO2 chart
        if (latestCo2 < co2MediumThreshold)
        {
            pieChart.DataSource.SetMaterial("co2", co2LowMaterial);
            pieChart.DataSource.SlideValue("co2", percentage, fadeTime);
            pieChart.DataSource.SlideValue("invisible", invisible, fadeTime);
        }
        else if (latestCo2 < co2HighThreshold)
        {
            pieChart.DataSource.SetMaterial("co2", co2MediumMaterial);
            pieChart.DataSource.SlideValue("co2", percentage, fadeTime);
            pieChart.DataSource.SlideValue("invisible", invisible, fadeTime);
        }
        else
        {
            pieChart.DataSource.SetMaterial("co2", co2HighMaterial);
            pieChart.DataSource.SlideValue("co2", percentage, fadeTime);
            pieChart.DataSource.SlideValue("invisible", invisible, fadeTime);
        }

        yield return null;
    }
}
