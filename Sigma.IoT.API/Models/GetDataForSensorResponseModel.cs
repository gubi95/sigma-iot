using System.Collections.Generic;

namespace Sigma.IoT.API.Models
{
    public class GetDataForSensorResponseModel
    {
        public string Device { get; set; }

        public string Date { get; set; }

        public string Sensor { get; set; }

        public IEnumerable<UnitDataModel> Data { get; set; }
    }
}
