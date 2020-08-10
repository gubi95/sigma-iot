using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using JsonConvert = Newtonsoft.Json.JsonConvert;

namespace Sigma.IoT.Data
{
    public class CosmosDbDataProvider : ICacheService
    {
        private const string DateFormat = "yyyy-MM-dd";
        private const string TimeFormat = "HH:mm:ss";
        private readonly IMongoClient _client;
        private readonly string _databaseName;
        private readonly string _collectionName;

        public CosmosDbDataProvider(IMongoClient client, string databaseName, string collectionName, bool prepareStorage)
        {
            if (string.IsNullOrWhiteSpace(databaseName))
            {
                throw new ArgumentException($"Argument {nameof(databaseName)} cannot be null or empty string", nameof(databaseName));
            }

            if (string.IsNullOrWhiteSpace(collectionName))
            {
                throw new ArgumentException($"Argument {nameof(collectionName)} cannot be null or empty string", nameof(collectionName));
            }

            _client = client ?? throw new ArgumentNullException(nameof(client));
            _databaseName = databaseName;
            _collectionName = collectionName;

            if (prepareStorage)
            {
                PrepareStorage(client);
            }
        }

        public async Task<IReadOnlyCollection<SensorData>> GetDataAsync(string deviceName, DateTime date, SensorType? sensorType = null)
        {
            var collection = _client.GetDatabase(_databaseName).GetCollection<BsonDocument>(_collectionName);
            var filter = GetDocumentFilter(deviceName, date, sensorType);
            var documents = await collection.FindAsync(filter).ConfigureAwait(false);
            var data = await MapAsync(documents).ConfigureAwait(false);
            return data;
        }

        public async Task SaveDataAsync(string deviceName, SensorType sensorType, IReadOnlyCollection<UnitData> sensorsData)
        {
            var collection = _client.GetDatabase(_databaseName).GetCollection<BsonDocument>(_collectionName);
            var tasks = sensorsData.GroupBy(x => x.DateTime.Date)
                .Select(group => new { Dto = Map(deviceName, sensorType, group.Key.Date, group), Date = group.Key })
                .Select(x => collection
                    .ReplaceOneAsync(
                        GetDocumentFilter(deviceName, x.Date, sensorType),
                        BsonSerializer.Deserialize<BsonDocument>(JsonConvert.SerializeObject(x.Dto)),
                        new UpdateOptions { IsUpsert = true }));
            await Task.WhenAll(tasks).ConfigureAwait(false);
        }

        private FilterDefinition<BsonDocument> GetDocumentFilter(string deviceName, DateTime date, SensorType? sensorType = null)
        {
            var filter = Builders<BsonDocument>.Filter.Eq(nameof(DocumentDto.DeviceName), deviceName);

            filter &= Builders<BsonDocument>.Filter.Eq(nameof(DocumentDto.Date), date.ToString(DateFormat, CultureInfo.InvariantCulture));

            if (sensorType.HasValue)
            {
                filter &= Builders<BsonDocument>.Filter.Eq(nameof(DocumentDto.SensorType), (int)sensorType.Value);
            }

            return filter;
        }

        private static async Task<IReadOnlyCollection<SensorData>> MapAsync(IAsyncCursor<BsonDocument> bsonDocuments)
        {
            var documents = new List<DocumentDto>();

            await bsonDocuments.ForEachAsync(bsonDocument =>
            {
                bsonDocument.Remove("_id");
                var dto = JsonConvert.DeserializeObject<DocumentDto>(bsonDocument.ToString());
                documents.Add(dto);
            }).ConfigureAwait(false);

            return documents.SelectMany(document => document.Values.Select(value => Map(value, document.Date, document.SensorType))).ToList();
        }

        private static DocumentDto Map(string deviceName, SensorType sensorType, DateTime date, IEnumerable<UnitData> unitData) =>
            new DocumentDto
            {
                DeviceName = deviceName,
                SensorType = sensorType,
                Date = date.ToString(DateFormat, CultureInfo.InvariantCulture),
                Values = unitData.OrderBy(x => x.DateTime).Select(Map)
            };

        private void PrepareStorage(IMongoClient mongoClient)
        {
            var indexedProperties = new List<string>
            {
                nameof(DocumentDto.DeviceName),
                nameof(DocumentDto.Date),
                nameof(DocumentDto.SensorType),
            };

            var collection = mongoClient
                .GetDatabase(_databaseName)
                .GetCollection<BsonDocument>(_collectionName);

            var createIndexOptions = new CreateIndexOptions { Background = false };

            foreach (var indexedProperty in indexedProperties)
            {
                collection.Indexes.CreateOne(Builders<BsonDocument>.IndexKeys.Ascending(indexedProperty), createIndexOptions);
            }
        }

        private static ValueDto Map(UnitData unitData) =>
            new ValueDto
            {
                Value = unitData.Value,
                Time = unitData.DateTime.ToString(TimeFormat, CultureInfo.InvariantCulture)
            };

        private static SensorData Map(ValueDto valueDto, string date, SensorType sensorType)
        {
            var parsedDate = DateTime.ParseExact(date, DateFormat, CultureInfo.InvariantCulture);
            var time = DateTime.ParseExact(valueDto.Time, TimeFormat, CultureInfo.InvariantCulture);
            var dateTime = new DateTime(parsedDate.Year, parsedDate.Month, parsedDate.Day, time.Hour, time.Minute, time.Second, 0);
            return new SensorData(dateTime, valueDto.Value, sensorType);
        }

        private class DocumentDto
        {
            public string DeviceName { get; set; }

            public SensorType SensorType { get; set; }

            public string Date { get; set; }

            public IEnumerable<ValueDto> Values { get; set; }
        }

        private class ValueDto
        {
            public int Value { get; set; }

            public string Time { get; set; }
        }
    }
}
