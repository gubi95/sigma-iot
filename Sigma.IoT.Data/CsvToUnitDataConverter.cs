using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using CsvHelper;
using CsvHelper.Configuration.Attributes;

namespace Sigma.IoT.Data
{
    public sealed class CsvToUnitDataConverter : IFileToDataConverter<Stream, IEnumerable<UnitData>>
    {
        public string GetFileExtension() => ".csv";

        public IEnumerable<UnitData> Convert(Stream file)
        {
            using var csvReader = new CsvReader(new StreamReader(file), CultureInfo.InvariantCulture);
            csvReader.Configuration.HasHeaderRecord = false;
            csvReader.Configuration.MissingFieldFound = null;
            var records = csvReader.GetRecords<Record>();
            var unitDataCollection = new List<UnitData>();

            foreach (var record in records)
            {
                try
                {
                    // Some CSV files had invalid date format so skip that record
                    if (DateTime.TryParse(record.Date.Split(";")[0], CultureInfo.InvariantCulture, DateTimeStyles.None, out var dateTime) &&
                       int.TryParse(record.Value, out var value))
                    {
                        unitDataCollection.Add(new UnitData(dateTime, value));
                    }
                }
                catch
                {
                    // Some CSV files had totally invalid formats
                }
            }

            return unitDataCollection;
        }

        private class Record
        {
            [Index(0)]
            public string Date { get; set; }

            [Index(1)]
            public string Value { get; set; }
        }
    }
}
