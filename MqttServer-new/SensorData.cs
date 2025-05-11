using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MqttBroker
{
    public class SensorData
    {
        public DateTime Timestamp { get; set; }
        public double Value { get; set; }
    }
}
