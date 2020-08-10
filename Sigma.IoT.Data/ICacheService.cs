using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Sigma.IoT.Data
{
    public interface ICacheService
    {
        Task<IReadOnlyCollection<SensorData>> GetDataAsync(string deviceName, DateTime date, SensorType? sensorType = null);

        Task SaveDataAsync(string deviceName, SensorType sensorType, IReadOnlyCollection<UnitData> sensorsData);
    }
}
