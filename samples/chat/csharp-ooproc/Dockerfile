FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build-env

# Add support for .NET 6 (required for Azure Functions build process)
COPY --from=mcr.microsoft.com/dotnet/sdk:6.0 /usr/share/dotnet/shared /usr/share/dotnet/shared

# Environment variable for getting test version
ENV testVersion=99.99.99-test

# Need to build the full repo to get the samples to work
COPY . /root
RUN cd /root/samples/chat/csharp-ooproc && \
    mkdir -p /home/site/wwwroot && \
    mkdir ~/NuGetPackagesLocal && \
    dotnet build ../../../src/WebJobs.Extensions.OpenAI/WebJobs.Extensions.OpenAI.csproj -p:WebJobsVersion=$testVersion -p:Version=$testVersion && \
    cp "/root/src/WebJobs.Extensions.OpenAI/bin/Debug/Microsoft.Azure.WebJobs.Extensions.OpenAI.${testVersion}.nupkg" ~/NuGetPackagesLocal && \
    dotnet nuget add source ~/NuGetPackagesLocal && \
    dotnet build -c Release -p:WebJobsVersion=$testVersion -p:Version=$testVersion && \
    dotnet publish --no-restore -c Release -o /home/site/wwwroot -p:WebJobsVersion=$testVersion -p:Version=$testVersion

# The final image is based on the Azure Functions 4.0 runtime image
FROM mcr.microsoft.com/azure-functions/dotnet-isolated:4

# This is the standard setup for Azure Functions running in Docker containers
ENV AzureWebJobsScriptRoot=/home/site/wwwroot \
    AzureFunctionsJobHost__Logging__Console__IsEnabled=true
COPY --from=build-env ["/home/site/wwwroot", "/home/site/wwwroot"]
