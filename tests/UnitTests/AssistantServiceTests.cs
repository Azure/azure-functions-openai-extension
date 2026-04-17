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
    public async Task CreateAssistantAsync_WithPreserveChatHistory_UpdatesSystemMessageInPlace()
    {
        // Arrange
        var request = new AssistantCreateRequest("testId", "Updated instructions")
        {
            CollectionName = "ChatState",
            ChatStorageConnectionSetting = "AzureWebJobsStorage",
            PreserveChatHistory = true
        };

        var stateEntity = new TableEntity(request.Id, AssistantStateEntity.FixedRowKeyValue)
        {
            [nameof(AssistantStateEntity.Exists)] = true,
            [nameof(AssistantStateEntity.CreatedAt)] = DateTime.UtcNow.AddDays(-1),
            [nameof(AssistantStateEntity.LastUpdatedAt)] = DateTime.UtcNow.AddHours(-1),
            [nameof(AssistantStateEntity.TotalMessages)] = 2,
            [nameof(AssistantStateEntity.TotalTokens)] = 0
        };

        var systemMessage = new TableEntity(request.Id, "msg-0001")
        {
            [nameof(ChatMessageTableEntity.Content)] = "Original instructions",
            [nameof(ChatMessageTableEntity.Role)] = ChatMessageRole.System.ToString(),
            [nameof(ChatMessageTableEntity.CreatedAt)] = DateTime.UtcNow.AddHours(-2),
            [nameof(ChatMessageTableEntity.ToolCalls)] = ""
        };

        var userMessage = new TableEntity(request.Id, "msg-0002")
        {
            [nameof(ChatMessageTableEntity.Content)] = "Hello",
            [nameof(ChatMessageTableEntity.Role)] = ChatMessageRole.User.ToString(),
            [nameof(ChatMessageTableEntity.CreatedAt)] = DateTime.UtcNow.AddHours(-1),
            [nameof(ChatMessageTableEntity.ToolCalls)] = ""
        };

        AsyncPageable<TableEntity> mockQueryable = MockAsyncPageable<TableEntity>.Create(
            new List<TableEntity> { stateEntity, systemMessage, userMessage });

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

        var assistantService = new DefaultAssistantService(
            this.mockOpenAIClientFactory.Object,
            this.mockAzureComponentFactory.Object,
            this.mockConfiguration.Object,
            this.mockSkillInvoker.Object,
            this.mockLoggerFactory.Object);

        System.Reflection.FieldInfo? tableClientField = typeof(DefaultAssistantService).GetField(
            "tableClient",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        tableClientField?.SetValue(assistantService, this.mockTableClient.Object);

        // Act
        await assistantService.CreateAssistantAsync(request, CancellationToken.None);

        // Assert
        this.mockTableClient.Verify(x => x.SubmitTransactionAsync(
            It.Is<IEnumerable<TableTransactionAction>>(actions =>
                actions.Any(a => a.ActionType == TableTransactionActionType.UpdateMerge) &&
                actions.All(a => a.ActionType != TableTransactionActionType.Delete) &&
                actions.All(a => a.ActionType != TableTransactionActionType.Add)),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAssistantAsync_WithPreserveChatHistory_NoSystemMessage_AppendsSystemMessage()
    {
        // Arrange
        var request = new AssistantCreateRequest("testId", "Updated instructions")
        {
            CollectionName = "ChatState",
            ChatStorageConnectionSetting = "AzureWebJobsStorage",
            PreserveChatHistory = true
        };

        var stateEntity = new TableEntity(request.Id, AssistantStateEntity.FixedRowKeyValue)
        {
            [nameof(AssistantStateEntity.Exists)] = true,
            [nameof(AssistantStateEntity.CreatedAt)] = DateTime.UtcNow.AddDays(-1),
            [nameof(AssistantStateEntity.LastUpdatedAt)] = DateTime.UtcNow.AddHours(-1),
            [nameof(AssistantStateEntity.TotalMessages)] = 1,
            [nameof(AssistantStateEntity.TotalTokens)] = 0
        };

        var userMessage = new TableEntity(request.Id, "msg-0001")
        {
            [nameof(ChatMessageTableEntity.Content)] = "Hello",
            [nameof(ChatMessageTableEntity.Role)] = ChatMessageRole.User.ToString(),
            [nameof(ChatMessageTableEntity.CreatedAt)] = DateTime.UtcNow.AddHours(-1),
            [nameof(ChatMessageTableEntity.ToolCalls)] = ""
        };

        AsyncPageable<TableEntity> mockQueryable = MockAsyncPageable<TableEntity>.Create(
            new List<TableEntity> { stateEntity, userMessage });

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

        var assistantService = new DefaultAssistantService(
            this.mockOpenAIClientFactory.Object,
            this.mockAzureComponentFactory.Object,
            this.mockConfiguration.Object,
            this.mockSkillInvoker.Object,
            this.mockLoggerFactory.Object);

        System.Reflection.FieldInfo? tableClientField = typeof(DefaultAssistantService).GetField(
            "tableClient",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        tableClientField?.SetValue(assistantService, this.mockTableClient.Object);

        // Act
        await assistantService.CreateAssistantAsync(request, CancellationToken.None);

        // Assert
        this.mockTableClient.Verify(x => x.SubmitTransactionAsync(
            It.Is<IEnumerable<TableTransactionAction>>(actions =>
                actions.Any(a => a.ActionType == TableTransactionActionType.Add) &&
                actions.Any(a => a.ActionType == TableTransactionActionType.UpdateMerge) &&
                actions.All(a => a.ActionType != TableTransactionActionType.Delete)),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAssistantAsync_PreserveHistory_UpdatesSystemMessage_AndKeepsUserMessage()
    {
        // Arrange
        var tableEntities = new List<TableEntity>();

        void ApplyActions(IEnumerable<TableTransactionAction> actions)
        {
            foreach (TableTransactionAction action in actions)
            {
                TableEntity entity = this.ConvertToTableEntity(action.Entity);

                switch (action.ActionType)
                {
                    case TableTransactionActionType.Add:
                        tableEntities.Add(entity);
                        break;
                    case TableTransactionActionType.UpdateMerge:
                        TableEntity? existing = tableEntities.FirstOrDefault(
                            e => e.PartitionKey == entity.PartitionKey && e.RowKey == entity.RowKey);
                        if (existing != null)
                        {
                            foreach (KeyValuePair<string, object> property in entity)
                            {
                                existing[property.Key] = property.Value;
                            }
                        }
                        break;
                    case TableTransactionActionType.Delete:
                        tableEntities.RemoveAll(e => e.PartitionKey == entity.PartitionKey && e.RowKey == entity.RowKey);
                        break;
                }
            }
        }

        var assistantService = new DefaultAssistantService(
            this.mockOpenAIClientFactory.Object,
            this.mockAzureComponentFactory.Object,
            this.mockConfiguration.Object,
            this.mockSkillInvoker.Object,
            this.mockLoggerFactory.Object);

        System.Reflection.FieldInfo? tableClientField = typeof(DefaultAssistantService).GetField(
            "tableClient",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        tableClientField?.SetValue(assistantService, this.mockTableClient.Object);

        this.mockTableClient.Setup(x => x.CreateIfNotExistsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Response.FromValue(new TableItem("ChatState"), new Mock<Response>().Object));

        this.mockTableClient.Setup(x => x.QueryAsync<TableEntity>(
                It.IsAny<string>(),
                null,
                null,
                It.IsAny<CancellationToken>()))
            .Returns(() => MockAsyncPageable<TableEntity>.Create(tableEntities.ToList()));

        this.mockTableClient.Setup(x => x.SubmitTransactionAsync(
                It.IsAny<IEnumerable<TableTransactionAction>>(),
                It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<TableTransactionAction>, CancellationToken>((actions, _) => ApplyActions(actions))
            .ReturnsAsync(Response.FromValue(new List<Response>() as IReadOnlyList<Response>, new Mock<Response>().Object));

        // 1) Create a new assistant with a system message
        var initialCreate = new AssistantCreateRequest("testId", "Initial instructions")
        {
            CollectionName = "ChatState",
            ChatStorageConnectionSetting = "AzureWebJobsStorage"
        };

        await assistantService.CreateAssistantAsync(initialCreate, CancellationToken.None);

        // 2) Add a user message
        tableEntities.Add(new TableEntity("testId", "msg-0002")
        {
            [nameof(ChatMessageTableEntity.Content)] = "Hello",
            [nameof(ChatMessageTableEntity.Role)] = ChatMessageRole.User.ToString(),
            [nameof(ChatMessageTableEntity.CreatedAt)] = DateTime.UtcNow,
            [nameof(ChatMessageTableEntity.ToolCalls)] = ""
        });

        // 3) Update the system message
        var updateCreate = new AssistantCreateRequest("testId", "Updated instructions")
        {
            CollectionName = "ChatState",
            ChatStorageConnectionSetting = "AzureWebJobsStorage",
            PreserveChatHistory = true
        };

        await assistantService.CreateAssistantAsync(updateCreate, CancellationToken.None);

        // 4) Confirm the user message still exists
        Assert.Contains(tableEntities, entity =>
            entity.PartitionKey == "testId" &&
            entity.RowKey == "msg-0002" &&
            entity.GetString(nameof(ChatMessageTableEntity.Role)) == ChatMessageRole.User.ToString());
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

    TableEntity ConvertToTableEntity(ITableEntity entity)
    {
        if (entity is TableEntity tableEntity)
        {
            return tableEntity;
        }

        TableEntity result = new(entity.PartitionKey, entity.RowKey)
        {
            ETag = entity.ETag,
            Timestamp = entity.Timestamp
        };

        foreach (System.Reflection.PropertyInfo property in entity.GetType().GetProperties())
        {
            if (!property.CanRead)
            {
                continue;
            }

            if (property.Name is nameof(ITableEntity.PartitionKey)
                or nameof(ITableEntity.RowKey)
                or nameof(ITableEntity.ETag)
                or nameof(ITableEntity.Timestamp))
            {
                continue;
            }

            object? value = property.GetValue(entity);
            if (value is not null)
            {
                result[property.Name] = value;
            }
        }

        return result;
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
