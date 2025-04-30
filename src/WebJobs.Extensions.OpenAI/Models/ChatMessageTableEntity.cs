// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Runtime.Serialization;
using System.Text.Json;
using Azure;
using Azure.Data.Tables;
using OpenAI.Chat;

namespace Microsoft.Azure.WebJobs.Extensions.OpenAI.Models;

/// <summary>
/// The ChatMessageTableEntity class represents each chat message to interact with table storage.
/// </summary>
class ChatMessageTableEntity : ITableEntity
{
    // WARNING: Changing this is a breaking change!
    internal const string RowKeyPrefix = "msg-";

    public ChatMessageTableEntity(
        string partitionKey,
        int messageIndex,
        string content,
        ChatMessageRole role,
        string? name = null,
        IEnumerable<ChatToolCall>? toolCalls = null)
    {
        this.PartitionKey = partitionKey;
        this.RowKey = GetRowKey(messageIndex);
        this.Content = content;
        this.Role = role.ToString();
        this.Name = name;
        this.CreatedAt = DateTime.UtcNow;
        this.ToolCalls = toolCalls?.ToList();
    }

    public ChatMessageTableEntity(TableEntity entity)
    {
        this.PartitionKey = entity.PartitionKey;
        this.RowKey = entity.RowKey;
        this.Timestamp = entity.Timestamp;
        this.ETag = entity.ETag;
        this.Content = entity.GetString(nameof(this.Content));
        this.Role = entity.GetString(nameof(this.Role));
        this.Name = entity.GetString(nameof(this.Name));
        this.CreatedAt = DateTime.SpecifyKind(entity.GetDateTime(nameof(this.CreatedAt)).GetValueOrDefault(), DateTimeKind.Utc);
        this.ToolCallsString = entity.GetString(nameof(this.ToolCalls));
    }

    /// <summary>
    /// Partition key.
    /// </summary>
    public string PartitionKey { get; set; }

    /// <summary>
    /// Row key.
    /// </summary>
    public string RowKey { get; set; }

    /// <summary>
    /// For chat messages, this is the chat content. For function calls, this is the function return value.
    /// </summary>
    public string Content { get; set; }

    /// <summary>
    /// Name of the function, if applicable.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Role of who sent message.
    /// </summary>
    public string Role { get; set; }

    /// <summary>
    /// Gets timestamp of table entity.
    /// </summary>
    public DateTimeOffset? Timestamp { get; set; }

    /// <summary>
    /// Gets ETag of table entity.
    /// </summary>
    public ETag ETag { get; set; }

    /// <summary>
    /// Gets when table entity was created at.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the ToolCalls for Assistant
    /// </summary>
    [IgnoreDataMember]
    public IList<ChatToolCall>? ToolCalls { get; set; }

    // WARNING: Changing this is a breaking change!
    static string GetRowKey(int messageNumber)
    {
        // Example msg-001B
        return string.Concat(RowKeyPrefix, messageNumber.ToString("X4"));
    }

    /// <summary>
    /// Converts the ToolCalls to a Json string for table storage
    /// </summary>
    [DataMember(Name = "ToolCalls")]
    public string ToolCallsString
    {
        get
        {
            if (this.ToolCalls == null || this.ToolCalls.Count == 0)
            {
                return string.Empty;
            }

            IList<ChatToolCallClone> cloneList = this.SerializeChatTool(this.ToolCalls);
            var options = new JsonSerializerOptions { WriteIndented = false, PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
            return JsonSerializer.Serialize(cloneList, options);
        }
        set
        {
            if (!string.IsNullOrEmpty(value))
            {
                JsonSerializerOptions options = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
                List<ChatToolCallClone>? cloneList = JsonSerializer.Deserialize<List<ChatToolCallClone>>(value, options);
                this.ToolCalls = cloneList != null ? this.DeserializeChatTool(cloneList) : new List<ChatToolCall>();
            }
            else
            {
                this.ToolCalls = new List<ChatToolCall>();
            }
        }
    }

    IList<ChatToolCallClone> SerializeChatTool(IList<ChatToolCall> toolCalls)
    {
        IList<ChatToolCallClone> chatToolCloneList = new List<ChatToolCallClone>();
        foreach (ChatToolCall toolCall in toolCalls)
        {
            ChatToolCallClone chatToolClone = new(toolCall.Id, toolCall.FunctionName, toolCall.FunctionArguments.ToString(), toolCall.Kind.ToString());
            chatToolCloneList.Add(chatToolClone);
        }
        return chatToolCloneList;
    }

    IList<ChatToolCall> DeserializeChatTool(IList<ChatToolCallClone> clones)
    {
        IList<ChatToolCall> result = new List<ChatToolCall>();
        foreach (ChatToolCallClone clone in clones)
        {
            JsonElement functionArgs = JsonDocument.Parse(clone.FunctionArguments).RootElement;
            ChatToolCall toolCall = ChatToolCall.CreateFunctionToolCall(clone.Id, clone.FunctionName, BinaryData.FromString(functionArgs.GetRawText()));
            result.Add(toolCall);
        }
        return result;
    }
}

class ChatToolCallClone
{
    public ChatToolCallClone()
    {
        this.Id = string.Empty;
        this.FunctionName = string.Empty;
        this.FunctionArguments = string.Empty;
        this.Kind = string.Empty;
    }

    internal ChatToolCallClone(string id, string functionName, string functionArguments, string kind)
    {
        this.Id = id;
        this.FunctionName = functionName;
        this.Kind = kind;
        this.FunctionArguments = functionArguments;
    }

    public string Id { get; set; }

    public string FunctionName { get; set; }

    public string FunctionArguments { get; set; }

    public string Kind { get; set; }
}