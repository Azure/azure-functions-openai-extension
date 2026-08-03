# Azure functions Java library for OpenAI

Target audience of this file - Internal developers of this library at Microsoft.
This project contains the necessary annotations and classes needed for the interaction of functions Java runtime with the OpenAI extension.

## Prerequisites

* Install Java [supported version](https://learn.microsoft.com/en-us/azure/azure-functions/functions-reference-java?tabs=bash%2Cconsumption#java-versions)
* [Apache maven](https://maven.apache.org/) 3.0 or above.

## Package feed

All Maven packages and plugins are restored from the `upstream-public` Azure Artifacts feed
(`https://pkgs.dev.azure.com/azfunc/public/_packaging/upstream-public/maven/v1`), which is configured
as the `central` repository in every `pom.xml` in this repository.

### Anonymous restore (default)

The feed allows anonymous reads, so no credentials are required to build once a package version has
been saved to the feed. External contributors and fresh clones need no setup — `mvn` just works.
Never commit credentials or a `settings.xml` containing a `<server>` entry to this repository; doing
so would force authentication on everyone.

### Authenticating (Microsoft developers only)

Authentication is only needed to *ingest* a package version that the feed has not cached yet. The
first restore of any new or upgraded dependency will fail anonymously with:

> No local versions of package '...'; please provide authentication to access versions from upstream
> that have not yet been saved to your feed.

When that happens, a Microsoft developer with access to the `azfunc/public` project must run the
restore once with credentials, which pulls the version from upstream and saves it to the feed. Every
subsequent anonymous restore then succeeds.

The recommended way to authenticate is the `artifacts-maven-credprovider` which acquires a token via
Entra ID so you don't have to manage a PAT.

Run the helper script for your shell from the root of your clone. It installs the credential provider
into your local Maven repository if it is missing, then writes `.mvn/extensions.xml`. Both scripts
are idempotent, so re-running them is safe:

```powershell
./eng/scripts/Install-MavenCredentialProvider.ps1
```

```bash
./eng/scripts/install-maven-credprovider.sh
```

Pass `-Version` / `--version` to install a different release, and `-Force` / `--force` to reinstall or
to overwrite an `.mvn/extensions.xml` the script does not manage.

If you would rather do it by hand, the equivalent steps are:

1. Bootstrap the credential provider once per machine. Run this from a directory **outside** any
   Maven project (e.g. your home directory) — it downloads the extension from the public
   `AzureArtifacts` tools feed, which needs no authentication:

   ```powershell
   mvn dependency:get "-Dartifact=com.microsoft.azure:artifacts-maven-credprovider:3.2.1" "-DremoteRepositories=central::::https://pkgs.dev.azure.com/artifacts-public/PublicTools/_packaging/AzureArtifacts/maven/v1"
   ```

   Using the repository id `central` matters: Maven records the extension as having come from
   `central`, which is the same id this repository's `pom.xml` files declare, so the cached copy
   validates during later builds.

2. Create `.mvn/extensions.xml` at the root of your clone:

   ```xml
   <extensions xmlns="http://maven.apache.org/EXTENSIONS/1.1.0" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance"
     xsi:schemaLocation="http://maven.apache.org/EXTENSIONS/1.1.0 https://maven.apache.org/xsd/core-extensions-1.0.0.xsd">
     <extension>
       <groupId>com.microsoft.azure</groupId>
       <artifactId>artifacts-maven-credprovider</artifactId>
       <version>3.2.1</version>
     </extension>
   </extensions>
   ```

`.mvn/` is deliberately listed in `.gitignore` — **do not commit it**. The extension exits when it
detects a build context, and committing it would break anonymous restores for everyone else.

If you would rather not use the credential provider, you can instead add a `<server>` entry to your
**user-level** `~/.m2/settings.xml` (never to a file inside this repository), using an Azure DevOps
personal access token with Packaging read & write scope:

```xml
<settings>
  <servers>
    <server>
      <!-- Must match the <id> of the repository declared in the pom.xml files. -->
      <id>central</id>
      <username>azfunc</username>
      <password>[PERSONAL_ACCESS_TOKEN]</password>
    </server>
  </servers>
</settings>
```

CI covers this automatically: the `MavenAuthenticate@0` task in the build templates authenticates the
`central` repository, so merged changes to dependency versions are ingested by the pipeline. The
credential provider is not used in pipelines.

## Build and Test

1. To build the java library locally, JDK 8 is required. Update the system variables - JAVA_HOME to jdk 8 path and add the jdk 8 bin path to PATH variable.
1. Update the `azure-functions-java-library-openai` version in `pom.xml`.
1. Build and install the library with necessary changes into local maven repository using `mvn clean install -D gpg.skip`
1. Update your Azure functions Java `pom.xml` to use the above version, build and test your samples.

## Release

1. Build and install the unsigned jars into your local maven repository using `mvn clean install -Dgpg.skip`. This should install the jars at - `C:\Users\<username>\.m2\repository\com\microsoft\azure\functions\azure-functions-java-library-openai\<version>`(`Users/<username>/.m2/*` for Mac, and, `/home/<username>/.m2/*` for Linux)
1. Use the steps at [Partner release pipeline](https://dev.azure.com/azure-sdk/internal/_wiki/wikis/internal.wiki/1/Partner-Release-Pipeline) that involve:
    1. Uploading the jars to the [drops](https://azuresdkpartnerdrops.blob.core.windows.net/drops) container. The location for azure functions java is at -  `drops / azure-functions / java / azure-functions-java-library-openai`
    1. Trigger the [java - partner-release](https://dev.azure.com/azure-sdk/internal/_build?definitionId=1809&_a=summary) pipeline to release the library to sonatype and maven.
