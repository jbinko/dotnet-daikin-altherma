using DotNet.Daikin.Altherma;

using var da = new DaikinAltherma();
await da.ConnectAsync("myDaikinHost");

for (; ; )
{
    var info = await da.GetDeviceInfoAsync();
    Console.WriteLine($"Adapter Model:             {info.AdapterModel}");
    Console.WriteLine($"Outdoor Temperature:       {info.OutdoorTemperature}");
    Console.WriteLine($"Indoor Temperature:        {info.IndoorTemperature}");
    Console.WriteLine($"Leaving Water Temperature: {info.LeavingWaterTemperature}");
    Console.WriteLine($"Target Temperature:        {info.TargetTemperature}");
    Console.WriteLine($"Power State:               {info.PowerState}");
    //await da.SetHeatingAsync(PowerState.On);
    //await da.SetTargetTemperatureAsync(23);

    System.Threading.Thread.Sleep(30 * 1000); // Report each 30s
}
