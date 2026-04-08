using Microsoft.AspNetCore.Mvc;
using AzureOpenAIServer.Models;
using AzureOpenAIServer.Services;
using System.Text;

namespace AzureOpenAIServer.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ChatController : ControllerBase
    {
        private readonly IAzureOpenAIService _openAIService;
        private readonly ILogger<ChatController> _logger;

        public ChatController(IAzureOpenAIService openAIService, ILogger<ChatController> logger)
        {
            _openAIService = openAIService;
            _logger = logger;
        }

        /// <summary>
        /// Sends a chat request and returns a complete response.
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(ChatResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Chat([FromBody] ChatRequest request, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var response = await _openAIService.ChatAsync(request, cancellationToken);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing chat request");
                return StatusCode(StatusCodes.Status500InternalServerError, new { error = "An error occurred while processing the chat request." });
            }
        }

        /// <summary>
        /// Sends a chat request and streams the response as Server-Sent Events.
        /// </summary>
        [HttpPost("stream")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task StreamChat([FromBody] ChatRequest request, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                Response.StatusCode = StatusCodes.Status400BadRequest;
                return;
            }

            Response.Headers.Append("Content-Type", "text/event-stream");
            Response.Headers.Append("Cache-Control", "no-cache");
            Response.Headers.Append("X-Accel-Buffering", "no");

            try
            {
                await foreach (var chunk in _openAIService.ChatStreamAsync(request, cancellationToken))
                {
                    var data = $"data: {chunk}\n\n";
                    var bytes = Encoding.UTF8.GetBytes(data);
                    await Response.Body.WriteAsync(bytes, cancellationToken);
                    await Response.Body.FlushAsync(cancellationToken);
                }

                var doneBytes = Encoding.UTF8.GetBytes("data: [DONE]\n\n");
                await Response.Body.WriteAsync(doneBytes, cancellationToken);
                await Response.Body.FlushAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Chat stream was cancelled by the client.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error streaming chat response");
                var errorBytes = Encoding.UTF8.GetBytes("data: [ERROR]\n\n");
                await Response.Body.WriteAsync(errorBytes, cancellationToken);
                await Response.Body.FlushAsync(cancellationToken);
            }
        }
    }
}
