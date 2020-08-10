using System.Collections.Generic;

namespace Sigma.IoT.API.Models
{
    public class GetDataForAllSensorsResponseModel
    {
        public string Device { get; set; }

        public string Date { get; set; }

        public IDictionary<string, IEnumerable<UnitDataModel>> SensorData { get; set; }
    }
}
