using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using ChartAndGraph;
using Newtonsoft.Json.Linq;
using UnityEngine;
using static UnityEngine.XR.Interaction.Toolkit.XRInteractionGroup;

public class BarChartUpdater : MonoBehaviour
{
    public Material customCategoryMaterial;
    public int daysVisualized = 7;
    public int minutesPerGroup = 60;
    public string dataField = "motion";
    public GameObject apiManagerGameObject;

    private ApiManager apiManager;
    private System.DayOfWeek currentWeekday;
    private int selectedWeek;
    private readonly List<string> weekdays;
    private BarChart barChart;

    public BarChartUpdater()
    {
        weekdays = new List<string>
        {
        "Sunday",
        "Saturday",
        "Friday",
        "Thursday",
        "Wednesday",
        "Tuesday",
        "Monday"
        };
    }

    void Start()
    {
        apiManager = FindObjectOfType<ApiManager>();
        barChart = GetComponent<BarChart>();

        // Initialize the current weekday and selected week
        // Does not handle month change during use
        DateTime currentDateTime = DateTime.Now;
        currentWeekday = DateTime.Now.DayOfWeek;
        System.DayOfWeek group = currentWeekday;
        selectedWeek = (int)Math.Ceiling(DateTime.Now.DayOfYear / 7.0);

        // Initialize the bar chart with the current week
        try
        {
           ChangeWeek(selectedWeek);
        }
        catch (Exception e)
        {
           Debug.LogError(e);
        }
    }

    /// <summary>
    /// Changes the week to the previous week.
    /// </summary>
    public async Task PreviousWeek()
    {
        selectedWeek--;
        await ChangeWeek(selectedWeek);
    }

    /// <summary>
    /// Changes the week to the next week.
    /// </summary>
    public async Task NextWeek()
    {
        int currentWeek = DateTime.Now.DayOfYear / 7;

        // Check if the selected week is not the latest week
        if (selectedWeek < currentWeek)
        {
            selectedWeek++;
            await ChangeWeek(selectedWeek);
        }
    }

    /// <summary>
    /// Changes the week to the current week.
    /// </summary>
    public async Task CurrentWeek()
    {
        int currentWeek = (int)Math.Ceiling(DateTime.Now.DayOfYear / 7.0);

        // Check if the selected week is not the current week
        if (selectedWeek != currentWeek)
        {
            selectedWeek = currentWeek;
            await ChangeWeek(selectedWeek);
        }
    }

    /// <summary>
    /// Changes the week to the specified week.
    /// </summary>
    /// <param name="weekNumber">Number of the wanted week</param>
    public async Task ChangeWeek(int weekNumber)
    {
        // Does not handle year change during use
        int currentYear = DateTime.Now.Year;
        Debug.Log($"Changing week to {weekNumber} year {currentYear}");
        Debug.Log("[START] Barchart");

        // Check and print if apimanager is null
        if (apiManager == null)
        {
            Debug.LogError("ApiManager is null");
        }

        // Get time period for the week and fetch data
        TimePeriod week = apiManager.GetWeekStartAndEndUtc(2024, weekNumber);
        Debug.Log("aa");
        JArray data =  await apiManager.FetchDataRange(week);

        // Clear the bar chart
        barChart.DataSource.ClearGroups();

        // Make sure the bar chart is not null
        if (barChart != null)
        {
            // Loop through weekdays
            for (int i = 0; i < 7; i++)
            {
                DateTime lastTimestamp = DateTime.MinValue;
                JArray day = new();

                // Add the group (weekday) to the bar chart
                barChart.DataSource.AddGroup(weekdays[6 - i]);

                // Loop through the data
                while (data.Count > 0)
                {
                    // Check if the data is a JSON object
                    if (data[0] is JObject obj)
                    {
                        // Convert timestamp to the wanted format in local time
                        DateTime timestampUtc = DateTime.Parse(obj["time"].ToString(), CultureInfo.InvariantCulture);
                        DateTime timestampLocal = timestampUtc.ToLocalTime();

                        // Check if day has changed
                        if (lastTimestamp != DateTime.MinValue && lastTimestamp.Day != timestampLocal.Day || data.Count == 1)
                        {
                            // Aggregate the data for the day
                            JArray aggregatedData = apiManager.AggregateData(day, minutesPerGroup);

                            // Should be moved to another method
                            // Loop through the aggregated data and add it to the bar chart
                            foreach (var item in aggregatedData)
                            {
                                // Check if the data is a JSON object
                                if (item is JObject jObj)
                                {
                                    // Convert timestamp to the wanted format in local time
                                    DateTime aggregatedTimestampUtc = DateTime.Parse(jObj["time"].ToString(), CultureInfo.InvariantCulture);
                                    DateTime aggregatedTimestampLocal = aggregatedTimestampUtc.ToLocalTime();
                                    string category = aggregatedTimestampLocal.ToString("HH:mm");

                                    try
                                    {
                                        // Add the category (time) to the bar chart
                                        barChart.DataSource.AddCategory(category, customCategoryMaterial);
                                    }
                                    catch (Exception)
                                    {
                                        // Do nothing if the category already exists
                                    }

                                    // Add the data point to the bar chart
                                    barChart.DataSource.SetValue(category, weekdays[6 - i], jObj[dataField].ToObject<double>());

                                }
                            }
                            break;
                        }

                        // Append the data to the day and update the last timestamp
                        day.Add(obj);
                        lastTimestamp = timestampLocal;

                        // Remove the processed data point from the list
                        data.RemoveAt(0);
                    }
                }
            }
        }

        Debug.Log("[END] Barchart");
    }

    /// <summary>
    /// Asynchronously updates a specific group in the bar chart with new data fetched from an API.
    /// </summary>
    /// <param name="group">The group index to update,
    /// which corresponds to a specific day in the visualization.
    /// 0 is the current day, 1 is the day before, etc.</param>
    /// <param name="groupName">The name of the group (weekday) to update.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    private async Task UpdateCategory(int group, string groupName)
    {
        if (barChart != null)
        {
            barChart.DataSource.AddGroup(groupName);

            JArray data = await apiManager.FetchData(group);
            data = apiManager.AggregateData(data, minutesPerGroup);

            // Loop through the data and add it to the bar chart
            foreach (var item in data)
            {
                if (item is JObject obj)
                {
                    // Convert timestamp to the wanted format in local time
                    DateTime timestampUtc = DateTime.Parse(obj["time"].ToString(), CultureInfo.InvariantCulture);
                    DateTime timestampLocal = timestampUtc.ToLocalTime();
                    string category = timestampLocal.ToString("HH:mm");

                    try
                    {
                        // Add the category to the bar chart
                        barChart.DataSource.AddCategory(category, customCategoryMaterial);
                    }
                    catch (Exception)
                    {
                        // Do nothing if the category already exists
                    }

                    // Check if the data is not 0
                    if (obj[dataField].ToObject<double>() != 0)
                    {
                        // Add the data to the bar chart
                        barChart.DataSource.SetValue(category, groupName, obj[dataField].ToObject<double>());
                    }
                }
            }
        }
        else
        {
            Debug.LogError("Cannot find Bar Chart");
        }
    }
}
