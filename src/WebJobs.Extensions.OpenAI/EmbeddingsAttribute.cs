// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Azure.WebJobs.Description;
using OpenAI.ObjectModels.RequestModels;

namespace WebJobs.Extensions.OpenAI;

/// <summary>
/// Input binding attribute for converting function trigger input into OpenAI embeddings.
/// </summary>
/// <remarks>
/// More information on OpenAI embeddings can be found at
/// https://platform.openai.com/docs/guides/embeddings/what-are-embeddings.
/// </remarks>
[Binding]
[AttributeUsage(AttributeTargets.Parameter)]
public sealed class EmbeddingsAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EmbeddingsAttribute"/> class with the specified input.
    /// </summary>
    /// <param name="input">The input source containing the data to generate embeddings for.</param>
    /// <param name="inputType">The type of the input.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="input"/> is <c>null</c>.</exception>
    public EmbeddingsAttribute(string input, InputType inputType)
    {
        this.Input = input ?? throw new ArgumentNullException(nameof(input));
        this.InputType = inputType;
    }

    /// <summary>
    /// Gets or sets the ID of the model to use.
    /// </summary>
    [AutoResolve]
    public string Model { get; set; } = "text-embedding-ada-002";

    /// <summary>
    /// Gets or sets the maximum number of characters to chunk the input into.
    /// </summary>
    /// <remarks>
    /// <para>
    /// At the time of writing, the maximum input tokens allowed for second-generation input embedding models
    /// like <c>text-embedding-ada-002</c> is 8191. 1 token is ~4 chars in English, which translates to roughly 32K 
    /// characters of English input that can fit into a single chunk.
    /// </para>
    /// </remarks>
    public int MaxChunkLength { get; set; } = 8 * 1024; // REVIEW: Is 8K a good default?

    /// <summary>
    /// Gets the input to generate embeddings for.
    /// </summary>
    [AutoResolve]
    public string Input { get; }

    /// <summary>
    /// Gets the type of the input.
    /// </summary>
    public InputType InputType { get; }

    /// <summary>
    /// Gets or sets a value indicating whether the binding should throw if there is an error calling the OpenAI
    /// endpoint.
    /// </summary>
    /// <remarks>
    /// The default value is <c>true</c>. Set this to <c>false</c> to handle errors manually in the function code.
    /// </remarks>
    public bool ThrowOnError { get; set; } = true;

    internal EmbeddingCreateRequest BuildRequest()
    {
        using TextReader reader = this.GetTextReader();
        List<string> chunks = GetTextChunks(reader, 0, this.MaxChunkLength).ToList();
        return new EmbeddingCreateRequest { Model = this.Model, InputAsList = chunks };
    }

    TextReader GetTextReader()
    {
        if (this.InputType == InputType.RawText)
        {
            return new StringReader(this.Input);
        }
        else if (this.InputType == InputType.FilePath)
        {
            return new StreamReader(this.Input);
        }
        else
        {
            throw new NotSupportedException($"InputType = '{this.InputType}' is not supported.");
        }
    }

    public static IEnumerable<string> GetTextChunks(
        TextReader reader,
        int minChunkSize,
        int maxChunkSize,
        char[]? terminatorChars = null)
    {
        char[] buffer = new char[maxChunkSize];
        int startIndex = 0;

        if (terminatorChars == null)
        {
            terminatorChars = new[] { '\n' };
        }

        int bytesRead;
        while ((bytesRead = reader.Read(buffer, startIndex, maxChunkSize - startIndex)) > 0)
        {
            int endIndex = startIndex + bytesRead;
            int boundaryIndex = -1;

            // Search backwards to end the chunk with a terminator character
            for (int i = endIndex - 1; i >= startIndex && i >= minChunkSize; i--)
            {
                char c = buffer[i];
                if (Array.IndexOf(terminatorChars, c, 0) >= 0)
                {
                    boundaryIndex = i + 1;
                    break;
                }
            }

            // Didn't find anything to use as a boundary - just take the whole buffer
            if (boundaryIndex <= 0)
            {
                boundaryIndex = endIndex;
            }

            // Yield this section of the buffer
            string textChunk = new string(buffer, 0, boundaryIndex).Trim();
            yield return textChunk;

            if (boundaryIndex == endIndex)
            {
                // all bytes were yielded
                startIndex = 0;
            }
            else
            {
                // shift the remaining bytes into the front of the buffer
                int remainingBytes = endIndex - boundaryIndex;
                if (remainingBytes > 0)
                {
                    Array.Copy(buffer, boundaryIndex, buffer, 0, remainingBytes);
                    startIndex = remainingBytes;
                }
            }
        }
    }
}
