// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Azure.WebJobs.Extensions.OpenAI.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Azure.WebJobs.Extensions.OpenAI.Assistants;

class AssistantBindingConverter :
    IConverter<AssistantCreateAttribute, IAsyncCollector<AssistantCreateRequest>>,
    IConverter<AssistantPostAttribute, IAsyncCollector<AssistantPostRequest>>,
    IAsyncConverter<AssistantQueryAttribute, AssistantState>,
    IAsyncConverter<AssistantQueryAttribute, string>
{
    readonly IAssistantService assistantService;
    readonly ILogger logger;

    public AssistantBindingConverter(IAssistantService assistantService, ILoggerFactory loggerFactory)
    {
        this.assistantService = assistantService ?? throw new ArgumentNullException(nameof(assistantService));
        this.logger = loggerFactory.CreateLogger<AssistantBindingConverter>();
    }

    public IAsyncCollector<AssistantCreateRequest> Convert(AssistantCreateAttribute attribute)
    {
        return new AssistantCreateCollector(this.assistantService, this.logger);
    }

    public IAsyncCollector<AssistantPostRequest> Convert(AssistantPostAttribute input)
    {
        return new AssistantPostCollector(this.assistantService, this.logger, input);
    }

    public Task<AssistantState> ConvertAsync(
        AssistantQueryAttribute input,
        CancellationToken cancellationToken)
    {
        string timestampString = Uri.UnescapeDataString(input.TimestampUtc);
        if (!DateTime.TryParse(timestampString, out DateTime timestamp))
        {
            throw new ArgumentException($"Invalid timestamp '{timestampString}'");
        }

        return this.assistantService.GetStateAsync(input.Id, timestamp, cancellationToken);
    }

    async Task<string> IAsyncConverter<AssistantQueryAttribute, string>.ConvertAsync(
        AssistantQueryAttribute input,
        CancellationToken cancellationToken)
    {
        AssistantState state = await this.ConvertAsync(input, cancellationToken);
        return JsonConvert.SerializeObject(state);
    }

    internal AssistantCreateRequest ToAssistantCreateRequest(JObject json)
    {
        this.logger.LogDebug("Creating assistant request from JObject: {Text}", json);
        return json.ToObject<AssistantCreateRequest>() ?? throw new ArgumentException("Invalid assistant create request");
    }

    // Called by the host when processing binding requests from out-of-process workers.
    internal AssistantCreateRequest ToAssistantCreateRequest(string json)
    {
        this.logger.LogDebug("Creating assistant request from JSON string: {Text}", json);
        return JsonConvert.DeserializeObject<AssistantCreateRequest>(json) ?? throw new ArgumentException("Invalid assistant create request");
    }

    internal AssistantPostRequest ToAssistantPostRequest(JObject json)
    {
        this.logger.LogDebug("Creating assistant post request from JObject: {Text}", json);
        return json.ToObject<AssistantPostRequest>() ?? throw new ArgumentException("Invalid assistant post request");
    }

    // Called by the host when processing binding requests from out-of-process workers.
    internal AssistantPostRequest ToAssistantPostRequest(string json)
    {
        this.logger.LogDebug("Creating assistant post request from JSON string: {Text}", json);
        return JsonConvert.DeserializeObject<AssistantPostRequest>(json) ?? throw new ArgumentException("Invalid assistant post request");
    }

    class AssistantCreateCollector : IAsyncCollector<AssistantCreateRequest>
    {
        readonly IAssistantService chatService;
        readonly ILogger logger;

        public AssistantCreateCollector(IAssistantService chatService, ILogger logger)
        {
            this.chatService = chatService;
            this.logger = logger;
        }

        public async Task AddAsync(AssistantCreateRequest item, CancellationToken cancellationToken = default)
        {
            AssistantCreateRequest request = new(item.Id, item.Instructions);
            await this.chatService.CreateAssistantAsync(request, cancellationToken);
            this.logger.LogInformation("Created assistant '{Id}'", request.Id);
        }

        public Task FlushAsync(CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }

    class AssistantPostCollector : IAsyncCollector<AssistantPostRequest>
    {
        readonly IAssistantService chatService;
        readonly ILogger logger;
        readonly AssistantPostAttribute attribute;

        public AssistantPostCollector(IAssistantService chatService, ILogger logger, AssistantPostAttribute attribute)
        {
            this.chatService = chatService;
            this.logger = logger;
            this.attribute = attribute;
        }

        public Task AddAsync(AssistantPostRequest request, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(request.Id))
            {
                request.Id = this.attribute.Id;
            }

            request.Model = this.attribute.Model;

            this.logger.LogInformation("Posting message to assistant '{Id}': {Text}", request.Id, request.UserMessage);
            return this.chatService.PostMessageAsync(request, cancellationToken);
        }

        public Task FlushAsync(CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }
}