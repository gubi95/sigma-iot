using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Sigma.IoT.Data;

namespace Sigma.IoT.DataCollector
{
    public class DataImporter
    {
        private const int DeviceEntriesPerBatch = 10;
        private readonly IDataProvider _dataProvider;
        private readonly ICacheService _cacheService;
        private readonly ILogger<DataImporter> _logger;

        public DataImporter(IDataProvider dataProvider, ICacheService cacheService, ILogger<DataImporter> logger)
        {
            _dataProvider = dataProvider ?? throw new ArgumentNullException(nameof(dataProvider));
            _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task RunAsync()
        {
            _logger.LogInformation($"Importer starts...");

            var devices = _dataProvider.GetAllDevices();

            _logger.LogInformation($"Available devices: {string.Join(",", devices)}");

            var entriesAdded = 0;
            var totalEntriesAdded = 0;

            foreach (var device in devices)
            {
                var dataToSave = new List<UnitData>();

                try
                {
                    foreach (var sensorType in Enum.GetValues(typeof(SensorType)).Cast<SensorType>())
                    {
                        _logger.LogInformation($"Fetching data for device: {device} and sensor: {sensorType}");

                        await foreach (var data in _dataProvider.GetDataAsync(device, sensorType).ConfigureAwait(false))
                        {
                            dataToSave.AddRange(data);
                            entriesAdded++;
                            totalEntriesAdded++;

                            if (entriesAdded == DeviceEntriesPerBatch)
                            {
                                await SaveData(device, sensorType, dataToSave, totalEntriesAdded).ConfigureAwait(false);
                                dataToSave.Clear();
                                entriesAdded = 0;
                            }
                        }

                        if (dataToSave.Any())
                        {
                            await SaveData(device, sensorType, dataToSave, totalEntriesAdded).ConfigureAwait(false);
                            dataToSave.Clear();
                            entriesAdded = 0;
                        }
                    }
                }
                catch (Exception e)
                {
                    _logger.LogError(e, $"Error occurred while saving data for device: {device}");
                }
            }

            _logger.LogInformation($"Importer ends...");
        }

        private async Task SaveData(string device, SensorType sensorType, List<UnitData> dataToSave, int totalSaved)
        {
            _logger.LogInformation($"Saving data for device: {device} and sensor: {sensorType}");

            await _cacheService.SaveDataAsync(device, sensorType, dataToSave).ConfigureAwait(false);

            _logger.LogInformation($"Total entries saved: {totalSaved}");
        }
    }
}
