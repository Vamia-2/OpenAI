namespace AzureOpenAIServer.Models
{
    public class ChatResponse
    {
        public string Message { get; set; } = string.Empty;
        public string Role { get; set; } = "assistant";
        public int PromptTokens { get; set; }
        public int CompletionTokens { get; set; }
        public int TotalTokens { get; set; }
    }
}
