using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Authentication;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using Sigma.IoT.Data;

[assembly: FunctionsStartup(typeof(Sigma.IoT.DataCollectorAzureFunction.Startup))]

namespace Sigma.IoT.DataCollectorAzureFunction
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables()
                .Build();

            var cacheConfiguration = new CacheConfiguration();
            config.GetSection("CacheConfiguration").Bind(cacheConfiguration);

            builder.Services
                .Configure<CacheConfiguration>(config.GetSection("CacheConfiguration"))
                .AddTransient<IMongoClient>(serviceProvider =>
                {
                    var timeout = new TimeSpan(1, 0, 0);
                    var settings = MongoClientSettings.FromUrl(new MongoUrl(cacheConfiguration.Endpoint));
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
                    return new CosmosDbDataProvider(mongoClient, cacheConfiguration.DbName, cacheConfiguration.CollectionName, false);
                })
                .AddTransient<IFileToDataConverter<Stream, IEnumerable<UnitData>>>(serviceProvider => new CsvToUnitDataConverter());
        }
    }
}
