using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using AutoMapper;
using Sigma.IoT.API.Models;
using Sigma.IoT.Data;

namespace Sigma.IoT.API
{
    public class AutoMapping : Profile
    {
        public AutoMapping()
        {
            CreateMap<IEnumerable<SensorData>, GetDataForSensorResponseModel>()
                .ForMember(x => x.Data, x => x.MapFrom(data => 
                    data.Select(sensorData => new UnitDataModel
                    {
                        Value = sensorData.Value,
                        Time = sensorData.DateTime.ToString("HH:mm:ss", CultureInfo.InvariantCulture)
                    })));

            CreateMap<IEnumerable<SensorData>, GetDataForAllSensorsResponseModel>()
                .ForMember(x => x.SensorData, x => x.MapFrom(data =>
                    data.GroupBy(d => d.SensorType).ToDictionary(
                        group => GetSensorName(group.Key),
                        group => group.Select(sensorData => new UnitDataModel
                        {
                            Value = sensorData.Value,
                            Time = sensorData.DateTime.ToString("HH:mm:ss", CultureInfo.InvariantCulture)
                        }))));
        }

        private static string GetSensorName(SensorType sensorType) =>
            sensorType switch
            {
                SensorType.Humidity => "humidity",
                SensorType.Rainfall => "rainfall",
                SensorType.Temperature => "temperature",
                _ => throw new ArgumentException($"Argument {nameof(sensorType)} is incorrect")
            };
    }
}
