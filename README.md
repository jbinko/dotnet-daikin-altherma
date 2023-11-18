# dotnet-daikin-altherma

This project is C# library port which can talk to Daikin Altherma
via LAN adapter BRP069A61 or BRP069A62.

## NuGet

You can add package with command:
```
dotnet add package dotnet-daikin-altherma
```

## Example

See example below, and change "myDaikinHost" to your Daikin host name or IP address.

```csharp
using DotNet.Daikin.Altherma;

using var da = new DaikinAltherma();
await da.ConnectAsync("myDaikinHost");

var networkInfo = await da.GetNetworkInfoAsync();

Console.WriteLine($"IPAddress:  {networkInfo.IPAddress}");
Console.WriteLine($"Subnet:     {networkInfo.Subnet}");
Console.WriteLine($"GW:         {networkInfo.GW}");
Console.WriteLine($"DNS:        {String.Join(",", networkInfo.Dns)}");
Console.WriteLine($"DHCP:       {networkInfo.Dhcp}");
Console.WriteLine($"MacAddress: {networkInfo.MacAddress}");
Console.WriteLine();

for (; ; )
{
    var info = await da.GetDeviceInfoAsync();
    Console.WriteLine($"Adapter Model:             {info.AdapterModel}");
    Console.WriteLine($"Outdoor Temperature:       {info.OutdoorTemperature}");
    Console.WriteLine($"Indoor Temperature:        {info.IndoorTemperature}");
    Console.WriteLine($"Leaving Water Temperature: {info.LeavingWaterTemperature}");
    Console.WriteLine($"Target Temperature:        {info.TargetTemperature}");
    Console.WriteLine($"Target Temperature Offset: {info.TargetTemperatureOffset}");
    Console.WriteLine($"Power State:               {info.PowerState}");
    Console.WriteLine($"Emergency State:           {info.EmergencyState}");
    Console.WriteLine($"Error State:               {info.ErrorState}");
    Console.WriteLine($"Warning State:             {info.WarningState}");
    //var ok = await da.SetHeatingAsync(PowerState.On);
    //var ok = await da.SetTargetTemperatureAsync(23);
    //var ok = await da.SetTargetTemperatureOffsetAsync(-1);

    Console.WriteLine($"------------------------------------");
    System.Threading.Thread.Sleep(30 * 1000); // Report each 30s
}
```

## Releases

- Version Next
  - New method DisconnectAsync to complement exisitng method ConnectAsync
- Version 2.0.0
  - Adds support for two modes (Configure with Room Temperature and Configure with Outside Temperature)
  - Error handling for action methods: SetHeatingAsync, SetTargetTemperatureAsync, SetTargetTemperatureOffsetAsync
  - TargetTemperature and TargetTemperatureOffset can be null. Depends on device mode.
  - New properties:
    - TargetTemperatureOffset
    - EmergencyState
    - ErrorState
    - WarningState
  - New Methods:
    - GetNetworkInfoAsync
    - SetTargetTemperatureOffsetAsync
- Version 1.0.0 - Inital Version

## Notes

This module has been successfully tested with following unites:

- Daikin HVAC controller BRP069A62

## Acknowledgments

Inspirational project is hosted here:
[https://github.com/Frankkkkk/python-daikin-altherma](https://github.com/Frankkkkk/python-daikin-altherma)
