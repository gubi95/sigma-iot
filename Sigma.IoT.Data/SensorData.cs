using System;

namespace Sigma.IoT.Data
{
    public sealed class SensorData
    {
        public SensorData(DateTime dateTime, int value, SensorType sensorType)
        {
            DateTime = dateTime;
            Value = value;
            SensorType = sensorType;
        }

        public DateTime DateTime { get; }

        public int Value { get; }

        public SensorType SensorType { get; }
    }
}
