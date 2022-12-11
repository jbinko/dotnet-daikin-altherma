using System.Text;
using System.Net.WebSockets;
using System.Text.Json.Nodes;

namespace DotNet.Daikin.Altherma
{
    public enum PowerState
    {
        On,
        Standby
    }

    public record DeviceInfo(string AdapterModel, float IndoorTemperature,
        float OutdoorTemperature, float LeavingWaterTemperature,
        float TargetTemperature, PowerState PowerState);

    public sealed class DaikinAltherma : IDisposable
    {
        public DaikinAltherma()
        {
            _ws = new ClientWebSocket();
        }

        public void Dispose() => _ws.Dispose();

        public async Task ConnectAsync(string hostname)
        {
            if (Uri.CheckHostName(hostname) == UriHostNameType.Unknown)
                throw new InvalidOperationException($"NOT valid Hostname or IP address was specified: '{hostname}'.");

            await _ws.ConnectAsync(new Uri($"ws://{hostname}/mca"), CancellationToken.None);
        }

        public async Task<DeviceInfo> GetDeviceInfoAsync()
        {
            var adapterModel = await RequestValueAsync<string>(
                "MNCSE-node/deviceInfo", "/m2m:rsp/pc/m2m:dvi/mod");
            var indoorTemperature = await RequestValueHPAsync<float>(
                "1/Sensor/IndoorTemperature/la", "/m2m:rsp/pc/m2m:cin/con");
            var outdoorTemperature = await RequestValueHPAsync<float>(
                "1/Sensor/OutdoorTemperature/la", "/m2m:rsp/pc/m2m:cin/con");
            var leavingWaterTemperature = await RequestValueHPAsync<float>(
                "1/Sensor/LeavingWaterTemperatureCurrent/la", "/m2m:rsp/pc/m2m:cin/con");
            var targetTemperature = await RequestValueHPAsync<float>(
                "1/Operation/TargetTemperature/la", "/m2m:rsp/pc/m2m:cin/con");
            var powerState = await RequestValueHPAsync<string>(
                "1/Operation/Power/la", "/m2m:rsp/pc/m2m:cin/con");

            return new DeviceInfo(adapterModel, indoorTemperature,
                outdoorTemperature, leavingWaterTemperature,
                targetTemperature, ParsePowerState(powerState));
        }

        public async Task SetTargetTemperatureAsync(int targetTemperature)
        {
            if (targetTemperature < 16 || targetTemperature > 30)
                throw new InvalidOperationException(
                    $"Target temperature value must be between 16-30. Provided value: '{targetTemperature}'.");

            var payload = new JsonObject
            {
                ["con"] = targetTemperature,
                ["cnf"] = "text/plain:0",
            };

            await RequestValueHPAsync<string>("1/Operation/TargetTemperature", "/m2m:rsp/rqi", payload);
        }

        public async Task SetHeatingAsync(PowerState powerState)
        {
            var payload = new JsonObject
            {
                ["con"] = powerState == PowerState.On ? "on" : "standby",
                ["cnf"] = "text/plain:0",
            };

            await RequestValueHPAsync<string>("1/Operation/Power", "/m2m:rsp/rqi", payload);
        }

        private async Task<T> RequestValueHPAsync<T>(string item, string outputPath, JsonObject? payload = null)
        {
            return await RequestValueAsync<T>($"MNAE/{item}", outputPath, payload);
        }

        private async Task<T> RequestValueAsync<T>(string item, string outputPath, JsonObject? payload = null)
        {
            if (_ws.State != WebSocketState.Open)
                throw new InvalidOperationException(
                    $"Web Socket State is in unexpected state: '{_ws.State}'. Expected: '{WebSocketState.Open}'.");

            var reqid = Guid.NewGuid().ToString("D").Substring(0, 5);
            var json = CreateJsonRequest(reqid, item, payload);

            //Console.WriteLine(json);
            await SendJsonAsync(json);
            var result = await ReceiveJsonObjectAsync();
            //Console.WriteLine(result.ToString());

            var returnedReqid = JsonGetValue<string>(result, "/m2m:rsp/rqi");
            if (returnedReqid != reqid)
                throw new InvalidOperationException(
                    $"Returned request ID: '{returnedReqid}' doesn't match. Expected: '{reqid}'.");

            var returnedTo = JsonGetValue<string>(result, "/m2m:rsp/to");
            if (returnedTo != DaikinAltherma.UserAgent)
                throw new InvalidOperationException(
                    $"Returned TO: '{returnedTo}' doesn't match. Expected: '{DaikinAltherma.UserAgent}'.");

            return JsonGetValue<T>(result, outputPath);
        }

        private string CreateJsonRequest(string reqid, string item, JsonObject? payload)
        {
            var requestObject = new JsonObject
            {
                ["m2m:rqp"] = new JsonObject
                {
                    ["fr"] = UserAgent,
                    ["rqi"] = reqid,
                    ["op"] = 2,
                    ["to"] = $"/[0]/{item}",
                }
            };

            if (payload != null)
            {
                var node = requestObject["m2m:rqp"]!;
                node["ty"] = 4;
                node["op"] = 1;
                node["pc"] = new JsonObject
                {
                    ["m2m:cin"] = payload,
                };
            }

            return requestObject.ToJsonString();
        }

        private async Task SendJsonAsync(string json)
        {
            await _ws.SendAsync(Encoding.UTF8.GetBytes(json), WebSocketMessageType.Text, true, CancellationToken.None);
        }

        private async Task<JsonNode> ReceiveJsonObjectAsync()
        {
            WebSocketReceiveResult result;
            var buffer = new ArraySegment<byte>(new Byte[256]);

            using var ms = new MemoryStream();
            do
            {
                result = await _ws.ReceiveAsync(buffer, CancellationToken.None);

                if (result.CloseStatus.HasValue && result.CloseStatus.Value != WebSocketCloseStatus.NormalClosure)
                    throw new InvalidOperationException(
                        $"Unexpected close status returned. Expected: '{WebSocketCloseStatus.NormalClosure}' but got: '{result.CloseStatus}'.");

                ms.Write(buffer.Array!, buffer.Offset, result.Count);
            }
            while (!result.EndOfMessage);

            ms.Seek(0, SeekOrigin.Begin);

            if (result.MessageType != WebSocketMessageType.Text)
                throw new InvalidOperationException(
                    $"Unexpected message type returned. Expected: '{WebSocketMessageType.Text}' but got: '{result.MessageType}'.");

            return JsonObject.Parse(ms)!;
        }

        private T JsonGetValue<T>(JsonNode node, string path)
        {
            if (node == null)
                throw new ArgumentNullException("Node argument is null.");
            if (String.IsNullOrWhiteSpace(path))
                throw new InvalidOperationException("Path argument is null or empty.");

            var items = path.Split('/');

            foreach (var item in items)
            {
                if (String.IsNullOrWhiteSpace(item))
                    continue;

                node = node[item]!;
            }

            return node.GetValue<T>(); // Caller is responsible for checking the value which can be null
        }

        private PowerState ParsePowerState(string powerState)
        {
            switch (powerState)
            {
                case "on":
                    return PowerState.On;
                case "standby":
                    return PowerState.Standby;
            }

            throw new InvalidOperationException(
                $"Unexpected power state: '{powerState}'.");
        }

        private readonly ClientWebSocket _ws;
        private static string UserAgent = "dotnet-daikin-altherma";
    }
}
