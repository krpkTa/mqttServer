using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MqttBroker
{
    public class DeviceData
    {
        public string UID { get; set; } = "";
        public Dictionary<string, List<SensorData>> Sensors { get; set; } = new();
    }
}
