using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Authentication;
using AutoMapper;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using MongoDB.Driver;
using Sigma.IoT.Data;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Sigma.IoT.API
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();
            ConfigureVersioning(services);
            ConfigureSwagger(services);

            var cosmosDbConfiguration = Configuration.GetSection("CosmosDb").Get<CosmosDbConfiguration>();

            services
                .AddAutoMapper(typeof(Startup))
                .AddTransient<IMongoClient>(serviceProvider =>
                {
                    var url = new MongoUrl(cosmosDbConfiguration.ConnectionString);
                    var settings = MongoClientSettings.FromUrl(url);
                    settings.SslSettings = new SslSettings { EnabledSslProtocols = SslProtocols.Tls12 };
                    return new MongoClient(settings);
                })
                .AddTransient<ICacheService, CosmosDbDataProvider>(provider =>
                    new CosmosDbDataProvider(
                        provider.GetService<IMongoClient>(),
                        cosmosDbConfiguration.DbName,
                        cosmosDbConfiguration.CollectionName,
                        false));

        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHttpsRedirection();

            app.UseRouting();

            //app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });

            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint($"/swagger/v{ApiVersions.V1}/swagger.json", $"v{ApiVersions.V1}");
            });
        }

        private static void ConfigureVersioning(IServiceCollection services) =>
            services.AddApiVersioning(config =>
            {
                config.DefaultApiVersion = new ApiVersion(1, 0);
                config.AssumeDefaultVersionWhenUnspecified = true;
                config.ReportApiVersions = true;
            });

        private static void ConfigureSwagger(IServiceCollection services) =>
            services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc($"v{ApiVersions.V1}", new OpenApiInfo
                {
                    Version = $"v{ApiVersions.V1}",
                    Title = $"v{ApiVersions.V1} API",
                    Description = $"v{ApiVersions.V1} API Description"
                });

                options.OperationFilter<RemoveVersionFromParameter>();
                options.DocumentFilter<ReplaceVersionWithExactValueInPath>();

                options.DocInclusionPredicate((version, desc) =>
                {
                    var versions = desc.CustomAttributes()
                        .OfType<ApiVersionAttribute>()
                        .SelectMany(attr => attr.Versions)
                        .ToArray();

                    var maps = desc.CustomAttributes()
                        .OfType<MapToApiVersionAttribute>()
                        .SelectMany(attr => attr.Versions)
                        .ToArray();

                    return versions.Any(v => $"v{v}" == version)
                           && (!maps.Any() || maps.Any(v => $"v{v}" == version)); ;
                });

                var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                options.IncludeXmlComments(xmlPath);
            });

        private class RemoveVersionFromParameter : IOperationFilter
        {
            public void Apply(OpenApiOperation operation, OperationFilterContext context)
            {
                var versionParameter = operation.Parameters.Single(parameter => parameter.Name == "version");
                operation.Parameters.Remove(versionParameter);
            }
        }

        private class ReplaceVersionWithExactValueInPath : IDocumentFilter
        {
            public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
            {
                var keysList = swaggerDoc.Paths.Keys.ToList();

                foreach (var pathKey in keysList)
                {
                    var path = swaggerDoc.Paths[pathKey];
                    swaggerDoc.Paths.Remove(pathKey);

                    var newKey = pathKey.Replace("v{version}", swaggerDoc.Info.Version);
                    swaggerDoc.Paths.Add(newKey, path);
                }
            }
        }
    }
}
