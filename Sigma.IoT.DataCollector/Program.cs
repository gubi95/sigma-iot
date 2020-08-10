using System;
using System.Security.Authentication;
using System.Threading.Tasks;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Blob;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Sigma.IoT.Data;

namespace Sigma.IoT.DataCollector
{
    internal class Program
    {
        private static async Task Main(string[] args)
        {
            var serviceCollection = new ServiceCollection();
            ConfigureApp(serviceCollection);

            await using var serviceProvider = serviceCollection.BuildServiceProvider();
            var dataImporter = serviceProvider.GetService<DataImporter>();
            await dataImporter.RunAsync().ConfigureAwait(false);
        }

        private static void ConfigureApp(IServiceCollection serviceCollection)
        {
            var configBuilder = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .Build();

            var configuration = configBuilder.GetSection("Settings").Get<Configuration>();

            serviceCollection
                .AddLogging(configure => configure.AddConsole(c =>
                {
                    c.TimestampFormat = "[yyyy-MM-dd HH:mm:ss] ";
                }))
                .AddTransient<IDataProvider>(serviceProvider =>
                {
                    var cloudStorageAccount = CloudStorageAccount.Parse(configuration.BlobEndpoint);
                    var fileProvider = new AzureBlobStorageFileProvider(cloudStorageAccount.CreateCloudBlobClient(),
                        configuration.BlobContainerName);
                    var filePathBuilder = new AzureBlobStorageFilePathBuilder();
                    var fileConverter = new CsvToUnitDataConverter();
                    var dataProvider = new FileDataProvider(fileProvider, filePathBuilder, fileConverter);
                    return dataProvider;
                })
                .AddTransient<IMongoClient>(serviceProvider =>
                {
                    var timeout = new TimeSpan(1, 0, 0);
                    var settings = MongoClientSettings.FromUrl(new MongoUrl(configuration.CacheEndpoint));
                    settings.SslSettings = new SslSettings { EnabledSslProtocols = SslProtocols.Tls12 };
                    settings.ConnectTimeout = timeout;
                    settings.MaxConnectionIdleTime = timeout;
                    settings.MaxConnectionLifeTime = timeout;
                    settings.ServerSelectionTimeout = timeout;
                    settings.SocketTimeout = timeout;
                    settings.WaitQueueTimeout = timeout;
                    return new MongoClient(settings);
                })
                .AddTransient<ICacheService>(serviceProvider =>
                {
                    var mongoClient = serviceProvider.GetService<IMongoClient>();
                    return new CosmosDbDataProvider(mongoClient, configuration.CacheDbName, configuration.CacheCollectionName, true);
                })
                .AddTransient<DataImporter>();
        }
    }
}
