using Newtonsoft.Json.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ApiManagerTest : MonoBehaviour
{
    private ApiManager apiManager;
    // Start is called before the first frame update
    async void Start()
    {
        apiManager = FindAnyObjectByType<ApiManager>();
        TimePeriod period = apiManager.GetWeekStartAndEndUtc(2024, 40);
        JArray data = await apiManager.FetchDataRange(period);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
