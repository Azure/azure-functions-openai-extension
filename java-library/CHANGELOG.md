# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## v0.5.0 - Unreleased

### Breaking

- model properties renamed to `chatModel` and `embeddingsModel` in assistantPost, embeddings and textCompletion bindings.
- renamed connectionName to `searchConnectionName` in semanticSearch binding.
- renamed connectionName to `storeConnectionName` in embeddingsStore binding.
- renamed ChatMessage to `AssistantMessage`.
- managed identity support through config section and binding parameter `aiConnectionName` in assistantPost, embeddings, embeddingsStore, semanticSearch and textCompletion bindings.

### Changed

- Update azure-ai-openai from 1.0.0-beta.11 to 1.0.0-beta.16

### Added

- Introduced experimental `isReasoningModel` property to support reasoning models. Setting of max_completion_tokens and reasoning_effort is not supported with current underlying Azure.AI.OpenAI

## v0.4.0 - 2024/10/08

### Changed

- Removed java-8-parent maven library, added some plugins, attached doc and source jars
- Fix added for table storage identity
