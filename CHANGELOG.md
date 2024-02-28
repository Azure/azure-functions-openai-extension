# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## v0.12.0 - 2024/02/28

### Changes

- Added dotnet-isolated support to AssistantSkills

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

- Migrated from Betalgo to Azure Open AI 1.0.0-beta.12
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
