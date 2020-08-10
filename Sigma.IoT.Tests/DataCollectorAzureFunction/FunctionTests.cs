using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Sigma.IoT.Data;
using Sigma.IoT.DataCollectorAzureFunction;
using Xunit;

namespace Sigma.IoT.Tests.DataCollectorAzureFunction
{
    public sealed class FunctionTests
    {
        [Fact]
        public void Function_cannot_be_created_without_cache_service() =>
            Assert.Throws<ArgumentNullException>(() =>
                new Function(null, new Mock<IFileToDataConverter<Stream, IEnumerable<UnitData>>>().Object));

        [Fact]
        public void Function_cannot_be_created_without_file_converter() =>
            Assert.Throws<ArgumentNullException>(() =>
                new Function(new Mock<ICacheService>().Object, null));

        [InlineData("humidity", SensorType.Humidity)]
        [InlineData("rainfall", SensorType.Rainfall)]
        [InlineData("temperature", SensorType.Temperature)]
        [Theory]
        public async Task Function_updates_data_for_each_sensor_name(string sensorName, SensorType sensorType)
        {
            var fileContent = new StringBuilder();
            fileContent.AppendLine("\"2020-12-24T13:45:10\",\"10\"");
            fileContent.AppendLine("\"2021-01-10T09:58:45\",\"20\"");
            fileContent.AppendLine("\"1995-03-27T23:01:32\",\"30\"");
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            writer.Write(fileContent);
            writer.Flush();
            stream.Position = 0;

            var cacheServiceMock = new Mock<ICacheService>();
            var fileConverterMock = new Mock<IFileToDataConverter<Stream, IEnumerable<UnitData>>>();
            var loggerMock = new Mock<ILogger>();

            var data = new List<UnitData>
            {
                new UnitData(new DateTime(2020, 12, 24, 13, 45, 10), 10),
                new UnitData(new DateTime(2021, 01, 10, 09, 58, 45), 20),
                new UnitData(new DateTime(1995, 03, 27, 23, 01, 32), 30)
            };

            fileConverterMock.Setup(x => x.Convert(stream)).Returns(data);

            const string deviceName = "TestDevice";
            const string filename = "2020-04-20.csv";

            await new Function(cacheServiceMock.Object, fileConverterMock.Object)
                .Run(stream, deviceName, sensorName, filename, loggerMock.Object).ConfigureAwait(false);

            fileConverterMock.Verify(x => x.Convert(stream), Times.Once);
            cacheServiceMock.Verify(x => x.SaveDataAsync(deviceName, sensorType, data), Times.Once);
        }
    }
}
