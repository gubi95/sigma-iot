using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Moq;
using Sigma.IoT.Data;
using Sigma.IoT.Tests.Shared;
using Xunit;

namespace Sigma.IoT.Tests.Data
{
    public sealed class FileDataProviderTests
    {
        [InlineData("humidity", SensorType.Humidity)]
        [InlineData("rainfall", SensorType.Rainfall)]
        [InlineData("temperature", SensorType.Temperature)]
        [Theory]
        public async Task Provider_returns_correct_data_when_historical_file_does_not_exist(string sensorName, SensorType sensorType)
        {
            var filePath = DefaultMocks.FilePathBuilder.Build("testDevice", sensorName, "2019-01-01.csv");
            var fileContent = new StringBuilder();
            fileContent.AppendLine("\"2020-12-24T13:45:10\",\"10\"");
            fileContent.AppendLine("\"2021-01-10T09:58:45\",\"20\"");
            fileContent.AppendLine("\"1995-03-27T23:01:32\",\"30\"");
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            writer.Write(fileContent);
            writer.Flush();
            stream.Position = 0;

            var fileProviderMock = new Mock<IFileProvider>();
            fileProviderMock
                .Setup(x => x.ListAllFiles(DefaultMocks.FilePathBuilder.Build("testDevice", sensorName)))
                .Returns(new List<string> { "2019-01-01.csv" });
            fileProviderMock.Setup(x => x.GetFileAsync(filePath)).Returns((() => Task.FromResult<Stream>(stream)));

            var fileConverterMock = new Mock<IFileToDataConverter<Stream, IEnumerable<UnitData>>>();
            fileConverterMock.Setup(x => x.GetFileExtension()).Returns(".csv");
            fileConverterMock.Setup(x => x.Convert(stream)).Returns(
                new List<UnitData>
                {
                    new UnitData(new DateTime(2020,12,24, 13,45,10), 10),
                    new UnitData(new DateTime(2021,01,10, 09,58,45), 20),
                    new UnitData(new DateTime(1995,03,27, 23,01,32), 30)
                });

            var fileDataProvider = new FileDataProvider(fileProviderMock.Object, DefaultMocks.FilePathBuilder, fileConverterMock.Object);
            var data = fileDataProvider.GetDataAsync("testDevice", sensorType);

            var actualResult = new List<UnitData>();
            await foreach (var chunk in data)
            {
                actualResult.AddRange(chunk);
            }

            var expectedResult = new List<UnitData>
            {
                new UnitData(new DateTime(2020, 12, 24, 13, 45, 10), 10),
                new UnitData(new DateTime(2021, 01, 10, 09, 58, 45), 20),
                new UnitData(new DateTime(1995, 03, 27, 23, 01, 32), 30)
            };

            fileProviderMock.Verify(x => x.ListAllFiles(DefaultMocks.FilePathBuilder.Build("testDevice", sensorName)), Times.Once);
            fileProviderMock.Verify(x => x.GetFileAsync(filePath), Times.Once);
            fileConverterMock.Verify(x => x.GetFileExtension(), Times.Exactly(3));
            fileConverterMock.Verify(x => x.Convert(stream), Times.Once);
            Assert.Equal(expectedResult, actualResult);
        }
    }
}
