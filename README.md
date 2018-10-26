
[SpatialTutorial](https://github.com/ptv-logistics/SpatialTutorial/wiki)
===============

A tutorial that shows some practices to visualize, analyze and manipulate spatial data with PTV xServer and Leaflet. Read the tutorial in the wiki https://github.com/ptv-logistics/SpatialTutorial/wiki

## New in Version 0.2
* Updated to Leaflet 1.0.3
* Updated to xMapServer-2
* Updated to .NET 4.6 
* Updated to System.Data.SQLite.Core (via NuGet)
* Updated to SpatiaLite 4.4.0-RC0
* Supporting 64-bit
* Fixed creation and usage of MBRCache
* Reducing polygon vertices to corresponding pixel accuracy
* Some beautifications

## ToDo
* Update the wiki

## Known Isues
There are some Leaflet issues which affect the map-display in the tutorial. These issues have already been reported:
* Latest Chrome introduced 1px tile borders to integer zooms https://github.com/Leaflet/Leaflet/issues/6101
