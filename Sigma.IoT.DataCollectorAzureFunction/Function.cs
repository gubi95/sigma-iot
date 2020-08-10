using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Sigma.IoT.Data;

namespace Sigma.IoT.DataCollectorAzureFunction
{
    public class Function
    {
        private readonly ICacheService _cacheService;
        private readonly IFileToDataConverter<Stream, IEnumerable<UnitData>> _fileToDataConverter;

        public Function(
            ICacheService cacheService, IFileToDataConverter<Stream,
            IEnumerable<UnitData>> fileToDataConverter)
        {
            _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
            _fileToDataConverter = fileToDataConverter ?? throw new ArgumentNullException(nameof(fileToDataConverter));
        }

        [FunctionName("Sigma_IoT_DataCollector")]
        public async Task Run(
            [BlobTrigger("iot/{deviceName}/{sensorName}/{fileName}.csv", Connection = "AzureWebJobsStorage")] Stream blob,
            string deviceName,
            string sensorName,
            string fileName,
            ILogger logger)
        {
            logger.LogInformation($"Function starts for: {deviceName}/{sensorName}/{fileName}");

            try
            {
                var data = _fileToDataConverter.Convert(blob).ToList();
                var sensorType = GetSensorType(sensorName);

                if (data.Any())
                {
                    await _cacheService.SaveDataAsync(deviceName, sensorType, data).ConfigureAwait(false);
                }
            }
            catch (Exception e)
            {
                logger.LogError(e, "Error occurred while saving data");
            }

            logger.LogInformation($"Function ends for: {deviceName}/{sensorName}/{fileName}");
        }

        private static SensorType GetSensorType(string sensorName) =>
            sensorName.ToLowerInvariant() switch
            {
                "humidity" => SensorType.Humidity,
                "rainfall" => SensorType.Rainfall,
                "temperature" => SensorType.Temperature,
                _ => throw new ArgumentException($"Argument {nameof(sensorName)} has incorrect value")
            };

    }
}
