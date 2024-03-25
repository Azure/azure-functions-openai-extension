# Azure functions Java library for OpenAI

This project contains the necessary annotations and classes needed for the interaction of functions Java runtime with the OpenAI extension.

## Prerequisites

* Java versions 8, 11, 17 or 21.
* Apache maven 3.0 or above.

## Build and Test

1. Update the `azure-functions-java-library-openai` version in `pom.xml`.
2. Build and install the library with necessary changes into local maven repository using `mvn clean install -Dgpg.skip`
3. Update your Azure functions Java `pom.xml` to use the above version, build and test your samples.


## Release

1. Build and install the unsigned jars into your local maven repository using `mvn clean install -Dgpg.skip`. This should install the jars at - `C:\Users\<username>\.m2\repository\com\microsoft\azure\functions\azure-functions-java-library-openai\1.0.0-SNAPSHOT`
2. Use the steps at [Partner release pipeline[(https://dev.azure.com/azure-sdk/internal/_wiki/wikis/internal.wiki/1/Partner-Release-Pipeline) that involve:
3. Uploading the jars to the [drops](https://azuresdkpartnerdrops.blob.core.windows.net/drops) container. The location for azure functions java is at -  `drops / azure-functions / java / azure-functions-java-library-openai`
4. Trigger the [java - partner-release](https://dev.azure.com/azure-sdk/internal/_build?definitionId=1809&_a=summary) pipeline to release the library to sonatype and maven.

