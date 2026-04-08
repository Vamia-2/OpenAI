# Azure OpenAI ASP.NET Core Project

A complete C# solution integrating Azure OpenAI for text generation and chat functionality, consisting of:

- **ASP.NET Core REST API** (`OpenAI/`) — exposes endpoints for text generation and chat
- **Console Chat Client** (`OpenAI.ConsoleClient/`) — interactive command-line chat interface

---

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- An [Azure OpenAI resource](https://learn.microsoft.com/azure/ai-services/openai/overview) with a deployed GPT model

---

## Configuration

Edit `OpenAI/appsettings.json` and replace the placeholder values with your Azure OpenAI credentials:

```json
{
  "AzureOpenAI": {
    "Endpoint": "https://<your-resource-name>.openai.azure.com/",
    "ApiKey": "<your-api-key>",
    "DeploymentName": "<your-deployment-name>"
  }
}
```

> **Security tip:** For production, store sensitive values in environment variables or Azure Key Vault instead of `appsettings.json`.

---

## Running the API

```bash
cd OpenAI
dotnet run
```

The API will start on `http://localhost:5000` (or the port shown in the console).

### API Endpoints

| Method | Path | Description |
|--------|------|-------------|
| `POST` | `/api/textgeneration/generate` | Generate text from a prompt |
| `POST` | `/api/chat` | Send a chat request (full response) |
| `POST` | `/api/chat/stream` | Send a chat request (streaming via SSE) |

Swagger UI is available at `http://localhost:5000/swagger` when running in Development mode.

#### Text Generation

```bash
curl -X POST http://localhost:5000/api/textgeneration/generate \
  -H "Content-Type: application/json" \
  -d '{
    "prompt": "Explain quantum computing in simple terms",
    "maxTokens": 512,
    "temperature": 0.7
  }'
```

#### Chat

```bash
curl -X POST http://localhost:5000/api/chat \
  -H "Content-Type: application/json" \
  -d '{
    "messages": [
      { "role": "system", "content": "You are a helpful assistant." },
      { "role": "user", "content": "Hello! What can you help me with?" }
    ],
    "maxTokens": 512,
    "temperature": 0.7
  }'
```

---

## Running the Console Chat Client

With the API running, open a new terminal:

```bash
cd OpenAI.ConsoleClient
dotnet run
```

You can also specify the API base URL as an argument:

```bash
dotnet run -- http://localhost:5000
```

Or via an environment variable:

```bash
OPENAI_API_URL=http://localhost:5000 dotnet run
```

### Console Commands

| Command | Description |
|---------|-------------|
| Any text | Send message to the AI |
| `clear` | Reset conversation history |
| `stream` | Toggle streaming mode on/off |
| `exit` / `quit` | Exit the application |

---

## Project Structure

```
OpenAI.sln
├── OpenAI/                          # ASP.NET Core API
│   ├── Controllers/
│   │   ├── ChatController.cs        # Chat endpoints (regular + streaming)
│   │   └── TextGenerationController.cs
│   ├── Models/
│   │   ├── ChatRequest.cs
│   │   ├── ChatResponse.cs
│   │   ├── TextGenerationRequest.cs
│   │   └── TextGenerationResponse.cs
│   ├── Services/
│   │   ├── IAzureOpenAIService.cs   # Service interface
│   │   └── AzureOpenAIService.cs    # Azure OpenAI SDK implementation
│   ├── Program.cs
│   └── appsettings.json
└── OpenAI.ConsoleClient/            # Interactive console chat client
    └── Program.cs
```

---

## NuGet Packages

| Package | Version | Used In |
|---------|---------|---------|
| `Azure.AI.OpenAI` | 2.1.0 | API project |
| `Swashbuckle.AspNetCore` | 6.4.0 | API project (Swagger) |
| `Microsoft.Extensions.Http` | 8.0.0 | Console client |
