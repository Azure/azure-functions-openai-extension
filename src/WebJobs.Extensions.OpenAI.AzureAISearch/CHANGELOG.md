# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## v0.4.0 - Unreleased

### Changed

- Updated Microsoft.Azure.WebJobs.Extensions.OpenAI to 0.16.0

## v0.3.0 - 2024/05/06

### Changed

- Added support for key authentication back (use of managed identity is always recommended)
- Updated Microsoft.Azure.WebJobs.Extensions.OpenAI to 0.15.0

## v0.2.0 - 2024/04/24

### Changed

- Updated Microsoft.Azure.WebJobs.Extensions.OpenAI to 0.14.0

### Breaking

- Removed authentication support based on Search API Key, managed identity is default.

## v0.1.0 - 2024/04/05

### Added

- Added support for Azure AI Search Provider. Refer [README](../../samples/rag-aisearch/README.md) for more information on usage.
