FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build-env

RUN apt-get update && \
  apt-get install -y curl gpg && \
  apt install -y lsb-release && \
  curl https://packages.microsoft.com/keys/microsoft.asc | gpg --dearmor > microsoft.gpg && \
  mv microsoft.gpg /etc/apt/trusted.gpg.d/microsoft.gpg && \
  echo "deb [arch=amd64] https://packages.microsoft.com/debian/$(lsb_release -rs | cut -d'.' -f 1)/prod $(lsb_release -cs) main" > /etc/apt/sources.list.d/dotnetdev.list && \
  apt-get update && \
  apt-get install -y azure-functions-core-tools-4

# Add support for .NET 6 (required for Azure Functions build process)
COPY --from=mcr.microsoft.com/dotnet/sdk:6.0 /usr/share/dotnet/shared /usr/share/dotnet/shared

# Need to build the full repo to get the samples to work
COPY . /root
RUN cd /root/samples/chat/csharp-ooproc && \
    mkdir -p /home/site/wwwroot && \
    dotnet restore --verbosity normal && \
    dotnet build --no-restore -c Release && \
    dotnet publish --no-restore -c Release -o /home/site/wwwroot

# The final image is based on the Azure Functions 4.0 runtime image
FROM mcr.microsoft.com/azure-functions/dotnet:4

# This is the standard setup for Azure Functions running in Docker containers
ENV AzureWebJobsScriptRoot=/home/site/wwwroot \
    AzureFunctionsJobHost__Logging__Console__IsEnabled=true
COPY --from=build-env ["/home/site/wwwroot", "/home/site/wwwroot"]
