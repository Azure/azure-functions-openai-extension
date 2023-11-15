# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

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
