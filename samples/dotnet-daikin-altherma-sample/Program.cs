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
