// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Azure.WebJobs.Extensions.OpenAI.Embeddings;
using System.Net;
using Xunit;

namespace Microsoft.Azure.WebJobs.Extensions.OpenAI.Tests.Embeddings;

public class EmbeddingsHelperTests
{
    [Fact]
    public void ResolveFilePath_WithoutConfiguredRoot_PreservesInput()
    {
        string input = Path.GetFullPath("document.txt");

        string result = EmbeddingsHelper.ResolveFilePath(input, allowedRoot: null);

        Assert.Equal(input, result);
    }

    [Fact]
    public void ResolveFilePath_WithConfiguredRoot_ResolvesRelativePath()
    {
        string root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

        string result = EmbeddingsHelper.ResolveFilePath(
            Path.Combine("documents", "document.txt"),
            root);

        Assert.Equal(
            Path.GetFullPath(Path.Combine(root, "documents", "document.txt")),
            result);
    }

    [Fact]
    public void ResolveFilePath_WithConfiguredRoot_RejectsTraversal()
    {
        string root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

        ArgumentException exception = Assert.Throws<ArgumentException>(
            () => EmbeddingsHelper.ResolveFilePath(
                Path.Combine("..", "document.txt"),
                root));

        Assert.Contains(EmbeddingsHelper.FilePathRootSettingName, exception.Message);
    }

    [Fact]
    public void ResolveFilePath_WithConfiguredRoot_RejectsRootedPath()
    {
        string root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        string input = Path.GetFullPath("document.txt");

        ArgumentException exception = Assert.Throws<ArgumentException>(
            () => EmbeddingsHelper.ResolveFilePath(input, root));

        Assert.Contains(EmbeddingsHelper.FilePathRootSettingName, exception.Message);
    }

    [Fact]
    public void ResolveFilePath_WithConfiguredRoot_RejectsLinkedRoot()
    {
        string testDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        string targetDirectory = Path.Combine(testDirectory, "target");
        string linkedRoot = Path.Combine(testDirectory, "root");
        Directory.CreateDirectory(targetDirectory);

        try
        {
            try
            {
                Directory.CreateSymbolicLink(linkedRoot, targetDirectory);
            }
            catch (Exception creationException) when (
                creationException is PlatformNotSupportedException or
                UnauthorizedAccessException or
                IOException)
            {
                return;
            }

            ArgumentException exception = Assert.Throws<ArgumentException>(
                () => EmbeddingsHelper.ResolveFilePath("document.txt", linkedRoot));

            Assert.Contains(EmbeddingsHelper.FilePathRootSettingName, exception.Message);
        }
        finally
        {
            if (Directory.Exists(linkedRoot))
            {
                Directory.Delete(linkedRoot);
            }

            Directory.Delete(testDirectory, recursive: true);
        }
    }

    [Fact]
    public void ValidateUrl_WithoutConfiguredOrigins_PreservesHttpsUrl()
    {
        const string input = "https://example.com/documents/file.txt";

        Uri result = EmbeddingsHelper.ValidateUrl(input, allowedOrigins: null);

        Assert.Equal(input, result.AbsoluteUri);
    }

    [Fact]
    public void ValidateUrl_WithoutConfiguredOrigins_RejectsHttpUrl()
    {
        Assert.Throws<ArgumentException>(
            () => EmbeddingsHelper.ValidateUrl(
                "http://example.com/documents/file.txt",
                allowedOrigins: null));
    }

    [Fact]
    public void ValidateUrl_WithConfiguredOrigins_AllowsMatchingOrigin()
    {
        const string input = "https://example.com/documents/file.txt";

        Uri result = EmbeddingsHelper.ValidateUrl(
            input,
            "https://example.com, https://contoso.example:8443");

        Assert.Equal(input, result.AbsoluteUri);
    }

    [Fact]
    public void ValidateUrl_WithConfiguredOrigins_RejectsDifferentOrigin()
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(
            () => EmbeddingsHelper.ValidateUrl(
                "https://other.example/documents/file.txt",
                "https://example.com"));

        Assert.Contains(EmbeddingsHelper.UrlAllowedOriginsSettingName, exception.Message);
    }

    [Fact]
    public void ValidateUrl_WithConfiguredOrigins_RejectsUserInformation()
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(
            () => EmbeddingsHelper.ValidateUrl(
                "https://user@example.com/documents/file.txt",
                "https://example.com"));

        Assert.Contains(EmbeddingsHelper.UrlAllowedOriginsSettingName, exception.Message);
    }

    [Theory]
    [InlineData("http://example.com")]
    [InlineData("not-a-url")]
    public void ValidateUrl_RejectsInvalidUrl(string input)
    {
        Assert.Throws<ArgumentException>(
            () => EmbeddingsHelper.ValidateUrl(input, "https://example.com"));
    }

    [Theory]
    [InlineData("http://example.com")]
    [InlineData("https://example.com/path")]
    [InlineData("https://example.com?query=value")]
    public void ValidateUrl_RejectsInvalidConfiguredOrigin(string allowedOrigin)
    {
        Assert.Throws<InvalidOperationException>(
            () => EmbeddingsHelper.ValidateUrl(
                "https://example.com/documents/file.txt",
                allowedOrigin));
    }

    [Fact]
    public async Task GetRestrictedUrlReader_TimesOutWhileReadingResponseBody()
    {
        using HttpClient client = new(new StubHttpMessageHandler(
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StreamContent(new BlockingReadStream())
            }));

        await Assert.ThrowsAsync<TimeoutException>(
            () => EmbeddingsHelper.GetRestrictedUrlReader(
                new Uri("https://example.com/document.txt"),
                client,
                TimeSpan.FromMilliseconds(50),
                CancellationToken.None));
    }

    sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        readonly HttpResponseMessage response;

        public StubHttpMessageHandler(HttpResponseMessage response)
        {
            this.response = response;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(this.response);
        }
    }

    sealed class BlockingReadStream : Stream
    {
        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length => throw new NotSupportedException();

        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        public override async Task<int> ReadAsync(
            byte[] buffer,
            int offset,
            int count,
            CancellationToken cancellationToken)
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            return 0;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }
    }
}
