using Azure;
using Azure.AI.OpenAI;
using global::OpenAI.Chat;
using AzureOpenAIServer.Models;
using System.Runtime.CompilerServices;

namespace AzureOpenAIServer.Services
{
    public class AzureOpenAIService : IAzureOpenAIService
    {
        private readonly ChatClient _chatClient;
        private readonly ILogger<AzureOpenAIService> _logger;

        public AzureOpenAIService(IConfiguration configuration, ILogger<AzureOpenAIService> logger)
        {
            _logger = logger;

            var endpoint = configuration["AzureOpenAI:Endpoint"]
                ?? throw new InvalidOperationException("AzureOpenAI:Endpoint configuration is missing.");
            var apiKey = configuration["AzureOpenAI:ApiKey"]
                ?? throw new InvalidOperationException("AzureOpenAI:ApiKey configuration is missing.");
            var deploymentName = configuration["AzureOpenAI:DeploymentName"]
                ?? throw new InvalidOperationException("AzureOpenAI:DeploymentName configuration is missing.");

            var azureClient = new AzureOpenAIClient(new Uri(endpoint), new AzureKeyCredential(apiKey));
            _chatClient = azureClient.GetChatClient(deploymentName);
        }

        public async Task<TextGenerationResponse> GenerateTextAsync(
            TextGenerationRequest request,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Generating text for prompt of length {Length}", request.Prompt.Length);

            var messages = new List<ChatMessage>
            {
                new UserChatMessage(request.Prompt)
            };

            var options = new ChatCompletionOptions
            {
                MaxOutputTokenCount = request.MaxTokens,
                Temperature = (float)request.Temperature
            };

            var completion = await _chatClient.CompleteChatAsync(messages, options, cancellationToken);

            var result = completion.Value;
            var text = result.Content.Count > 0 ? result.Content[0].Text : string.Empty;

            return new TextGenerationResponse
            {
                Text = text,
                PromptTokens = result.Usage?.InputTokenCount ?? 0,
                CompletionTokens = result.Usage?.OutputTokenCount ?? 0,
                TotalTokens = result.Usage?.TotalTokenCount ?? 0
            };
        }

        public async Task<ChatResponse> ChatAsync(
            ChatRequest request,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Processing chat with {Count} messages", request.Messages.Count);

            var messages = BuildChatMessages(request.Messages);

            var options = new ChatCompletionOptions
            {
                MaxOutputTokenCount = request.MaxTokens,
                Temperature = (float)request.Temperature
            };

            var completion = await _chatClient.CompleteChatAsync(messages, options, cancellationToken);

            var result = completion.Value;
            var text = result.Content.Count > 0 ? result.Content[0].Text : string.Empty;

            return new ChatResponse
            {
                Message = text,
                Role = result.Role.ToString().ToLowerInvariant(),
                PromptTokens = result.Usage?.InputTokenCount ?? 0,
                CompletionTokens = result.Usage?.OutputTokenCount ?? 0,
                TotalTokens = result.Usage?.TotalTokenCount ?? 0
            };
        }

        public async IAsyncEnumerable<string> ChatStreamAsync(
            ChatRequest request,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Streaming chat with {Count} messages", request.Messages.Count);

            var messages = BuildChatMessages(request.Messages);

            var options = new ChatCompletionOptions
            {
                MaxOutputTokenCount = request.MaxTokens,
                Temperature = (float)request.Temperature
            };

            await foreach (var update in _chatClient.CompleteChatStreamingAsync(messages, options, cancellationToken))
            {
                foreach (var part in update.ContentUpdate)
                {
                    if (!string.IsNullOrEmpty(part.Text))
                    {
                        yield return part.Text;
                    }
                }
            }
        }

        private static List<ChatMessage> BuildChatMessages(List<ChatMessageModel> messages)
        {
            var result = new List<ChatMessage>();
            foreach (var msg in messages)
            {
                result.Add(msg.Role.ToLowerInvariant() switch
                {
                    "system" => new SystemChatMessage(msg.Content),
                    "assistant" => new AssistantChatMessage(msg.Content),
                    _ => new UserChatMessage(msg.Content)
                });
            }
            return result;
        }
    }
}
