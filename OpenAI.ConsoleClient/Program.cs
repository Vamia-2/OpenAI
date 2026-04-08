using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AzureOpenAIClient
{
    internal class ChatMessageDto
    {
        [JsonPropertyName("role")]
        public string Role { get; set; } = string.Empty;

        [JsonPropertyName("content")]
        public string Content { get; set; } = string.Empty;
    }

    internal class ChatRequestDto
    {
        [JsonPropertyName("messages")]
        public List<ChatMessageDto> Messages { get; set; } = new();

        [JsonPropertyName("maxTokens")]
        public int MaxTokens { get; set; } = 1024;

        [JsonPropertyName("temperature")]
        public double Temperature { get; set; } = 0.7;
    }

    internal class ChatResponseDto
    {
        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        [JsonPropertyName("role")]
        public string Role { get; set; } = string.Empty;

        [JsonPropertyName("promptTokens")]
        public int PromptTokens { get; set; }

        [JsonPropertyName("completionTokens")]
        public int CompletionTokens { get; set; }

        [JsonPropertyName("totalTokens")]
        public int TotalTokens { get; set; }
    }

    internal class Program
    {
        private static readonly HttpClient _httpClient = new HttpClient();
        private static string _baseUrl = "http://localhost:5000";

        static async Task Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("╔══════════════════════════════════════╗");
            Console.WriteLine("║     Azure OpenAI Console Chat        ║");
            Console.WriteLine("╚══════════════════════════════════════╝");
            Console.ResetColor();

            // Allow overriding the base URL via environment variable or argument
            if (args.Length > 0)
            {
                _baseUrl = args[0].TrimEnd('/');
            }
            else if (Environment.GetEnvironmentVariable("OPENAI_API_URL") is { Length: > 0 } envUrl)
            {
                _baseUrl = envUrl.TrimEnd('/');
            }

            Console.WriteLine($"Connecting to: {_baseUrl}");
            Console.WriteLine("Type 'exit' or 'quit' to close. Type 'clear' to reset conversation.");
            Console.WriteLine("Type 'stream' to toggle streaming mode.");
            Console.WriteLine();

            _httpClient.BaseAddress = new Uri(_baseUrl);
            _httpClient.Timeout = TimeSpan.FromSeconds(120);

            var conversationHistory = new List<ChatMessageDto>();
            bool useStreaming = false;

            while (true)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write("You: ");
                Console.ResetColor();

                var input = Console.ReadLine()?.Trim();

                if (string.IsNullOrEmpty(input))
                    continue;

                if (input.Equals("exit", StringComparison.OrdinalIgnoreCase) ||
                    input.Equals("quit", StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine("Goodbye!");
                    break;
                }

                if (input.Equals("clear", StringComparison.OrdinalIgnoreCase))
                {
                    conversationHistory.Clear();
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("[Conversation cleared]");
                    Console.ResetColor();
                    continue;
                }

                if (input.Equals("stream", StringComparison.OrdinalIgnoreCase))
                {
                    useStreaming = !useStreaming;
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"[Streaming mode: {(useStreaming ? "ON" : "OFF")}]");
                    Console.ResetColor();
                    continue;
                }

                conversationHistory.Add(new ChatMessageDto { Role = "user", Content = input });

                try
                {
                    Console.ForegroundColor = ConsoleColor.Blue;
                    Console.Write("Assistant: ");
                    Console.ResetColor();

                    if (useStreaming)
                    {
                        var assistantMessage = await SendStreamingMessage(conversationHistory);
                        if (assistantMessage != null)
                        {
                            conversationHistory.Add(new ChatMessageDto { Role = "assistant", Content = assistantMessage });
                        }
                    }
                    else
                    {
                        var response = await SendMessage(conversationHistory);
                        if (response != null)
                        {
                            Console.WriteLine(response.Message);
                            Console.ForegroundColor = ConsoleColor.DarkGray;
                            Console.WriteLine($"  [Tokens: prompt={response.PromptTokens}, completion={response.CompletionTokens}, total={response.TotalTokens}]");
                            Console.ResetColor();
                            conversationHistory.Add(new ChatMessageDto { Role = response.Role, Content = response.Message });
                        }
                    }
                }
                catch (HttpRequestException ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"Connection error: {ex.Message}");
                    Console.WriteLine("Make sure the API server is running.");
                    Console.ResetColor();
                    // Remove the last user message since request failed
                    if (conversationHistory.Count > 0)
                        conversationHistory.RemoveAt(conversationHistory.Count - 1);
                }
                catch (TaskCanceledException)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Request timed out.");
                    Console.ResetColor();
                    if (conversationHistory.Count > 0)
                        conversationHistory.RemoveAt(conversationHistory.Count - 1);
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"Error: {ex.Message}");
                    Console.ResetColor();
                    if (conversationHistory.Count > 0)
                        conversationHistory.RemoveAt(conversationHistory.Count - 1);
                }

                Console.WriteLine();
            }
        }

        static async Task<ChatResponseDto?> SendMessage(List<ChatMessageDto> messages)
        {
            var requestBody = new ChatRequestDto { Messages = messages };
            var response = await _httpClient.PostAsJsonAsync("/api/chat", requestBody);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException($"API returned {(int)response.StatusCode}: {error}");
            }

            return await response.Content.ReadFromJsonAsync<ChatResponseDto>();
        }

        static async Task<string?> SendStreamingMessage(List<ChatMessageDto> messages)
        {
            var requestBody = new ChatRequestDto { Messages = messages };
            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var request = new HttpRequestMessage(HttpMethod.Post, "/api/chat/stream")
            {
                Content = content
            };

            using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException($"API returned {(int)response.StatusCode}: {error}");
            }

            var fullMessage = new StringBuilder();
            using var stream = await response.Content.ReadAsStreamAsync();
            using var reader = new StreamReader(stream);

            while (!reader.EndOfStream)
            {
                var line = await reader.ReadLineAsync();
                if (line == null) break;

                if (line.StartsWith("data: "))
                {
                    var data = line[6..]; // Remove "data: " prefix
                    if (data == "[DONE]")
                    {
                        Console.WriteLine();
                        break;
                    }
                    if (data == "[ERROR]")
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("[Stream error]");
                        Console.ResetColor();
                        return null;
                    }
                    Console.Write(data);
                    fullMessage.Append(data);
                }
            }

            return fullMessage.Length > 0 ? fullMessage.ToString() : null;
        }
    }
}

