using System.ComponentModel.DataAnnotations;

namespace AzureOpenAIServer.Models
{
    public class ChatMessageModel
    {
        [Required]
        public string Role { get; set; } = string.Empty;

        [Required]
        public string Content { get; set; } = string.Empty;
    }

    public class ChatRequest
    {
        [Required]
        [MinLength(1)]
        public List<ChatMessageModel> Messages { get; set; } = new();

        [Range(1, 4096)]
        public int MaxTokens { get; set; } = 1024;

        [Range(0.0, 2.0)]
        public double Temperature { get; set; } = 0.7;
    }
}
