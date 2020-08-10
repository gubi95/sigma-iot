using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Sigma.IoT.Data
{
    public sealed class FileDataProvider : IDataProvider
    {
        private const string HistoricalFilename = "historical.zip";
        private const string DateFormat = "yyyy-MM-dd";
        private readonly IFileProvider _fileProvider;
        private readonly IFilePathBuilder _filePathBuilder;
        private readonly IFileToDataConverter<Stream, IEnumerable<UnitData>> _fileConverter;

        public FileDataProvider(
            IFileProvider fileProvider,
            IFilePathBuilder filePathBuilder,
            IFileToDataConverter<Stream, IEnumerable<UnitData>> fileConverter)
        {
            _fileProvider = fileProvider ?? throw new ArgumentNullException(nameof(fileProvider));
            _filePathBuilder = filePathBuilder ?? throw new ArgumentNullException(nameof(filePathBuilder));
            _fileConverter = fileConverter ?? throw new ArgumentNullException(nameof(fileConverter));
        }

        public async IAsyncEnumerable<IEnumerable<UnitData>> GetDataAsync(string deviceName, SensorType sensorType)
        {
            var sensorFiles = _fileProvider.ListAllFiles(BuildDirectory(deviceName, sensorType));

            foreach (var sensorFile in sensorFiles.Where(file => file.EndsWith(_fileConverter.GetFileExtension())))
            {
                await using var stream = await _fileProvider
                    .GetFileAsync(BuildFilePath(deviceName, sensorType, GetDateFromFilename(sensorFile), _fileConverter.GetFileExtension()))
                    .ConfigureAwait(false);

                yield return _fileConverter.Convert(stream);
            }

            if (sensorFiles.SingleOrDefault(file => file.Equals(HistoricalFilename)) != null)
            {
                var archivedFiles = _fileProvider.GetFilesFromZipArchive(BuildArchiveFilePath(deviceName, sensorType));

                await foreach (var archivedFile in archivedFiles.ConfigureAwait(false))
                {
                    var data = _fileConverter.Convert(archivedFile);

                    await archivedFile
                        .DisposeAsync()
                        .ConfigureAwait(false);

                    yield return data;
                }
            }
        }

        public IReadOnlyCollection<string> GetAllDevices() =>
            _fileProvider.ListAllFolders(string.Empty);

        private static string BuildFileName(DateTime date, string fileExtension) =>
            date.ToString(DateFormat, CultureInfo.InvariantCulture) + fileExtension;

        private string BuildFilePath(string deviceName, SensorType sensorType, DateTime date, string fileExtension) =>
            _filePathBuilder.Build(deviceName, GetSensorName(sensorType), BuildFileName(date, fileExtension));

        private string BuildArchiveFilePath(string deviceName, SensorType sensorType) =>
            _filePathBuilder.Build(BuildDirectory(deviceName, sensorType), HistoricalFilename);

        private string BuildDirectory(string deviceName, SensorType sensorType) =>
            _filePathBuilder.Build(deviceName, GetSensorName(sensorType));

        private static string GetSensorName(SensorType sensorType) =>
            sensorType switch
            {
                SensorType.Humidity => "humidity",
                SensorType.Rainfall => "rainfall",
                SensorType.Temperature => "temperature",
                _ => throw new ArgumentException($"Incorrect {nameof(sensorType)} argument value")
            };

        private DateTime GetDateFromFilename(string fileName) =>
            DateTime.ParseExact(fileName.Replace(_fileConverter.GetFileExtension(), string.Empty), DateFormat, CultureInfo.InvariantCulture);
    }
}
