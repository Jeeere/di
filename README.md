# di

Repository containing scripts related to my Master's thesis.

---

## Contents

Descriptions of the files in the repository.

---

### /unity_scripts

Scripts used in the Unity project.

|Script|Description|
| -------- | ------- |
|ApiManager|Methods for interacting with a third-party API and preprocessing the fetched data|
|BarChartUpdater|Implements the updating and controlling of the 3D bar chart for historical CO2 data|
|Co2Meter|Implements the continuous visualization of real-time CO2 data|
|RelativeTemperatureVisualizer|Implements the continuous visualization of the difference between real-time and desired temperatures|
|XRAnchorManager|Manages XR anchors on game objects|
|XRSwitcher|Implements switching between AR and VR scenes|

#### /unity_scripts/helper_scripts

|Script|Description|
| -------- | ------- |
|ApiManagerTest|Tests the api manager by fetching a week of data|
|DebugConsole|Script for adding debug log messages to an in-game text field|
|Evaluation|Tests for functional features used in the performance data collection process|
|MockData|Used for generating mock data for real time data visualization features|

---

### /data_analysis

|File|Description|
| -------- | ------- |
|analysis.ipynb|Jupyter file containing the program responsible for the analysis process|
|/input|Directory containing the performance measurement data|

---
