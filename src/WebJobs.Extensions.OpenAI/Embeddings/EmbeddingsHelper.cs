// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.ClientModel;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using OpenAI.Embeddings;

namespace Microsoft.Azure.WebJobs.Extensions.OpenAI.Embeddings;
static class EmbeddingsHelper
{
    static readonly char[] sentenceEndingsDefault = new[] { '.', '!', '?' };
    static readonly char[] wordBreaksDefault = new[] { ',', ';', ':', ' ', '(', ')', '[', ']', '{', '}', '\t', '\n' };
    static readonly string UserAgent = $"{typeof(OpenAIExtension).Namespace}/{FileVersionInfo.GetVersionInfo(typeof(OpenAIExtension).Assembly.Location).FileVersion}";
    static readonly HttpClient httpClient = new();

    static EmbeddingsHelper()
    {
        httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgent);
    }

    internal static async Task<EmbeddingsContext> GenerateEmbeddingsAsync(
        EmbeddingsBaseAttribute attribute,
        OpenAIClientFactory openAIClientFactory,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        List<string> chunks = await BuildRequest(attribute);

        logger.LogInformation("Sending OpenAI embeddings request");

        ClientResult<OpenAIEmbeddingCollection> response = await openAIClientFactory.GetEmbeddingClient(
            attribute.AIConnectionName,
            attribute.EmbeddingsModel).GenerateEmbeddingsAsync(chunks, cancellationToken: cancellationToken);

        logger.LogInformation("Received OpenAI embeddings count: {count}", response.Value.Count);

        return new EmbeddingsContext(chunks, response);
    }

    static async Task<List<string>> BuildRequest(EmbeddingsBaseAttribute attribute)
    {
        using TextReader reader = await GetTextReader(attribute.InputType, attribute.Input);
        if (attribute.MaxOverlap >= attribute.MaxChunkLength)
        {
            throw new ArgumentOutOfRangeException($"MaxOverlap ({attribute.MaxOverlap}) must be less than MaxChunkLength ({attribute.MaxChunkLength}).");
        }

        List<string> chunks = GetTextChunks(reader, 0, attribute.MaxChunkLength, attribute.MaxOverlap).ToList();
        return chunks;
    }

    static async Task<TextReader> GetTextReader(InputType inputType, string input)
    {
        if (inputType == InputType.RawText)
        {
            return new StringReader(input);
        }
        else if (inputType == InputType.FilePath)
        {
            return new StreamReader(input);
        }
        else if (inputType == InputType.Url)
        {
            if (!Uri.TryCreate(input, UriKind.Absolute, out Uri? uriResult) ||
                uriResult.Scheme != Uri.UriSchemeHttps)
            {
                throw new ArgumentException($"Invalid Url: {input}. Ensure it is a valid https Url.");
            }

            Stream stream = await httpClient.GetStreamAsync(input);
            return new StreamReader(stream);
        }
        else
        {
            throw new NotSupportedException($"InputType = '{inputType}' is not supported.");
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
