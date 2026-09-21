// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.ClientModel;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using OpenAI.Embeddings;

namespace Microsoft.Azure.WebJobs.Extensions.OpenAI.Embeddings;

static class EmbeddingsHelper
{
    internal const string FilePathRootSettingName = "OPENAI_EMBEDDINGS_FILE_PATH_ROOT";
    internal const string UrlAllowedOriginsSettingName = "OPENAI_EMBEDDINGS_URL_ALLOWED_ORIGINS";

    const int MaxUrlContentLength = 10 * 1024 * 1024; // Cap downloads at 10 MB to bound memory use.
    const int UrlReadBufferSize = 81920; // Match the standard .NET stream-copy buffer size.
    static readonly TimeSpan UrlDownloadTimeout = TimeSpan.FromSeconds(30);
    static readonly char[] sentenceEndingsDefault = new[] { '.', '!', '?' };
    static readonly char[] wordBreaksDefault = new[] { ',', ';', ':', ' ', '(', ')', '[', ']', '{', '}', '\t', '\n' };
    static readonly string UserAgent = $"{typeof(OpenAIExtension).Namespace}/{FileVersionInfo.GetVersionInfo(typeof(OpenAIExtension).Assembly.Location).FileVersion}";

    // Function app settings are process-level; configuration changes restart the host.
    static readonly string? configuredFilePathRoot = Environment.GetEnvironmentVariable(FilePathRootSettingName);
    static readonly string? configuredUrlAllowedOrigins = Environment.GetEnvironmentVariable(UrlAllowedOriginsSettingName);
    static readonly HttpClient httpClient = new();
    static readonly HttpClient restrictedHttpClient = new(new HttpClientHandler
    {
        AllowAutoRedirect = false
    });

    // Log missing opt-in configuration warnings only once per host process.
    static int filePathRootNoticeLogged;
    static int urlAllowedOriginsNoticeLogged;

    static EmbeddingsHelper()
    {
        httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgent);
        restrictedHttpClient.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgent);
    }

    internal static async Task<EmbeddingsContext> GenerateEmbeddingsAsync(
        EmbeddingsBaseAttribute attribute,
        OpenAIClientFactory openAIClientFactory,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        List<string> chunks = await BuildRequest(attribute, logger, cancellationToken);

        logger.LogInformation("Sending OpenAI embeddings request");

        ClientResult<OpenAIEmbeddingCollection> response = await openAIClientFactory.GetEmbeddingClient(
            attribute.AIConnectionName,
            attribute.EmbeddingsModel).GenerateEmbeddingsAsync(chunks, cancellationToken: cancellationToken);

        logger.LogInformation("Received OpenAI embeddings count: {count}", response.Value.Count);

        return new EmbeddingsContext(chunks, response);
    }

    static async Task<List<string>> BuildRequest(
        EmbeddingsBaseAttribute attribute,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        using TextReader reader = await GetTextReader(
            attribute.InputType,
            attribute.Input,
            logger,
            cancellationToken);
        if (attribute.MaxOverlap >= attribute.MaxChunkLength)
        {
            throw new ArgumentOutOfRangeException($"MaxOverlap ({attribute.MaxOverlap}) must be less than MaxChunkLength ({attribute.MaxChunkLength}).");
        }

        List<string> chunks = GetTextChunks(reader, 0, attribute.MaxChunkLength, attribute.MaxOverlap).ToList();
        return chunks;
    }

    static async Task<TextReader> GetTextReader(
        InputType inputType,
        string input,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        if (inputType == InputType.RawText)
        {
            return new StringReader(input);
        }
        else if (inputType == InputType.FilePath)
        {
            if (string.IsNullOrWhiteSpace(configuredFilePathRoot) &&
                Interlocked.Exchange(ref filePathRootNoticeLogged, 1) == 0)
            {
                logger.LogWarning(
                    "FilePath input is running without {SettingName}. Configure this setting to constrain relative paths.",
                    FilePathRootSettingName);
            }

            return new StreamReader(ResolveFilePath(input, configuredFilePathRoot));
        }
        else if (inputType == InputType.Url)
        {
            Uri uri = ValidateUrl(input, configuredUrlAllowedOrigins);

            if (string.IsNullOrWhiteSpace(configuredUrlAllowedOrigins))
            {
                if (Interlocked.Exchange(ref urlAllowedOriginsNoticeLogged, 1) == 0)
                {
                    logger.LogWarning(
                        "Url input is running without {SettingName}. Configure this setting to constrain destinations.",
                        UrlAllowedOriginsSettingName);
                }

                Stream stream = await httpClient.GetStreamAsync(uri);
                return new StreamReader(stream);
            }

            return await GetRestrictedUrlReader(
                uri,
                restrictedHttpClient,
                UrlDownloadTimeout,
                cancellationToken);
        }
        else
        {
            throw new NotSupportedException($"InputType = '{inputType}' is not supported.");
        }
    }

    internal static Uri ValidateUrl(string input, string? allowedOrigins)
    {
        if (!Uri.TryCreate(input, UriKind.Absolute, out Uri? uri) ||
            uri.Scheme != Uri.UriSchemeHttps)
        {
            throw new ArgumentException($"Invalid Url: {input}. Ensure it is a valid https Url.", nameof(input));
        }

        if (string.IsNullOrWhiteSpace(allowedOrigins))
        {
            return uri;
        }

        if (!string.IsNullOrEmpty(uri.UserInfo))
        {
            throw new ArgumentException(
                $"The Url cannot contain user information when {UrlAllowedOriginsSettingName} is configured.",
                nameof(input));
        }

        HashSet<string> configuredOrigins = ParseAllowedOrigins(allowedOrigins);
        string inputOrigin = GetOrigin(uri);
        if (!configuredOrigins.Contains(inputOrigin))
        {
            throw new ArgumentException(
                $"The Url origin is not listed in {UrlAllowedOriginsSettingName}.",
                nameof(input));
        }

        return uri;
    }

    static HashSet<string> ParseAllowedOrigins(string allowedOrigins)
    {
        HashSet<string> configuredOrigins = new(StringComparer.OrdinalIgnoreCase);
        foreach (string value in allowedOrigins.Split(',', StringSplitOptions.RemoveEmptyEntries))
        {
            string origin = value.Trim();
            if (!Uri.TryCreate(origin, UriKind.Absolute, out Uri? originUri) ||
                originUri.Scheme != Uri.UriSchemeHttps ||
                !string.IsNullOrEmpty(originUri.UserInfo) ||
                originUri.AbsolutePath != "/" ||
                !string.IsNullOrEmpty(originUri.Query) ||
                !string.IsNullOrEmpty(originUri.Fragment))
            {
                throw new InvalidOperationException(
                    $"{UrlAllowedOriginsSettingName} contains an invalid origin: '{origin}'.");
            }

            configuredOrigins.Add(GetOrigin(originUri));
        }

        if (configuredOrigins.Count == 0)
        {
            throw new InvalidOperationException(
                $"{UrlAllowedOriginsSettingName} must contain at least one https origin.");
        }

        return configuredOrigins;
    }

    static string GetOrigin(Uri uri)
    {
        return uri.GetLeftPart(UriPartial.Authority).TrimEnd('/');
    }

    internal static async Task<TextReader> GetRestrictedUrlReader(
        Uri uri,
        HttpClient client,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        using CancellationTokenSource timeoutSource =
            CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutSource.CancelAfter(timeout);
        CancellationToken downloadCancellationToken = timeoutSource.Token;

        try
        {
            using HttpResponseMessage response = await client.GetAsync(
                uri,
                HttpCompletionOption.ResponseHeadersRead,
                downloadCancellationToken);

            int statusCode = (int)response.StatusCode;
            if (statusCode >= 300 && statusCode < 400)
            {
                throw new InvalidOperationException("Redirect responses are not supported for configured Url origins.");
            }

            response.EnsureSuccessStatusCode();

            if (response.Content.Headers.ContentLength > MaxUrlContentLength)
            {
                throw new InvalidOperationException(
                    $"Url content exceeds the maximum supported size of {MaxUrlContentLength} bytes.");
            }

            using Stream responseStream = await response.Content.ReadAsStreamAsync();
            MemoryStream content = new();
            try
            {
                byte[] buffer = new byte[UrlReadBufferSize];
                int totalBytesRead = 0;
                int bytesRead;
                while ((bytesRead = await responseStream.ReadAsync(
                    buffer,
                    0,
                    buffer.Length,
                    downloadCancellationToken)) > 0)
                {
                    totalBytesRead += bytesRead;
                    if (totalBytesRead > MaxUrlContentLength)
                    {
                        throw new InvalidOperationException(
                            $"Url content exceeds the maximum supported size of {MaxUrlContentLength} bytes.");
                    }

                    await content.WriteAsync(buffer, 0, bytesRead, downloadCancellationToken);
                }

                content.Position = 0;
                return new StreamReader(content);
            }
            catch
            {
                content.Dispose();
                throw;
            }
        }
        catch (OperationCanceledException exception)
            when (!cancellationToken.IsCancellationRequested && timeoutSource.IsCancellationRequested)
        {
            throw new TimeoutException($"Url download exceeded the timeout of {timeout.TotalSeconds} seconds.", exception);
        }
    }

    internal static string ResolveFilePath(string input, string? allowedRoot)
    {
        if (string.IsNullOrWhiteSpace(allowedRoot))
        {
            return input;
        }

        if (string.IsNullOrWhiteSpace(input))
        {
            throw new ArgumentException("The file path cannot be empty.", nameof(input));
        }

        if (Path.IsPathRooted(input))
        {
            throw new ArgumentException(
                $"The file path must be relative when {FilePathRootSettingName} is configured.",
                nameof(input));
        }

        string rootPath = Path.GetFullPath(allowedRoot);
        string resolvedPath = Path.GetFullPath(Path.Combine(rootPath, input));
        string relativePath = Path.GetRelativePath(rootPath, resolvedPath);

        if (relativePath == ".." ||
            relativePath.StartsWith($"..{Path.DirectorySeparatorChar}", StringComparison.Ordinal) ||
            relativePath.StartsWith($"..{Path.AltDirectorySeparatorChar}", StringComparison.Ordinal) ||
            Path.IsPathRooted(relativePath))
        {
            throw new ArgumentException(
                $"The file path must remain within the directory configured by {FilePathRootSettingName}.",
                nameof(input));
        }

        RejectReparsePoints(rootPath, relativePath);
        return resolvedPath;
    }

    static void RejectReparsePoints(string rootPath, string relativePath)
    {
        RejectReparsePoint(rootPath);

        string currentPath = rootPath;
        foreach (string segment in relativePath.Split(
            new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar },
            StringSplitOptions.RemoveEmptyEntries))
        {
            currentPath = Path.Combine(currentPath, segment);
            RejectReparsePoint(currentPath);
        }
    }

    static void RejectReparsePoint(string path)
    {
        if ((File.Exists(path) || Directory.Exists(path)) &&
            File.GetAttributes(path).HasFlag(FileAttributes.ReparsePoint))
        {
            throw new ArgumentException(
                $"The configured root and file path cannot contain links when {FilePathRootSettingName} is configured.");
        }
    }

    public static IEnumerable<string> GetTextChunks(
        TextReader reader,
        int minChunkSize,
        int maxChunkSize,
        int overlap,
        char[]? sentenceEndings = null,
        char[]? wordBreaks = null)
    {
        if (reader == null)
        {
            throw new ArgumentNullException("reader");
        }

        if (minChunkSize < 0 || maxChunkSize <= 0 || overlap < 0 || minChunkSize > maxChunkSize || overlap > maxChunkSize)
        {
            throw new ArgumentException("Invalid chunk size or overlap");
        }

        char[] buffer = new char[maxChunkSize];
        int startIndex = 0;

        sentenceEndings ??= sentenceEndingsDefault;
        wordBreaks ??= wordBreaksDefault;

        HashSet<char> sentenceEndingsSet = new(sentenceEndings);
        HashSet<char> wordBreaksSet = new(wordBreaks);

        int bytesRead;
        while ((bytesRead = reader.Read(buffer, startIndex, maxChunkSize - startIndex)) > 0)
        {
            int endIndex = startIndex + bytesRead;
            int boundaryIndex = -1;

            // Search backwards to end the chunk with a terminator character  
            for (int i = endIndex - 1; i >= startIndex && i >= minChunkSize; i--)
            {
                if (sentenceEndingsSet.Contains(buffer[i]))
                {
                    boundaryIndex = i + 1;
                    break;
                }
            }

            // If sentence boundary not found, look for word breaks      
            if (boundaryIndex == -1)
            {
                for (int i = endIndex - 1; i >= startIndex && i >= minChunkSize; i--)
                {
                    if (wordBreaksSet.Contains(buffer[i]) && i < maxChunkSize)
                    {
                        boundaryIndex = i + 1;
                        break;
                    }
                }
            }

            // Didn't find anything to use as a boundary - just take the whole buffer  
            boundaryIndex = boundaryIndex <= 0 ? endIndex : boundaryIndex;

            // Yield this section of the buffer  
            string textChunk = new string(buffer, 0, boundaryIndex).Trim();
            yield return textChunk;

            // Find overlap start without word truncation
            int overlapIndex = Math.Max(0, boundaryIndex - overlap);
            while (overlapIndex < boundaryIndex && !wordBreaksSet.Contains(buffer[overlapIndex]))
            {
                overlapIndex++;
            }

            // Shift the remaining bytes including overlap into the front of the buffer  
            int remainingBytes = endIndex - overlapIndex;
            if (remainingBytes > 0)
            {
                Array.Copy(buffer, overlapIndex, buffer, 0, remainingBytes);
                startIndex = remainingBytes;
            }
            else
            {
                startIndex = 0;
            }
        }
    }
}
