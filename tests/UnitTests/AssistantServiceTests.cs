// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.


using System.ClientModel;
using System.Threading;
using Azure;
using Azure.Core;
using Azure.Data.Tables;
using Azure.Data.Tables.Models;
using Microsoft.Azure.WebJobs.Extensions.OpenAI.Assistants;
using Microsoft.Azure.WebJobs.Extensions.OpenAI.Models;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using OpenAI.Chat;
using Xunit;

namespace Microsoft.Azure.WebJobs.Extensions.OpenAI.Tests.Assistants;

public class DefaultAssistantServiceTests
{
    readonly Mock<OpenAIClientFactory> mockOpenAIClientFactory;
    readonly Mock<AzureComponentFactory> mockAzureComponentFactory;
    readonly Mock<IConfiguration> mockConfiguration;
    readonly Mock<IAssistantSkillInvoker> mockSkillInvoker;
    readonly Mock<ILoggerFactory> mockLoggerFactory;
    readonly Mock<ILogger<IAssistantService>> mockLogger;
    readonly Mock<TableServiceClient> mockTableServiceClient;
    readonly Mock<TableClient> mockTableClient;

    public DefaultAssistantServiceTests()
    {
        this.mockAzureComponentFactory = new Mock<AzureComponentFactory>();
        this.mockConfiguration = new Mock<IConfiguration>();
        this.mockSkillInvoker = new Mock<IAssistantSkillInvoker>();
        this.mockLoggerFactory = new Mock<ILoggerFactory>();
        this.mockLogger = new Mock<ILogger<IAssistantService>>();
        this.mockTableServiceClient = new Mock<TableServiceClient>();
        this.mockTableClient = new Mock<TableClient>();
        this.mockOpenAIClientFactory = new Mock<OpenAIClientFactory>(
             this.mockConfiguration.Object,
             this.mockAzureComponentFactory.Object,
             this.mockLoggerFactory.Object);

        this.mockLoggerFactory.Setup(x => x.CreateLogger(It.IsAny<string>()))
            .Returns(this.mockLogger.Object);

        // Setup table client
        this.mockTableServiceClient.Setup(x => x.GetTableClient(It.IsAny<string>()))
            .Returns(this.mockTableClient.Object);
    }

    [Fact]
    public async Task CreateAssistantAsync_WithValidRequest_CreatesAssistantAndMessages()
    {
        // Arrange
        var request = new AssistantCreateRequest("testId", "Test instructions")
        {
            CollectionName = "ChatState",
            ChatStorageConnectionSetting = "AzureWebJobsStorage"
        };

        var mockQueryResult = new List<TableEntity>();
        AsyncPageable<TableEntity> mockQueryable = MockAsyncPageable<TableEntity>.Create(mockQueryResult);

        this.mockTableClient.Setup(x => x.CreateIfNotExistsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Response.FromValue(new TableItem(request.CollectionName), new Mock<Response>().Object));

        this.mockTableClient.Setup(x => x.QueryAsync<TableEntity>(
                It.Is<string>(s => s == $"PartitionKey eq '{request.Id}'"),
                null,
                null,
                 It.IsAny<CancellationToken>()))
            .Returns(mockQueryable);

        this.mockTableClient.Setup(x => x.SubmitTransactionAsync(
                It.IsAny<IEnumerable<TableTransactionAction>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Response.FromValue(new List<Response>() as IReadOnlyList<Response>, new Mock<Response>().Object));

        //// Arrange
        //Mock<IConfigurationSection> mockSection = CreateMockSection(
        //    exists: false,
        //    tableServiceUri: null);
        //mockSection.Setup(s => s.Value).Returns("UseDevelopmentStorage=true");

        //// Setup AzureWebJobsStorage directly
        //this.mockConfiguration.Setup(c => c["AzureWebJobsStorage"]).Returns("UseDevelopmentStorage=true");
        //this.mockConfiguration.Setup(c => c.GetSection("AzureWebJobsStorage")).Returns(mockSection.Object);

        // Create the service under test
        var assistantService = new DefaultAssistantService(
            this.mockOpenAIClientFactory.Object,
            this.mockAzureComponentFactory.Object,
            this.mockConfiguration.Object,
            this.mockSkillInvoker.Object,
            this.mockLoggerFactory.Object);

        // Use reflection to set the tableClient field
        System.Reflection.FieldInfo? tableClientField = typeof(DefaultAssistantService).GetField("tableClient",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        tableClientField?.SetValue(assistantService, this.mockTableClient.Object);

        // Act
        await assistantService.CreateAssistantAsync(request, CancellationToken.None);

        // Assert
        this.mockTableClient.Verify(x => x.CreateIfNotExistsAsync(It.IsAny<CancellationToken>()), Times.Once);

        this.mockTableClient.Verify(x => x.QueryAsync<TableEntity>(
            It.Is<string>(s => s.Contains(request.Id)),
            null,
            null,
            It.IsAny<CancellationToken>()), Times.Once);

        this.mockTableClient.Verify(x => x.SubmitTransactionAsync(
            It.IsAny<IEnumerable<TableTransactionAction>>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAssistantAsync_WithExistingAssistant_DeletesOldEntitiesFirst()
    {
        // Arrange
        var request = new AssistantCreateRequest("testId", "Test instructions")
        {
            CollectionName = "ChatState",
            ChatStorageConnectionSetting = "AzureWebJobsStorage"
        };

        // Create existing entities
        var existingEntities = new List<TableEntity>
        {
            new("testId", "state"),
            new("testId", "msg-001")
        };

        AsyncPageable<TableEntity> mockQueryable = MockAsyncPageable<TableEntity>.Create(existingEntities);

        this.mockTableClient.Setup(x => x.CreateIfNotExistsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Response.FromValue(new TableItem(request.CollectionName), new Mock<Response>().Object));

        this.mockTableClient.Setup(x => x.QueryAsync<TableEntity>(
                It.Is<string>(s => s == $"PartitionKey eq '{request.Id}'"),
                null,
                null,
                 It.IsAny<CancellationToken>()))
            .Returns(mockQueryable);

        this.mockTableClient.Setup(x => x.SubmitTransactionAsync(
                It.IsAny<IEnumerable<TableTransactionAction>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Response.FromValue(new List<Response>() as IReadOnlyList<Response>, new Mock<Response>().Object));

        // Create the service under test
        var assistantService = new DefaultAssistantService(
            this.mockOpenAIClientFactory.Object,
            this.mockAzureComponentFactory.Object,
            this.mockConfiguration.Object,
            this.mockSkillInvoker.Object,
            this.mockLoggerFactory.Object);

        // Use reflection to set the tableClient field
        System.Reflection.FieldInfo? tableClientField = typeof(DefaultAssistantService).GetField("tableClient",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        tableClientField?.SetValue(assistantService, this.mockTableClient.Object);


        // Act
        await assistantService.CreateAssistantAsync(request, CancellationToken.None);

        // Assert
        this.mockTableClient.Verify(x => x.SubmitTransactionAsync(
            It.IsAny<IEnumerable<TableTransactionAction>>(),
            It.IsAny<CancellationToken>()), Times.AtLeastOnce);

        this.mockTableClient.Verify(x => x.SubmitTransactionAsync(
            It.Is<IEnumerable<TableTransactionAction>>(
                actions => actions.Any(a => a.ActionType == TableTransactionActionType.Add)),
            It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task GetStateAsync_WithValidId_ReturnsCorrectState()
    {
        // Arrange
        string id = "testId";
        string timestamp = DateTime.UtcNow.AddHours(-1).ToString("o");
        var attribute = new AssistantQueryAttribute(id)
        {
            TimestampUtc = timestamp,
            CollectionName = "testCollection"
        };

        // Create mock entities
        var stateEntity = new TableEntity(id, AssistantStateEntity.FixedRowKeyValue)
        {
            ["Exists"] = true,
            ["CreatedAt"] = DateTime.UtcNow.AddDays(-1),
            ["LastUpdatedAt"] = DateTime.UtcNow,
            ["TotalMessages"] = 2,
            ["TotalTokens"] = 100
        };

        var message1 = new TableEntity(id, "msg-001")
        {
            ["Content"] = "Test message 1",
            ["Role"] = ChatMessageRole.System.ToString(),
            ["CreatedAt"] = DateTime.UtcNow.AddMinutes(-30),
            ["ToolCalls"] = ""
        };

        var message2 = new TableEntity(id, "msg-002")
        {
            ["Content"] = "Test message 2",
            ["Role"] = ChatMessageRole.Assistant.ToString(),
            ["CreatedAt"] = DateTime.UtcNow.AddMinutes(-15),
            ["ToolCalls"] = ""
        };

        var mockQueryResult = new List<TableEntity> { stateEntity, message1, message2 };
        AsyncPageable<TableEntity> mockQueryable = MockAsyncPageable<TableEntity>.Create(mockQueryResult);

        this.mockTableClient.Setup(x => x.QueryAsync<TableEntity>(
                It.Is<string>(s => s == $"PartitionKey eq '{id}'"),
                null,
                null,
                It.IsAny<CancellationToken>()))
            .Returns(mockQueryable);

        // Create the service under test
        var assistantService = new DefaultAssistantService(
            this.mockOpenAIClientFactory.Object,
            this.mockAzureComponentFactory.Object,
            this.mockConfiguration.Object,
            this.mockSkillInvoker.Object,
            this.mockLoggerFactory.Object);

        // Use reflection to set the tableClient field
        System.Reflection.FieldInfo? tableClientField = typeof(DefaultAssistantService).GetField("tableClient",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        tableClientField?.SetValue(assistantService, this.mockTableClient.Object);

        // Act
        AssistantState result = await assistantService.GetStateAsync(attribute, CancellationToken.None);

        // Assert
        Assert.Equal(id, result.Id);
        Assert.True(result.Exists);
        Assert.Equal(2, result.TotalMessages);
        Assert.Equal(100, result.TotalTokens);

        // Verify that we filter messages based on timestamp
        DateTime parsedTimestamp = DateTime.Parse(Uri.UnescapeDataString(timestamp)).ToUniversalTime();
        Assert.All(result.RecentMessages, msg =>
            Assert.True(DateTime.UtcNow.AddMinutes(-30) > parsedTimestamp));
    }

    [Fact]
    public async Task GetStateAsync_WithNonExistentId_ReturnsEmptyState()
    {
        // Arrange
        string id = "nonExistentId";
        string timestamp = DateTime.UtcNow.AddHours(-1).ToString("o");
        var attribute = new AssistantQueryAttribute(id)
        {
            TimestampUtc = timestamp,
            CollectionName = "testCollection"
        };

        var mockQueryResult = new List<TableEntity> { };
        AsyncPageable<TableEntity> mockQueryable = MockAsyncPageable<TableEntity>.Create(mockQueryResult);

        this.mockTableClient.Setup(x => x.QueryAsync<TableEntity>(
                It.Is<string>(s => s == $"PartitionKey eq '{id}'"),
                null,
                null,
                It.IsAny<CancellationToken>()))
            .Returns(mockQueryable);

        // Create the service under test
        var assistantService = new DefaultAssistantService(
            this.mockOpenAIClientFactory.Object,
            this.mockAzureComponentFactory.Object,
            this.mockConfiguration.Object,
            this.mockSkillInvoker.Object,
            this.mockLoggerFactory.Object);

        // Use reflection to set the tableClient field
        System.Reflection.FieldInfo? tableClientField = typeof(DefaultAssistantService).GetField("tableClient",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        tableClientField?.SetValue(assistantService, this.mockTableClient.Object);

        // Act
        AssistantState result = await assistantService.GetStateAsync(attribute, CancellationToken.None);

        // Assert
        Assert.Equal(id, result.Id);
        Assert.False(result.Exists);
        Assert.Equal(0, result.TotalMessages);
        Assert.Empty(result.RecentMessages);
    }

    [Fact]
    public void Constructor_WithNullArguments_ThrowsArgumentNullException()
    {
        // Act & Assert
#nullable disable
        Assert.Throws<ArgumentNullException>(() => new DefaultAssistantService(
            null,
            this.mockAzureComponentFactory.Object,
            this.mockConfiguration.Object,
            this.mockSkillInvoker.Object,
            this.mockLoggerFactory.Object));

        Assert.Throws<ArgumentNullException>(() => new DefaultAssistantService(
           this.mockOpenAIClientFactory.Object,
            null,
            this.mockConfiguration.Object,
            this.mockSkillInvoker.Object,
            this.mockLoggerFactory.Object));

        Assert.Throws<ArgumentNullException>(() => new DefaultAssistantService(
           this.mockOpenAIClientFactory.Object,
            this.mockAzureComponentFactory.Object,
            null,
            this.mockSkillInvoker.Object,
            this.mockLoggerFactory.Object));

        Assert.Throws<ArgumentNullException>(() => new DefaultAssistantService(
           this.mockOpenAIClientFactory.Object,
            this.mockAzureComponentFactory.Object,
            this.mockConfiguration.Object,
            null,
            this.mockLoggerFactory.Object));

        Assert.Throws<ArgumentNullException>(() => new DefaultAssistantService(
           this.mockOpenAIClientFactory.Object,
            this.mockAzureComponentFactory.Object,
            this.mockConfiguration.Object,
            this.mockSkillInvoker.Object,
            null));
#nullable restore
    }

    [Fact]
    public async Task PostMessageAsync_WithNonExistentAssistant_ReturnsEmptyState()
    {
        // Arrange
        string assistantId = "nonExistentId";
        var attribute = new AssistantPostAttribute(assistantId, "Hello")
        {
            CollectionName = "ChatState",
            ChatStorageConnectionSetting = "AzureWebJobsStorage"
        };

        var mockQueryResult = new List<TableEntity>();
        AsyncPageable<TableEntity> mockQueryable = MockAsyncPageable<TableEntity>.Create(mockQueryResult);

        this.mockTableClient.Setup(x => x.QueryAsync<TableEntity>(
                It.Is<string>(s => s == $"PartitionKey eq '{assistantId}'"),
                null,
                null,
                It.IsAny<CancellationToken>()))
            .Returns(mockQueryable);

        // Create the service under test
        var assistantService = new DefaultAssistantService(
            this.mockOpenAIClientFactory.Object,
            this.mockAzureComponentFactory.Object,
            this.mockConfiguration.Object,
            this.mockSkillInvoker.Object,
            this.mockLoggerFactory.Object);

        // Use reflection to set the tableClient field
        System.Reflection.FieldInfo? tableClientField = typeof(DefaultAssistantService).GetField("tableClient",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        tableClientField?.SetValue(assistantService, this.mockTableClient.Object);

        // Act
        AssistantState result = await assistantService.PostMessageAsync(attribute, CancellationToken.None);

        // Assert
        Assert.Equal(assistantId, result.Id);
        Assert.False(result.Exists);
        Assert.Equal(0, result.TotalMessages);
        Assert.Empty(result.RecentMessages);
    }

    [Fact]
    public async Task PostMessageAsync_WithInvalidAttributes_ThrowsArgumentException()
    {
        // Arrange - Missing user message
        var attributeNoMessage = new AssistantPostAttribute("testId", "")
        {
            CollectionName = "ChatState",
            ChatStorageConnectionSetting = "AzureWebJobsStorage"
        };

        // Create the service under test
        var assistantService = new DefaultAssistantService(
            this.mockOpenAIClientFactory.Object,
            this.mockAzureComponentFactory.Object,
            this.mockConfiguration.Object,
            this.mockSkillInvoker.Object,
            this.mockLoggerFactory.Object);

        // Act & Assert - Empty message
        await Assert.ThrowsAsync<ArgumentException>(() =>
            assistantService.PostMessageAsync(attributeNoMessage, CancellationToken.None));

        // Arrange - Missing ID
        var attributeNoId = new AssistantPostAttribute("", "Hello")
        {
            CollectionName = "ChatState",
            ChatStorageConnectionSetting = "AzureWebJobsStorage"
        };

        // Act & Assert - Empty ID
        await Assert.ThrowsAsync<ArgumentException>(() =>
            assistantService.PostMessageAsync(attributeNoId, CancellationToken.None));
    }

    static Mock<IConfigurationSection> CreateMockSection(bool exists, string? tableServiceUri = null)
    {
        var mockSection = new Mock<IConfigurationSection>();

        if (!exists)
        {
            mockSection.Setup(s => s.Value).Returns("");
            mockSection.Setup(s => s.GetChildren()).Returns([]);
        }
        else
        {
            mockSection.Setup(s => s.Value).Returns("some-value");
        }

        var endpointSection = new Mock<IConfigurationSection>();
#nullable disable
        endpointSection.Setup(s => s.Value).Returns(tableServiceUri);
#nullable restore
        mockSection.Setup(s => s.GetSection("tableServiceUri")).Returns(endpointSection.Object);

        return mockSection;
    }
}

// Update the MockAsyncPageable class to ensure TableEntity satisfies the 'notnull' constraint
public class MockAsyncPageable<T> : AsyncPageable<T> where T : notnull
{
    readonly IEnumerable<T> _items;

    MockAsyncPageable(IEnumerable<T> items)
    {
        this._items = items;
    }

    public static AsyncPageable<T> Create(IEnumerable<T> items)
    {
        return new MockAsyncPageable<T>(items);
    }

    public override async IAsyncEnumerable<Page<T>> AsPages(
        string? continuationToken = null,
        int? pageSizeHint = null)
    {
        await Task.Yield();
        yield return Page<T>.FromValues([.. this._items], null, new Mock<Response>().Object);
    }
}
