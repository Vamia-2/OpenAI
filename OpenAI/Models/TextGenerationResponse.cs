namespace AzureOpenAIServer.Models
{
    public class TextGenerationResponse
    {
        public string Text { get; set; } = string.Empty;
        public int PromptTokens { get; set; }
        public int CompletionTokens { get; set; }
        public int TotalTokens { get; set; }
    }
}
