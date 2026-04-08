using Microsoft.AspNetCore.Mvc;
using AzureOpenAIServer.Models;
using AzureOpenAIServer.Services;

namespace AzureOpenAIServer.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TextGenerationController : ControllerBase
    {
        private readonly IAzureOpenAIService _openAIService;
        private readonly ILogger<TextGenerationController> _logger;

        public TextGenerationController(IAzureOpenAIService openAIService, ILogger<TextGenerationController> logger)
        {
            _openAIService = openAIService;
            _logger = logger;
        }

        /// <summary>
        /// Generates text based on the provided prompt.
        /// </summary>
        [HttpPost("generate")]
        [ProducesResponseType(typeof(TextGenerationResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Generate([FromBody] TextGenerationRequest request, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var response = await _openAIService.GenerateTextAsync(request, cancellationToken);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating text");
                return StatusCode(StatusCodes.Status500InternalServerError, new { error = "An error occurred while generating text." });
            }
        }
    }
}
