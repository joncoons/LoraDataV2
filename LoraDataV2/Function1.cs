using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Azure.EventHubs;
using Microsoft.Azure.WebJobs.ServiceBus;
using System.Text;
using System.Net.Http;
using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Microsoft.Extensions.Configuration;


namespace LoraDecode
{
    public static class LoRAConversion
    {
        private static HttpClient client = new HttpClient();

        [FunctionName("LoRAConversion")]
        public static void Run([EventHubTrigger("%eventHubName1%", Connection = "eventHubStr1", ConsumerGroup = "%consumerGroup1%")]EventData message,
            [EventHub("%eventHubName2%", Connection = "eventHubStr2")] IAsyncCollector<string> outputEventHubMessages,
            TraceWriter log, ExecutionContext context)
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(context.FunctionAppDirectory)
                .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables()
                .Build();

            string loraMsg = Encoding.UTF8.GetString(message.Body.Array); //convert IoT Hub message to string
            dynamic jsonLora = JObject.Parse(loraMsg); //parse JSON
            string loraDataEncoded = jsonLora["data"]; //create string for Base64 encoded message
            var loraDataBytes = System.Convert.FromBase64String(loraDataEncoded); //convert Base64 to bytes
            var loraDataHex = BitConverter.ToString(loraDataBytes); //convert bytes to hex
            loraDataHex = loraDataHex.Replace("-", ""); //remove "-" from hex string
            int loraSensor = Int32.Parse(loraDataHex, System.Globalization.NumberStyles.HexNumber) & 0xFFFF; //parse hex into integer value; get sensor value
           
            
      
            log.Info($"Lora data:{loraDataHex}");

            var telemetryData = new
            {
                loraID = jsonLora["deveui"],
                sensorData = loraSensor,
                timestamp = jsonLora["time"]
            };
            var messageString = JsonConvert.SerializeObject(telemetryData);
            outputEventHubMessages.AddAsync(messageString);

            log.Info($"**Message Sent to EventHub**:{messageString}");

            outputEventHubMessages.FlushAsync();
        }


    }
}
