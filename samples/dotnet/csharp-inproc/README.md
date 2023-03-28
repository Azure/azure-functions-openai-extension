# OpenAI Bindings - .NET in-proc samples

This project contains sample code for using the OpenAI bindings with the Azure Functions .NET in-proc experience. The examples are written in C#.

## Prerequisites

You must have the following installed on your local machine in order to run these samples.

* [.NET 6 SDK](https://dotnet.microsoft.com/download/dotnet/6.0)
* [Azure Functions Core Tools v4.x](https://learn.microsoft.com/azure/azure-functions/functions-run-local?tabs=v4%2Cwindows%2Ccsharp%2Cportal%2Cbash)
* An [OpenAI API key](https://platform.openai.com/account/api-keys) saved into a `OPENAI_API_KEY` environment variable

## Installing the OpenAI extension

The OpenAI extension isn't yet available on nuget.org and therefore must be built locally. This is handled automatically by the build process.

## Running the "who is" Text Completion sample

Use the following command to build the project:

```bash
dotnet build
```

Once built, you can run the sample using the following command:

```bash
func start
```

You should see output similar to the following:

```
Azure Functions Core Tools
Core Tools Version:       4.0.4915 Commit hash: N/A  (64-bit)
Function Runtime Version: 4.14.0.19631

Functions:

        WhoIs:  http://localhost:7071/api/whois/{name}
```

You can then use a tool like [Postman](https://www.postman.com/) to send a request to the function. The following is an example request:

```http
POST http://localhost:7071/api/whois/pikachu
```

The HTTP response should look something like the following example (with newlines added for readability):

```http
HTTP/1.1 200 OK
Content-Type: text/plain; charset=utf-8
Server: Kestrel
Transfer-Encoding: chunked

Pikachu is a fictional creature from the Pokémon franchise. It is a yellow
mouse-like creature with powerful electrical abilities and a mischievous
personality. Pikachu is one of the most iconic and recognizable characters
from the franchise, and is featured in numerous video games, anime series,
movies, and other media.
```