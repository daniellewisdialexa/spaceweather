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
- run in debug mode
- Open postman or alike tool


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

## Group Controller 
- api/{endpoint}/group  - Group data by specific property

## Filter Controller 
- api/{endpoint}/filter - Filter data by specific property

## Correlation Controller
- api/report/sametime  - Get report of FLR and CME event data that happened nearly at the same time
- api/report/scottplot  - Get a visual of the CME and FLR data on a scatter plot that maps the intensity of the FLR to the speed of the CME, displays in html


