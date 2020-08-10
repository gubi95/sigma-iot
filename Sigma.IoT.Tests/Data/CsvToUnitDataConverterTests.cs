using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Sigma.IoT.Data;
using Xunit;

namespace Sigma.IoT.Tests.Data
{
    public sealed class CsvToUnitDataConverterTests
    {
        [Fact]
        public void Converter_has_correct_extension() =>
            Assert.Equal(".csv", new CsvToUnitDataConverter().GetFileExtension());

        [Fact]
        public void Converter_correctly_coverts_csv_content()
        {
            var csvContent = new StringBuilder();
            csvContent.AppendLine("\"2020-12-24T13:45:10\",\"10\"");
            csvContent.AppendLine("\"2021-01-10T09:58:45\",\"20\"");
            csvContent.AppendLine("\"1995-03-27T23:01:32\",\"30\"");

            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            writer.Write(csvContent);
            writer.Flush();
            stream.Position = 0;

            var converter = new CsvToUnitDataConverter();
            var actualResult = converter.Convert(stream);

            var expectedResult = new List<UnitData>
            {
                new UnitData(new DateTime(2020,12,24, 13,45,10), 10),
                new UnitData(new DateTime(2021,01,10, 09,58,45), 20),
                new UnitData(new DateTime(1995,03,27, 23,01,32), 30)
            };

            Assert.Equal(expectedResult, actualResult);
        }

        [Fact]
        public void Converter_skips_not_well_formatted_date_csv_record()
        {
            var csvContent = new StringBuilder();
            csvContent.AppendLine("\"2020-12-24T13:45:10\",\"10\"");
            csvContent.AppendLine("\"this is wrong date\",\"20\"");
            csvContent.AppendLine("\"1995-03-27T23:01:32\",\"30\"");

            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            writer.Write(csvContent);
            writer.Flush();
            stream.Position = 0;

            var converter = new CsvToUnitDataConverter();
            var actualResult = converter.Convert(stream);

            var expectedResult = new List<UnitData>
            {
                new UnitData(new DateTime(2020,12,24, 13,45,10), 10),
                new UnitData(new DateTime(1995,03,27, 23,01,32), 30)
            };

            Assert.Equal(expectedResult, actualResult);
        }

        [Fact]
        public void Converter_skips_not_well_formatted_value_record()
        {
            var csvContent = new StringBuilder();
            csvContent.AppendLine("\"2020-12-24T13:45:10\",\"10\"");
            csvContent.AppendLine("\"2021-01-10T09:58:45\",\"2x\"");
            csvContent.AppendLine("\"1995-03-27T23:01:32\",\"30\"");

            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            writer.Write(csvContent);
            writer.Flush();
            stream.Position = 0;

            var converter = new CsvToUnitDataConverter();
            var actualResult = converter.Convert(stream);

            var expectedResult = new List<UnitData>
            {
                new UnitData(new DateTime(2020,12,24, 13,45,10), 10),
                new UnitData(new DateTime(1995,03,27, 23,01,32), 30)
            };

            Assert.Equal(expectedResult, actualResult);
        }
    }
}
