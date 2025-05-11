using MQTTnet.Server;
using MQTTnet;
using System.Text;
using System.Net;
using System.Text.Json;
using MQTTnet.Protocol;

namespace MqttBroker
{
    public class Server
    {
        private readonly MqttServer _mqttServer;
        private readonly MqttServerOptions _serverOptions;
        private readonly string _dataDirectory = "DeviceData";

        public Server()
        {
            Directory.CreateDirectory(_dataDirectory);

            _serverOptions = new MqttServerOptionsBuilder()
        .WithDefaultEndpoint()
        .WithDefaultEndpointPort(1883)
        .WithDefaultEndpointBoundIPAddress(IPAddress.Any)
        .Build();

            var factory = new MqttServerFactory();
            _mqttServer = factory.CreateMqttServer(_serverOptions);

            _mqttServer.InterceptingPublishAsync += OnMessageReceived;
            _mqttServer.ClientConnectedAsync += OnClientConnected;
            _mqttServer.ClientDisconnectedAsync += OnClientDisconnected;
        }

        private async Task OnMessageReceived(InterceptingPublishEventArgs args)
        {
            try
            {
                var topic = args.ApplicationMessage.Topic;
                var payload = Encoding.UTF8.GetString(args.ApplicationMessage.Payload);
                Console.WriteLine($"Message received: [Topic: {topic}] [Payload: {payload}]");

                // Обработка запроса исторических данных
                if (topic.EndsWith("/history/get"))
                {
                    string uid = topic.Split('/')[0];
                    await SendDeviceHistoryFile(uid, args.ClientId);
                    return;
                }

                // Обработка данных сенсоров
                var topicParts = topic.Split('/');
                if (topicParts.Length >= 2 && (topicParts[1] == "T" || topicParts[1] == "H" || topicParts[1] == "P"))
                {
                    string uid = topicParts[0];
                    string sensorType = topicParts[1];
                    await SaveSensorData(uid, sensorType, payload);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in OnMessageReceived: {ex.Message}");
            }
        }

        private async Task SendDeviceHistoryFile(string uid, string clientId)
        {
            string filePath = Path.Combine(_dataDirectory, $"{uid}.json");
            if (!File.Exists(filePath)) return;

            try
            {
                string jsonData = await File.ReadAllTextAsync(filePath);
                var message = new MqttApplicationMessageBuilder()
                    .WithTopic($"devices/{uid}/history/data")
                    .WithPayload(jsonData)
                    .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
                    .Build();


                
                    await _mqttServer.InjectApplicationMessage(
                        new InjectedMqttApplicationMessage(message));
                
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending history: {ex.Message}");
            }
        }


        private async Task SaveSensorData(string uid, string sensorType, string value)
        {
            string filePath = Path.Combine(_dataDirectory, $"{uid}.json");
            DeviceData deviceData;

            // Если файл существует - загружаем, иначе создаем новый
            if (File.Exists(filePath))
            {
                string json = await File.ReadAllTextAsync(filePath);
                deviceData = JsonSerializer.Deserialize<DeviceData>(json) ?? new DeviceData { UID = uid };
            }
            else
            {
                deviceData = new DeviceData { UID = uid };
            }

            // Добавляем новое значение
            var sensorData = new SensorData
            {
                Timestamp = DateTime.UtcNow,
                Value = double.TryParse(value, out double num) ? num : 0
            };

            if (!deviceData.Sensors.ContainsKey(sensorType))
            {
                deviceData.Sensors[sensorType] = new List<SensorData>();
            }

            deviceData.Sensors[sensorType].Add(sensorData);

            // Сохраняем обратно в файл
            string updatedJson = JsonSerializer.Serialize(deviceData, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(filePath, updatedJson);
        }

        private Task OnClientConnected(ClientConnectedEventArgs args)
        {
            Console.WriteLine($"Client connected: ID={args.ClientId}");

            return Task.CompletedTask;
        }

        private Task OnClientDisconnected(ClientDisconnectedEventArgs args)
        {
            Console.WriteLine($"Client Disconnected: {args.ClientId}");

            return Task.CompletedTask;
        }

        public async Task Start()
        {
            await _mqttServer.StartAsync();
            Console.WriteLine("MQTT Broker started:");
            Console.WriteLine($"- TCP: 1883 (DuckDNS: ваш_домен.duckdns.org:1883)");
            Console.WriteLine($"- SSL: 8883 (DuckDNS: ваш_домен.duckdns.org:8883)");
            Console.WriteLine($"Local IPs: {string.Join(", ", GetLocalIPs())}");

        }
        private List<string> GetLocalIPs()
        {
            return Dns.GetHostAddresses(Dns.GetHostName())
                .Where(ip => ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                .Select(ip => ip.ToString())
                .ToList();
        }

        public async Task Stop()
        {
            await _mqttServer.StopAsync();
            Console.WriteLine("Mqtt broker stopped");
        }
    }
}
