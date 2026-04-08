using System.ComponentModel.DataAnnotations;

namespace AzureOpenAIServer.Models
{
    public class TextGenerationRequest
    {
        [Required]
        [StringLength(4000, MinimumLength = 1)]
        public string Prompt { get; set; } = string.Empty;

        [Range(1, 4096)]
        public int MaxTokens { get; set; } = 1024;

        [Range(0.0, 2.0)]
        public double Temperature { get; set; } = 0.7;
    }
}
