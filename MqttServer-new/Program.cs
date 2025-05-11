using MqttBroker;

var broker = new Server();
await broker.Start();

Console.WriteLine("Press Enter to stop...");
Console.ReadLine();

await broker.Stop();