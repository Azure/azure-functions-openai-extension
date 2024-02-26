FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build-env

# Add support for .NET 6 (required for Azure Functions build process)
COPY --from=mcr.microsoft.com/dotnet/sdk:6.0 /usr/share/dotnet/shared /usr/share/dotnet/shared

# Need to build the full repo to get the samples to work
COPY . /root
RUN cd /root/samples/chat/csharp-ooproc && \
    mkdir -p /home/site/wwwroot && \
    version=$(sed -n "s/.*<WebJobsVersion[^>]*>\([^<]*\)<\/WebJobsVersion>.*/\1/p" /app/src/Functions.Worker.Extensions.OpenAI/Functions.Worker.Extensions.OpenAI.csproj) \
    && echo "$version" \
    && newVersion="${version}-test" \
    && echo "$newVersion" \
    && dotnet build /app/src/WebJobs.Extensions.OpenAI/WebJobs.Extensions.OpenAI.csproj --configuration $config -p:FileVersionRevision=$GITHUB_RUN_NUMBER -p:WebJobsVersion=$newVersion -p:Version=$newVersion \
    && cp "/app/src/WebJobs.Extensions.OpenAI/bin/Release/Microsoft.Azure.WebJobs.Extensions.OpenAI.${newVersion}.nupkg" /NuGetPackagesLocal \
    && ls /NuGetPackagesLocal \
    && pwd \
    && dotnet nuget locals all --clear \
    && dotnet new nuget.config --force \
    && dotnet nuget add source /NuGetPackagesLocal \
    && dotnet nuget list source \
    && pwd \
    && dotnet build OpenAI-Extension.sln --configuration $config -p:FileVersionRevision=$GITHUB_RUN_NUMBER -p:WebJobsVersion=0.11.0-alpha-test -p:Version=$newVersion \
    && dotnet build OpenAI-Extension.sln --configuration $config -p:FileVersionRevision=$GITHUB_RUN_NUMBER -p:WebJobsVersion=$newVersion -p:Version=$newVersion

RUN dotnet restore --verbosity normal && \
    dotnet build --no-restore -c Release && \
    dotnet publish --no-restore -c Release -o /home/site/wwwroot

# The final image is based on the Azure Functions 4.0 runtime image
FROM mcr.microsoft.com/azure-functions/dotnet-isolated:4

# This is the standard setup for Azure Functions running in Docker containers
ENV AzureWebJobsScriptRoot=/home/site/wwwroot \
    AzureFunctionsJobHost__Logging__Console__IsEnabled=true
COPY --from=build-env ["/home/site/wwwroot", "/home/site/wwwroot"]
