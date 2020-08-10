using System.Collections.Generic;

namespace Sigma.IoT.Data
{
    public interface IDataProvider
    {
        IAsyncEnumerable<IEnumerable<UnitData>> GetDataAsync(string deviceName, SensorType sensorType);

        IReadOnlyCollection<string> GetAllDevices();
    }
}
