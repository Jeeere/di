using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Evaluation : MonoBehaviour
{
    private BarChartUpdater barChartUpdater;
    private RelativeTemperatureVisualizer thermostat;
    private Co2Meter co2Meter;
    private ApiManager apiManager;
    private int barChartInterval = 2;

    [SerializeField] private bool autoRun = false;

    // Start is called before the first frame update
    async Task Start()
    {
        Debug.LogWarning("Evaluation script enabled");

        // Wait for other scripts to initialize
        while (true)
        {
            await Task.Delay(500);

            barChartUpdater = FindObjectOfType<BarChartUpdater>();
            thermostat = FindObjectOfType<RelativeTemperatureVisualizer>();
            co2Meter = FindObjectOfType<Co2Meter>();
            apiManager = FindObjectOfType<ApiManager>();

            if (barChartUpdater != null && thermostat != null && co2Meter != null && apiManager != null)
            {
                break;
            }
        }

        await barChartUpdater.ChangeWeek(40);

        if (autoRun == true)
        {
            // Idle for one minute
            await Task.Delay(60000);
            StartRunTests();
        }
    }

    public void StartRunTests()
    {
       StartCoroutine(RunTest());
    }

    private IEnumerator RunTest()
    {
        // Run thermostat test and wait
        StartCoroutine(TestThermostat(4));
        yield return new WaitForSeconds(90);

        // Run co2 meter test and wait
        StartCoroutine(TestCo2Meter(60));
        yield return new WaitForSeconds(90);

        // Run bar chart test and wait
        StartCoroutine(TestBarChart(5));
        yield return new WaitForSeconds(90);

        // Run all tests at once
        StartCoroutine(TestThermostat(2));
        StartCoroutine(TestCo2Meter(30));
        StartCoroutine(TestBarChart(7));
        yield return new WaitForSeconds(90);

        // Change scene to end scene
        SceneManager.LoadScene("tests_ended");
    }

    private IEnumerator TestCo2Meter(int seconds)
    {
        Debug.Log("[START] CO2 test");

        co2Meter.StartUpdatingCo2();
        Debug.Log("CO2 meter enabled");
        yield return new WaitForSeconds(seconds);
        Debug.Log("CO2aaa");
        co2Meter.StopAllCoroutines();
        Debug.Log("CO2 meter disabled");
        yield return new WaitForSeconds(5);

        Debug.Log("[END] CO2 test");
    }

    private IEnumerator TestBarChart(int rounds)
    {
        Debug.Log("[START] barchart test");

        for (int i = 0; i < rounds; i++)
        {
            var previousWeekTask = barChartUpdater.PreviousWeek();
            yield return new WaitUntil(() => previousWeekTask.IsCompleted);

            var nextWeekTask = barChartUpdater.NextWeek();
            yield return new WaitUntil(() => nextWeekTask.IsCompleted);

            yield return new WaitForSeconds(barChartInterval);
        }

        Debug.Log("[END] barchart test");
    }

    private IEnumerator TestThermostat(int rounds)
    {
        Debug.Log("[START] thermostat test");

        thermostat.thermostatTemperature = apiManager.todayData.Last["temperature"].ToObject<float>();
        thermostat.UpdateThermostatText();

        for (int i = 0; i < rounds; i++)
        {
            thermostat.ThermostatUp(2.0f);
            yield return new WaitForSeconds(7);
            thermostat.ThermostatDown(4.0f);
            yield return new WaitForSeconds(7);
            thermostat.ThermostatUp(2.0f);
            yield return new WaitForSeconds(7);
        }

        Debug.Log("[END] thermostat test");
    }

    private IEnumerator ChangeWeekAsync()
    {
        var previousWeekTask = barChartUpdater.PreviousWeek();
        yield return new WaitUntil(() => previousWeekTask.IsCompleted);

        var nextWeekTask = barChartUpdater.NextWeek();
        yield return new WaitUntil(() => nextWeekTask.IsCompleted);
    }
}
