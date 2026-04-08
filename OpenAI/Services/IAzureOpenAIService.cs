using AzureOpenAIServer.Models;

namespace AzureOpenAIServer.Services
{
    public interface IAzureOpenAIService
    {
        Task<TextGenerationResponse> GenerateTextAsync(TextGenerationRequest request, CancellationToken cancellationToken = default);
        Task<ChatResponse> ChatAsync(ChatRequest request, CancellationToken cancellationToken = default);
        IAsyncEnumerable<string> ChatStreamAsync(ChatRequest request, CancellationToken cancellationToken = default);
    }
}
