// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Net.Http;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

[assembly: FunctionsStartup(typeof(AssistantSample.Startup))]

namespace AssistantSample;

/// <summary>
/// Functions startup extension that configures the dependency injection container with an
/// appropriate implementation of <see cref="ITodoManager"/>.
/// </summary>
class Startup : FunctionsStartup
{
    public override void Configure(IFunctionsHostBuilder builder)
    {
        string? cosmosDbConnectionString = Environment.GetEnvironmentVariable("CosmosDbConnectionString");
        if (string.IsNullOrEmpty(cosmosDbConnectionString))
        {
            // Use an in-memory implementation of ITodoManager if no CosmosDB connection string is provided
            builder.Services.AddSingleton<ITodoManager, InMemoryTodoManager>();
        }
        else
        {
            // Use CosmosDB implementation of ITodoManager
            // Reference: https://learn.microsoft.com/azure/cosmos-db/nosql/best-practice-dotnet#best-practices-for-http-connections
            SocketsHttpHandler socketsHttpHandler = new()
            {
                PooledConnectionLifetime = TimeSpan.FromMinutes(5)
            };

            builder.Services.AddSingleton(socketsHttpHandler);

            builder.Services.AddSingleton(serviceProvider =>
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

            builder.Services.AddSingleton<ITodoManager, CosmosDbTodoManager>();
        }
    }
}