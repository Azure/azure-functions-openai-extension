# Azure functions Java library for OpenAI

Target audience of this file - Internal developers of this library at Microsoft.
This project contains the necessary annotations and classes needed for the interaction of functions Java runtime with the OpenAI extension.

## Prerequisites

* Install Java [supported version](https://learn.microsoft.com/en-us/azure/azure-functions/functions-reference-java?tabs=bash%2Cconsumption#java-versions)
* [Apache maven](https://maven.apache.org/) 3.0 or above.

## Build and Test

1. To build the java library locally, JDK 8 is required. Update the system variables - JAVA_HOME to jdk 8 path and add the jdk 8 bin path to PATH variable.
1. Update the `azure-functions-java-library-openai` version in `pom.xml`.
1. Build and install the library with necessary changes into local maven repository using `mvn clean install -D gpg.skip`
1. Update your Azure functions Java `pom.xml` to use the above version, build and test your samples.

## Release

1. Build and install the unsigned jars into your local maven repository using `mvn clean install -Dgpg.skip`. This should install the jars at - `C:\Users\<username>\.m2\repository\com\microsoft\azure\functions\azure-functions-java-library-openai\0.1.0-preview`(`Users/<username>/.m2/*` for Mac, and, `/home/<username>/.m2/*` for Linux)
1. Use the steps at [Partner release pipeline](https://dev.azure.com/azure-sdk/internal/_wiki/wikis/internal.wiki/1/Partner-Release-Pipeline) that involve:
    1. Uploading the jars to the [drops](https://azuresdkpartnerdrops.blob.core.windows.net/drops) container. The location for azure functions java is at -  `drops / azure-functions / java / azure-functions-java-library-openai`
    1. Trigger the [java - partner-release](https://dev.azure.com/azure-sdk/internal/_build?definitionId=1809&_a=summary) pipeline to release the library to sonatype and maven.
