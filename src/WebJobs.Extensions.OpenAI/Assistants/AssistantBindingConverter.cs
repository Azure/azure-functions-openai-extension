// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Azure.WebJobs.Extensions.OpenAI.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Azure.WebJobs.Extensions.OpenAI.Assistants;

class AssistantBindingConverter :
    IConverter<AssistantCreateAttribute, IAsyncCollector<AssistantCreateRequest>>,
    IAsyncConverter<AssistantPostAttribute, AssistantState>,
    IAsyncConverter<AssistantPostAttribute, string>,
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

    public Task<AssistantState> ConvertAsync(
        AssistantQueryAttribute input,
        CancellationToken cancellationToken)
    {
        return this.assistantService.GetStateAsync(input, cancellationToken);
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

    async Task<string> IAsyncConverter<AssistantPostAttribute, string>.ConvertAsync(AssistantPostAttribute input, CancellationToken cancellationToken)
    {
        AssistantState state = await this.ConvertAsync(input, cancellationToken);
        return JsonConvert.SerializeObject(state);
    }

    public Task<AssistantState> ConvertAsync(AssistantPostAttribute input, CancellationToken cancellationToken)
    {
        this.logger.LogInformation("Posting message to assistant '{Id}': {Text}", input.Id, input.UserMessage);
        return this.assistantService.PostMessageAsync(input, cancellationToken);
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
            await this.chatService.CreateAssistantAsync(item, cancellationToken);
            this.logger.LogInformation("Created assistant '{Id}'", item.Id);
        }

        public Task FlushAsync(CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }
}