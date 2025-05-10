using MQTTnet.Server;
using MQTTnet;
using System.Text;
using System.Net;

namespace MqttBroker
{
    public class Server
    {
        private readonly MqttServer _mqttServer;
        private readonly MqttServerOptions _serverOptions;

        public Server()
        {
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

        private Task OnMessageReceived(InterceptingPublishEventArgs args)
        {
            var payload = Encoding.UTF8.GetString(args.ApplicationMessage.Payload);
            Console.WriteLine($"Message: [Topic]: { args.ApplicationMessage.Topic}] [Payload: {payload}]");
            return Task.CompletedTask;
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
