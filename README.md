# SpaceWeather
![.Net](https://img.shields.io/badge/.NET-5C2D91?style=for-the-badge&logo=.net&logoColor=white) ![Visual Studio](https://img.shields.io/badge/Visual%20Studio-5C2D91.svg?style=for-the-badge&logo=visual-studio&logoColor=white) ![C#](https://img.shields.io/badge/c%23-%23239120.svg?style=for-the-badge&logo=csharp&logoColor=white)
  ### Tech stack
  - C# 12
  - Net 8
  
  ### Dependencies 
  All can be installed via Nuget
  - Scottplot for scatter plots (https://scottplot.net/) 
  - newtonsoft for json parsing
    
## General Information 
This project uses publically available apis from the DONKI NASA api 
more information on this and other apis can be found here: https://api.nasa.gov/

Check the base controller for more information on specifics on parameters that are accepted
By default, if no starttime and endtime parameters are supplied then the data coming back will be 30 days worth 
Additionally a few shorthands can be used in the starttime parameter:
"today" will convert to the current date
"yr" + number, ex: yr1,  will subtract the current data by the number and provide data far back in the archive.  Meaning if you sent in a "yr1" then the past year's worth of data
will be provided back


## Setup

- Clone repo
- Get NASA api key (api.nasa.gov)
- Add in appsettings.json
- Build
- Run 'dotnet run' in the developer terminal
- Open postman or alike tool to hit endpoints

App settings json
```json
{
  "ConnectionStrings": {
    "DONKIBaseURL": "https://api.nasa.gov/DONKI/",
    "NOAABaseURL": "https://services.swpc.noaa.gov/"
  },

  "IdentitySettings": {
    "ApiKey": "{add api key here}"
  }
 }
```

## Endpoints
Currently, only http is configured
There are two supported data sources from the DONKI NASA API:
FLR - Solar flare
CME - Coronal mass ejection
{endpoint} will be seen around this can be FLR or CME 

- Base: http://localhost:{port}
## Starting point Controller
- /api/{endpoint} - Get all data
- /api/{endpoint}/count - count of a specific property
  
## Order Controller
- api/{endpoint}/order - Order data (ASC/DESC) by specific property

## Group Controller 4
- api/{endpoint}/group  - Group data by specific property

## Filter Controller 
- api/{endpoint}/filter - Filter data by specific property

## Correlation Controller
- api/report/sametime  - Get report of FLR and CME event data that happened nearly at the same time
- api/report/scottplot  - Get a visual of the CME and FLR data on a scatter plot that maps the intensity of the FLR to the speed of the CME, displays in html
- /api/report/flagged - Get back a list of events in text format that are interesting, High intensity flares with slow CMEs, Low Intensity flares with fast CMEs, Flares with no CMEs

# Data Examples & Resources 
 ## Links
 - https://ccmc.gsfc.nasa.gov/wsa-dashboard/ - Neat dashboard for solar data visuals 
 - https://ccmc.gsfc.nasa.gov/RoR_WWW/SWREDI/training-for-engineers/Flares_CMEs_SEP.pdf - Flares, CMEs and SEPs
 - https://www.swpc.noaa.gov/ - Space Weather Prediction Center
 - https://scied.ucar.edu/learning-zone/sun-space-weather/what-space-weather -What Is Space Weather and How Does It Affect the Earth?
 - https://celestrak.org/SpaceData/ - Some neat space data
 - https://kauai.ccmc.gsfc.nasa.gov/DONKI/ - Home page the DONKI Api
 - https://www.weather.gov/media/nws/Results-of-the-First-National-Survey-of-User-Needs-for-Space-Weather-2024.pdf  - In dept report of needs for space weather data


## Data examples

Below is a excerpt from the Same time report (report/sametime) that helps correlate high speed CMEs to high intensity FLRs 
   ```json
  [
  {
    "flareID": "2024-08-31T00:38:00-FLR-001",
    "flareClassType": "M1.1",
    "flareBeginTime": "2024-08-31T00:38:00Z",
    "flarePeakTime": "2024-08-31T00:49:00Z",
    "flareRiseDuration": 11,
    "flareLink": "https://webtools.ccmc.gsfc.nasa.gov/DONKI/view/FLR/33070/-1",
    "cmeid": "2024-08-31T00:36:00-CME-001",
    "cmeStartTime": "2024-08-31T00:36:00Z",
    "timeDifferenceMins": -2,
    "cmeAnalyses": [
      {
        "isMostAccurate": true,
        "type": "S",
        "speed": 386,
        "latitude": 30,
        "longitude": -43,
        "halfAngle": 17,
        "link": "https://webtools.ccmc.gsfc.nasa.gov/DONKI/view/CMEAnalysis/33082/-1"
      }
    ]
  },
  {
    "flareID": "2024-08-31T00:38:00-FLR-001",
    "flareClassType": "M1.1",
    "flareBeginTime": "2024-08-31T00:38:00Z",
    "flarePeakTime": "2024-08-31T00:49:00Z",
    "flareRiseDuration": 11,
    "flareLink": "https://webtools.ccmc.gsfc.nasa.gov/DONKI/view/FLR/33070/-1",
    "cmeid": "2024-08-31T00:53:00-CME-001",
    "cmeStartTime": "2024-08-31T00:53:00Z",
    "timeDifferenceMins": 15,
    "cmeAnalyses": [
      {
        "isMostAccurate": true,
        "type": "S",
        "speed": 419,
        "latitude": 26,
        "longitude": 102,
        "halfAngle": 24,
        "link": "https://webtools.ccmc.gsfc.nasa.gov/DONKI/view/CMEAnalysis/33078/-1"
      }
    ]
  },
   ```
This image comes from the scatter plot report (report/scottplot)  

![image](https://github.com/user-attachments/assets/7c93ee2e-eea0-43e3-a72f-a02d95a489f7)


Interesting events report example
![image](https://github.com/user-attachments/assets/6fec70ba-6db8-4619-b85b-190c5c9ed133)


