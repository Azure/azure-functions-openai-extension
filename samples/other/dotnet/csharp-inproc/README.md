# OpenAI Bindings - .NET in-proc samples

This project contains sample code for using the OpenAI bindings with the Azure Functions .NET in-proc experience. The examples are written in C#.

## Prerequisites

Please refer to the root level [README](../../../../README.md#requirements) for prerequisites.

## Installing the OpenAI extension

The OpenAI extension isn't yet available on nuget.org and therefore must be built locally. This is handled automatically by the build process.

## Running the Function app

Use the following command to build the project:

```bash
dotnet build
```

Once built, you can run the sample using the following command:

```bash
func start
```

You should see output similar to the following:

```text
Azure Functions Core Tools
Core Tools Version:       4.0.4915 Commit hash: N/A  (64-bit)
Function Runtime Version: 4.14.0.19631

Functions:

        (list of functions)
```

## Running the "who is" Text Completion sample

Use a tool like [Postman](https://www.postman.com/) to send a request to the `WhoIs` function. The following is an example request:

```http
POST http://localhost:7071/api/whois/pikachu
```

The HTTP response should look something like the following example (with newlines added for readability):

```text
Pikachu is a fictional creature from the Pokemon franchise. It is a yellow
mouse-like creature with powerful electrical abilities and a mischievous
personality. Pikachu is one of the most iconic and recognizable characters
from the franchise, and is featured in numerous video games, anime series,
movies, and other media.
```
