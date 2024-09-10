// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json;
using System.Text.Json.Serialization;
using AssistantSample;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureServices(services =>
    {
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();

        services.Configure<JsonSerializerOptions>(options =>
        {
            options.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
            options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        });

        string? cosmosDbConnectionString = Environment.GetEnvironmentVariable("CosmosDbConnectionString");
        if (string.IsNullOrEmpty(cosmosDbConnectionString))
        {
            // Use an in-memory implementation of ITodoManager if no CosmosDB connection string is provided
            services.AddSingleton<ITodoManager, InMemoryTodoManager>();
        }
        else
        {
            // Use CosmosDB implementation of ITodoManager
            // Reference: https://learn.microsoft.com/azure/cosmos-db/nosql/best-practice-dotnet#best-practices-for-http-connections
            SocketsHttpHandler socketsHttpHandler = new()
            {
                PooledConnectionLifetime = TimeSpan.FromMinutes(5)
            };

            services.AddSingleton(socketsHttpHandler);

            services.AddSingleton(serviceProvider =>
            {
                SocketsHttpHandler socketsHttpHandler = serviceProvider.GetRequiredService<SocketsHttpHandler>();
                CosmosClientOptions cosmosClientOptions = new()
                {
                    HttpClientFactory = () => new HttpClient(socketsHttpHandler, disposeHandler: false),
                    SerializerOptions = new CosmosSerializationOptions
                    {
                        PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase
                    }
                };

                return new CosmosClient(cosmosDbConnectionString, cosmosClientOptions);
            });

            services.AddSingleton<ITodoManager, CosmosDbTodoManager>();
        }
    })
    .Build();

host.Run();
