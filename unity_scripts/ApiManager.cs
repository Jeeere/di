using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using UnityEngine;

public class ApiManager : MonoBehaviour
{
    // Variables for storing current day's data
    public JArray todayData;
    public JArray todayDataAggregated;

    [SerializeField] private string apiKey = "";
    [SerializeField] private string deviceIds = "A81758FFFE030D4E";
    [SerializeField] private List<string> deviceIdsList = new List<string> { "A81758FFFE030D4E", "A81758FFFE030FA6" };

    private readonly string credentialsJson = "{\"email\":\"\",\"password\":\"\"}";
    private static readonly HttpClient httpClient = new();
    private string accessToken;
    private readonly int fetchIntervalMinutes = 1;

    /// <summary>
    /// Initializes a new instance of the <see cref="ApiManager"/> class.
    /// Sets the base address for the HTTP client, clears existing request headers,
    /// and adds a header to accept JSON responses.
    /// </summary>
    public ApiManager()
    {
        httpClient.BaseAddress = new Uri("https://query-backend-sensor-data-modeling.2.rahtiapp.fi/");
        httpClient.DefaultRequestHeaders.Accept.Clear();
        httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    }

    // Start is called before the first frame update
    void Start()
    {
        //Start the data fetching loop
        InvokeRepeating(nameof(FetchDataWrapper), 0, fetchIntervalMinutes * 60);
    }

    /// <summary>
    /// Initiates an asynchronous task to fetch today's data and then aggregate it.
    /// Wraps the <see cref="FetchData"/> and <see cref="AggregateData"/> calls in a background task.
    /// It updates the <see cref="todayData"/> and <see cref="todayDataAggregated"/> variables with the fetched and aggregated data.
    /// </summary>
    private void FetchDataWrapper()
    {
        // Start a background task
        Task.Run(async () =>
        {
            // Get the data for the current day
            var fetchedData = await FetchData(0);

            // If the data was fetched successfully, aggregate it
            if (fetchedData != null)
            {
                lock (this)
                {
                    // Store the fetched and aggregated data
                    todayData = fetchedData;
                    todayDataAggregated = AggregateData(todayData, 60);
                    Debug.Log("Today's data fetched");
                }
            }
        });
    }

    /// <summary>
    /// Asynchronously logs in to the API using predefined credentials.
    /// On successful login, stores the access token in a class variable.
    /// </summary>
    public async Task Login()
    {
        string url = "api/auth/login";
        HttpContent content = new StringContent(credentialsJson, System.Text.Encoding.UTF8, "application/json");

        try
        {
            // Send a POST request to the login endpoint
            HttpResponseMessage response = await httpClient.PostAsync(httpClient.BaseAddress + url, content);

            // If the request was successful, store the access token
            if (response.IsSuccessStatusCode)
            {
                string responseJson = await response.Content.ReadAsStringAsync();
                var responseItems = JObject.Parse(responseJson);
                accessToken = responseItems["access_token"].ToString();
                Debug.Log("Login successful");
            }
            else
            {
                Debug.LogError("Login failed: " + response.StatusCode);
            }
        }
        catch (Exception e)
        {
            Debug.LogError("Login failed: " + e.Message);
        }
    }

    /// <summary>
    /// Asynchronously fetches sensor data for a specified day.
    /// Adjusts for local timezone, formats the request, and handles the API response.
    /// Automatically retries by re-authenticating if the session has expired.
    /// </summary>
    /// <param name="daysAgo">The number of days ago from the current date for which to fetch data.</param>
    /// <returns>A <see cref="JArray"/> containing the fetched sensor data, or null if an error occurs.</returns>
    public async Task<JArray> FetchData(int daysAgo)
    {
        int retryCount = 0;
        int maxRetries = deviceIdsList.Count - 1;

        // Calculate the start and end times based on daysAgo, adjusted for the local timezone
        TimeZoneInfo localZone = TimeZoneInfo.Local;
        DateTime targetDateLocal = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, localZone).Date.AddDays(-daysAgo);
        DateTime dayStartLocal = targetDateLocal;
        DateTime dayEndLocal = targetDateLocal.AddDays(1).AddTicks(-1);

        // Convert the start and end times back to UTC for the API call
        DateTime dayStartUtc = TimeZoneInfo.ConvertTimeToUtc(dayStartLocal, localZone);
        DateTime dayEndUtc = TimeZoneInfo.ConvertTimeToUtc(dayEndLocal, localZone);

        // Format the UTC times as ISO 8601 strings with milliseconds and 'Z' to indicate UTC
        string formattedDayStartUtc = dayStartUtc.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
        string formattedDayEndUtc = dayEndUtc.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");

        Debug.Log($"Fetching data for {targetDateLocal} (UTC: {formattedDayStartUtc} - {formattedDayEndUtc})");

        // Add the authorization header
        // API token functionality does not work in the backend; access token used instead
        // httpClient.DefaultRequestHeaders.Add("Authorization", $"Api-key {apiKey}");
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        while (retryCount < maxRetries)
        {
            try
            {
                string url = $"api/events.json?deviceIds={deviceIdsList[retryCount]}&from={formattedDayStartUtc}&to={formattedDayEndUtc}&metrics=battery,co2,humidity,light,luminosity,motion,temperature&";
                // Send a GET request to the API
                HttpResponseMessage response = await httpClient.GetAsync(httpClient.BaseAddress + url);

                // If the request was successful, parse the JSON response
                if (response.IsSuccessStatusCode)
                {
                    string jsonString = await response.Content.ReadAsStringAsync();
                    var json = JArray.Parse(jsonString);

                    if (json.Count == 0)
                    {
                        retryCount++;
                        Debug.Log("Retrying with another device ID...");
                    }
                    else
                    {
                        return json;
                    }
                }
                // If the session has expired, re-authenticate and try again
                else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    Debug.Log("Session expired, attempting to log in again...");
                    await Login();
                    return await FetchData(daysAgo);
                }
                // Handle other types of errors
                else
                {
                    // Handle other types of errors.
                    Debug.LogError($"Failed to fetch data: {response.StatusCode}");

                    // Wait for a minute and try again
                    await Task.Delay(60000);
                    return await FetchData(daysAgo);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"An error occurred: {e.Message}");
                return null;
            }
        }
        Debug.LogError("Failed to fetch data for all device IDs");
        return null;
    }

    /// <summary>
    /// Gets the start and end times for a specific week in UTC.
    /// Calculates the first Monday of the year and adjusts for the specified week number.
    /// </summary>
    /// <param name="year">Wanted year</param>
    /// <param name="weekNumber">Wanted week number</param>
    /// <returns>A <see cref="TimePeriod"/> object containing the start and end times of the week in UTC.</returns>
    public TimePeriod GetWeekStartAndEndUtc(int year, int weekNumber)
    {
        Debug.Log($"Fetching data for week {weekNumber} of {year}");

        // Get the first Monday of the year
        TimeZoneInfo localZone = TimeZoneInfo.Local;
        DateTime firstDay = new(year, 1, 1);
        DateTime firstMonday = firstDay.AddDays(DayOfWeek.Monday - firstDay.DayOfWeek);

        // Calculate the start and end times for the specified week
        DateTime startOfWeek = firstMonday.AddDays((weekNumber - 1) * 7);
        DateTime endOfWeek = startOfWeek.AddDays(7).AddTicks(-1);

        // Convert the start and end times to UTC
        DateTime startOfWeekUtc = TimeZoneInfo.ConvertTimeToUtc(startOfWeek, localZone);
        DateTime endOfWeekUtc = TimeZoneInfo.ConvertTimeToUtc(endOfWeek, localZone);

        return new TimePeriod { Start = startOfWeekUtc, End = endOfWeekUtc };
    }

    /// <summary>
    /// Asynchronously fetches data for a specified time period.
    /// </summary>
    /// <param name="range">The time period for which to fetch data.</param>
    /// <returns>A <see cref="JArray"/> containing the fetched data.</returns>
    public async Task<JArray> FetchDataRange(TimePeriod range)
    {
        // Format the UTC times as ISO 8601 strings with milliseconds and 'Z' to indicate UTC
        string start = range.Start.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
        string end = range.End.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");

        string url = $"api/events.json?deviceIds={deviceIds}&from={start}&to={end}&metrics=battery,co2,humidity,light,luminosity,motion,temperature&";
        
        // Log in, add the authorization header, and send the request
        await Login();
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        HttpResponseMessage response = await httpClient.GetAsync(httpClient.BaseAddress + url);

        if (response.IsSuccessStatusCode)
        {
            Debug.Log("Data fetch success");

            // Parse and return the JSON response
            string jsonString = await response.Content.ReadAsStringAsync();
            JArray json = JArray.Parse(jsonString);
            return json;
        }
        else
        {
            Debug.LogError($"Failed to fetch data: {response.StatusCode}");
            // Wait, log in again and try again
            await Task.Delay(10000);
            return await FetchDataRange(range);
        }
    }

    /// <summary>
    /// Aggregates sensor data into bins based on the specified time interval.
    /// Each bin represents a time span in minutes, and data within each bin is aggregated based on the variable.
    /// </summary>
    /// <param name="data">The data provided by the API as a <see cref="JArray"/>.</param>
    /// <param name="minutes">The wanted length of the time intervals of the bins in minutes.</param>
    /// <returns>A <see cref="JArray"/> containing the aggregated sensor data.</returns>
    public JArray AggregateData(JArray data, int minutes)
    {
        Dictionary<int, List<JObject>> bins = new();
        JArray aggregatedData = new();
        TimeSpan timeSpan = TimeSpan.FromMinutes(minutes);

        // Iterate over the data and sort it into bins based on the time property
        foreach (var item in data)
        {
            if (item is JObject obj)
            {
                string timeStr = obj["time"].ToString();
                DateTime time = DateTime.Parse(timeStr, CultureInfo.InvariantCulture);

                // Calculate bin index based on the binSize parameter
                int binIndex = (int)(time.TimeOfDay.TotalMinutes / timeSpan.TotalMinutes);

                // Initialize the bin if it doesn't exist
                if (!bins.ContainsKey(binIndex))
                {
                    bins[binIndex] = new List<JObject>();
                }

                // Add the object to the appropriate bin
                bins[binIndex].Add(obj);
            }
        }

        // Aggregate data within each bin
        foreach (var bin in bins)
        {
            JObject aggregatedObject = new JObject();
            int temperatureCount = 0;
            float temperatureSum = 0;
            float temperatureMax = float.MinValue;
            int co2Count = 0;
            float co2Sum = 0;
            float co2Max = float.MinValue;

            // Get the start time for the bin
            TimeSpan binStartTime = TimeSpan.FromMinutes(bin.Key * timeSpan.TotalMinutes);
            aggregatedObject["time"] = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, binStartTime.Hours, binStartTime.Minutes, 0).ToString("yyyy-MM-ddTHH:mm:ss");

            // Initialize fields
            aggregatedObject["motion"] = 0;

            // Aggregate other fields
            foreach (var obj in bin.Value)
            {
                foreach (var property in obj.Properties())
                {
                    // Ignore 'time' and 'deveui' properties
                    if (property.Name != "time" && property.Name != "deveui")
                    {
                        // Make sure the current property's value is not null
                        if (property.Value.Type != JTokenType.Null)
                        {
                            // Aggregate 'motion' by summing up its values
                            if (property.Name == "motion")
                            {
                                int valueToAdd = property.Value.ToObject<int>();
                                aggregatedObject[property.Name] = (int)aggregatedObject[property.Name] + valueToAdd;
                            }
                            else if (property.Name == "temperature")
                            {
                                // Increment the count and sum
                                temperatureCount++;
                                temperatureSum += property.Value.ToObject<float>();

                                // Track the maximum value
                                if (property.Value.ToObject<float>() > temperatureMax)
                                {
                                    temperatureMax = property.Value.ToObject<float>();
                                }
                            }
                            else if (property.Name == "co2")
                            {
                                // Increment the count and sum
                                co2Count++;
                                co2Sum += property.Value.ToObject<float>();

                                // Track the maximum value
                                if (property.Value.ToObject<float>() > co2Max)
                                {
                                    co2Max = property.Value.ToObject<float>();
                                }
                            }
                        }
                    }
                }
            }
            // Calculate and set the average values
            if (temperatureCount > 0)
            {
                float temperatureAverage = temperatureSum / temperatureCount;
                aggregatedObject["temperature"] = Math.Round(temperatureAverage, 1);
                aggregatedObject["temperatureMax"] = temperatureMax;
            }
            else
            {
                aggregatedObject["temperature"] = 0;
                aggregatedObject["temperatureMax"] = 0;
            }

            if (co2Count > 0)
            {
                float co2Average = co2Sum / co2Count;
                aggregatedObject["co2"] = Math.Round(co2Average, 1);
                aggregatedObject["co2Max"] = co2Max;
            }
            else
            {
                aggregatedObject["co2"] = 0;
                aggregatedObject["co2Max"] = 0;
            }

            // Add the aggregated object to the final data array
            aggregatedData.Add(aggregatedObject);
        }
        return aggregatedData;
    }
}

public class TimePeriod
{
    public DateTime Start { get; set; }
    public DateTime End { get; set; }
}
