# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

Starting v0.13.0 for Microsoft.Azure.WebJobs.Extensions.OpenAI.Kusto, it will maintain its own [Changelog](./src/WebJobs.Extensions.OpenAI.Kusto/CHANGELOG.md)

Starting v0.1.0 for Microsoft.Azure.WebJobs.Extensions.OpenAI.AzureAISearch, it will maintain its own [Changelog](./src/WebJobs.Extensions.OpenAI)

## v0.14.0 - Unreleased

### Changes

- Updated Azure.Identity from 1.10.4 to 1.11.0

### Breaking Changes

- Overlapping introduced to chunking in embeddings with word breaks and sentence endings.
- Removed authentication support based on Azure OpenAI Key, managed identity is default.
  
## v0.13.0 - 2024/04/05

### Added

- Added dotnet-isolated support for Embeddings
- Added support for multiple search providers.
- Added dotnet-isolated support for Kusto and Azure AI Search RAG scenarios.

### Breaking changes

- Changed default embeddings model from `text-embedding-ada-002` to `text-embedding-3-small`

### Changes

- Azure.AI.OpenAI package updated to 1.0.0-beta.15

## v0.12.0 - 2024/03/01

### Changes

- Added dotnet-isolated support to AssistantSkills
- Downgraded Microsoft.Azure.Kusto.Data and Microsoft.Azure.Kusto.Ingest packages to 11.3.5.

### Breaking changes

- Changed all references from `chatBot` to `assistants` (#17)
- Renamed namespace from `Microsoft.Azure.WebJobs.Extensions.OpenAI.Agents` to `Microsoft.Azure.WebJobs.Extensions.OpenAI.Assistants`
- Renamed namespace from `Functions.Worker.Extensions.OpenAI` to `Microsoft.Azure.Functions.Worker.Extensions.OpenAI`

## v0.11.0 - 2024/02/27

### Changes

- Added E2E test for ChatBot
- Extension bundle support

## v0.10.0 - 2024/02/21

### Changes

- Minor fixes to support extension bundles.

## v0.9.0 - 2024/02/21

### Changes

- Update nuget versions of available dependencies
- Bug fixed in totalTokens
- Minor fixes to support extension bundles.

## v0.8.0 - 2024/02/15

### Changes

- Enable Chat Completion to work with .NET Isolated ([#10](https://github.com/Azure/azure-functions-openai-extension/pull/10))
- Assistants: chat bots with skills via new `assistantSkillTrigger` trigger ([#14](https://github.com/Azure/azure-functions-openai-extension/pull/14))

## v0.7.0 - 2024/01/26

### Changes

- Text Completion using Chat Completion at the backend with default model as GPT 3.5 Turbo
- Added property totalTokens - total token usage to Chat Bot and Text Completion.

## v0.6.0 - 2024/01/22

### Changes

- Migrated from Betalgo to Azure OpenAI 1.0.0-beta.12
- Text Completion default model updated.

## v0.5.0 - 2024/01/03

### Changes

- Moved to Microsoft repository and references updated

## v0.4.0 - 2023/11/14

### Added

- Added support for text completion input bindings in .NET Isolated
- Added support for configuring the model used by chat bots

### Changes

- Updated [Betalgo.OpenAI](https://www.nuget.org/packages/Betalgo.OpenAI) from 7.3.0 to 7.4.0.
  - Fixes bad error handling behavior that was introduced in 7.3.0.

## v0.3.1 - 2023/11/5

### Changes

- Replace System.Text.Json usage with Newtonsoft.Json (unblocks .NET Isolated support)

## v0.3.0

### Added

- Support for dynamic deployment IDs with Azure OpenAI
- Added README.md for the semantic search sample

### Breaking changes

- Updated [Betalgo.OpenAI](https://www.nuget.org/packages/Betalgo.OpenAI) from 6.8.* to 7.3.0.
