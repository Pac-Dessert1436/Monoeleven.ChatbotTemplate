# MonoGame LLM Templates, by Pac-Dessert1436

## Important Notice
This is a personal project, and is not affiliated with DeepSeek or any other LLM provider. It is based on my earlier MonoGame chatbot template (version 1.0.x), which I have now deprecated after identifying potential trademark concerns.

The current version is 1.1.1, intended as a focused maintenance release to:
- avoid the remaining trademark-related risk by keeping the project name and branding aligned with the renamed template
- fix a security issue from 1.1.0 that included a real DeepSeek API key (creating an avoidable exposure risk)

This version removes the exposure of the API key, utilizing safer configuration practices. _Despite the fact that I must pause active development while preparing for the Postgraduate Entrance Exam, I still want to publish this release so the project can be shared safely and responsibly._

### Current Version: 1.1.1
⚠️ **Active development is paused** while I prepare for the Postgraduate Entrance Exam (~150 days remaining). This release is intentionally limited to:
- Resolving trademark concerns through the renamed project structure and documentation
- Removing the accidental API-key exposure risk from the shipped template
- Keeping the project functional and safe for users

No new features will be added during this period, but critical bug fixes may be addressed if time permits.

## Overview

_PacDessert1436.MonoGame.LLM.Templates_ is a collection of templates for building LLM-powered chatbot applications with MonoGame. It provides ready-to-use project templates for both C# and VB.NET, featuring:

- **DeepSeek API Integration**: Seamless connectivity to DeepSeek's powerful language models
- **Built-in Chat UI**: Pre-designed chat interface with message history and user/bot avatars
- **Configuration System**: Easy API key management and model parameter tuning
- **Chat Logging**: Automatic conversation history saving in YAML format
- **Cross-Platform Support**: Works on all MonoGame-supported platforms (DesktopGL, Windows, etc.)

> **Tip**: The LLM provider integrated in this project can be switched to other providers if needed. See [Customization Examples](#customization-examples) for details.

### Known Limitations

- **English-Only Input**: The current template only supports pure English text input. Adding Chinese input support would require significant modifications to the text rendering and input handling systems, due to MonoGame's limitations with non-ASCII character rendering and input processing.

## Quick Start

### Prerequisites
- [.NET 10.0 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) or later
- [MonoGame 3.8.4](https://www.monogame.net) or later
- A DeepSeek API key (get one at [https://platform.deepseek.com/](https://platform.deepseek.com/))

### Installation
1. Install the template package:
```bash
dotnet new install PacDessert1436.MonoGame.LLM.Templates
```

2. Create a new project using your preferred template:
```bash
# C# Template
dotnet new mgllm -n MyMonoGameChatbot

# VB.NET Template
dotnet new mgllmvb -n MyMonoGameChatbot
```

### Configuration
1. Navigate to your project directory
2. Create a `mgllm-config.json` file with your API key:
```json
{
  "api_key": "your_deepseek_api_key_here",
  "system_prompt": ["You are a helpful assistant.", "Respond in a friendly and concise manner."],
  "model_params": {
    "temperature": 0.7,
    "max_tokens": 500,
    "frequency_penalty": 0.3,
    "presence_penalty": 0.3
  }
}
```

3. Build and run the application:
```bash
dotnet run
```

## Features

### Core Functionality
- Real-time chat interface with message bubbles
- Support for long conversations with context retention
- Configurable model parameters (temperature, max tokens, etc.)
- Automatic chat log persistence
- Error handling for API requests

### Technical Details
- Uses DeepSeek's `deepseek-v4-flash` model by default
- Implements proper HTTP client disposal and resource management
- Follows C# nullable reference type guidelines
- YAML serialization for chat logs using YamlDotNet
- JSON configuration with fallback values

## Template Structure

The template provides a clean separation of concerns:
- `GameMain.cs`: Main game loop and initialization
- `ChatUI.cs`: Chat interface rendering and input handling
- `ChatBotLogic.cs`: LLM API integration and business logic
- `mgllm-config.json`: Configuration file (user-editable)
- `mgllm-chatlog.yml`: Auto-generated conversation history

## Customization Examples

### Swapping LLM Provider (C# Example)

Here's how you can modify the `ChatBotLogic.cs` to use OpenAI instead of DeepSeek.

First, add these model classes alongside the existing `Message` and `Choice` classes:

```csharp
// Add these classes for OpenAI integration
public sealed class OpenAIRequest
{
    public string? Model { get; set; } = "gpt-4o-mini";
    public List<Message>? Messages { get; set; }
    public double Temperature { get; set; } = 0.7;
    [JsonPropertyName("max_tokens")]
    public int MaxTokens { get; set; } = 500;
}

public sealed class OpenAIResponse
{
    public List<Choice>? Choices { get; set; }
    [JsonPropertyName("usage")]
    public UsageInfo? Usage { get; set; }
}

public sealed class UsageInfo
{
    [JsonPropertyName("prompt_tokens")]
    public int PromptTokens { get; set; }
    [JsonPropertyName("completion_tokens")]
    public int CompletionTokens { get; set; }
    [JsonPropertyName("total_tokens")]
    public int TotalTokens { get; set; }
}
```

Then, replace the `GetDeepSeekResponseAsync` method (and its associated `DeepSeekApiUrl` constant) with this OpenAI version:

```csharp
private const string OpenAIApiUrl = "https://api.openai.com/v1/chat/completions";

public async static Task<string> GetOpenAIResponseAsync(string userMessage, List<Message> conversationHistory = null!)
{
    using var httpClient = new HttpClient();
    httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {Config.ApiKey}");

    var messages = new List<Message>()
    {
        new()
        {
            Role = "system",
            Content = string.Join("\n", Config.SystemPrompt!)
        }
    };

    if (conversationHistory is not null)
    {
        foreach (Message msg in conversationHistory)
            messages.Add(new Message() { Role = msg.Role, Content = msg.Content });
    }
    messages.Add(new Message() { Role = "user", Content = userMessage });

    var requestBody = new OpenAIRequest()
    {
        Model = "gpt-4o-mini",
        Messages = messages,
        Temperature = Config.ModelParams!.Temperature,
        MaxTokens = Config.ModelParams.MaxTokens
    };

    var options = new JsonSerializerOptions()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };
    string jsonContent = JsonSerializer.Serialize(requestBody, options);
    var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

    try
    {
        var response = await httpClient.PostAsync(OpenAIApiUrl, content);
        string responseContent = await response.Content.ReadAsStringAsync();

        if (response.IsSuccessStatusCode)
        {
            var jsonResponse = JsonSerializer.Deserialize<OpenAIResponse>(responseContent, options);
            return jsonResponse!.Choices![0].Message!.Content!;
        }
        else
        {
            return $"API Error: {response.StatusCode} - {responseContent}";
        }
    }
    catch (Exception ex)
    {
        return $"Request failed: {ex.Message}";
    }
}
```

Finally, update the calling code in `ChatUI.cs` (or wherever `GetDeepSeekResponseAsync` is invoked) to call `GetOpenAIResponseAsync` instead.

> **Note**: The same pattern works for any OpenAI-compatible API provider (e.g., Azure OpenAI, Groq, Together AI, etc.). Simply change the `OpenAIApiUrl` constant and the `Model` name to match your provider's endpoint.

### Swapping LLM Provider (VB.NET Example)

Here's the equivalent modification for the VB.NET template (`ChatBotLogic.vb`).

First, add these model classes alongside the existing `Message` and `Choice` classes:

```vb
Public NotInheritable Class OpenAIRequest
    Public Property Model As String = "gpt-4o-mini"
    Public Property Messages As List(Of Message)
    Public Property Temperature As Double = 0.7
    <JsonPropertyName("max_tokens")> Public Property MaxTokens As Integer = 500
End Class

Public NotInheritable Class OpenAIResponse
    Public Property Choices As List(Of Choice)
    <JsonPropertyName("usage")> Public Property Usage As UsageInfo
End Class

Public NotInheritable Class UsageInfo
    <JsonPropertyName("prompt_tokens")> Public Property PromptTokens As Integer
    <JsonPropertyName("completion_tokens")> Public Property CompletionTokens As Integer
    <JsonPropertyName("total_tokens")> Public Property TotalTokens As Integer
End Class
```

Then, replace the `GetDeepSeekResponseAsync` method (and its associated `DEEPSEEK_API_URL` constant) with this OpenAI version:

```vb
Private Const OPENAI_API_URL As String = "https://api.openai.com/v1/chat/completions"

Public Async Function GetOpenAIResponseAsync _
        (userMessage As String, Optional conversationHistory As List(Of Message) = Nothing) _
        As Task(Of String)
    Using httpClient As New HttpClient
        httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {Config.ApiKey}")

        Dim messages As New List(Of Message) From {
            New Message With {
                .Role = "system",
                .Content = String.Join(vbCrLf, Config.SystemPrompt)
            }
        }

        If conversationHistory IsNot Nothing Then
            For Each msg As Message In conversationHistory
                messages.Add(New Message With {.Role = msg.Role, .Content = msg.Content})
            Next msg
        End If
        messages.Add(New Message With {.Role = "user", .Content = userMessage})

        Dim requestBody As New OpenAIRequest With {
            .Model = "gpt-4o-mini",
            .Messages = messages,
            .Temperature = Config.ModelParams.Temperature,
            .MaxTokens = Config.ModelParams.MaxTokens
        }

        Dim options As New JsonSerializerOptions With {
            .PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            .DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        }
        Dim jsonContent As String = JsonSerializer.Serialize(requestBody, options)
        Dim content As New StringContent(jsonContent, Encoding.UTF8, "application/json")

        Try
            Dim response = Await httpClient.PostAsync(OPENAI_API_URL, content)
            Dim responseContent = Await response.Content.ReadAsStringAsync()

            If response.IsSuccessStatusCode Then
                Dim jsonResponse =
                    JsonSerializer.Deserialize(Of OpenAIResponse)(responseContent, options)
                Return jsonResponse.Choices(0).Message.Content
            Else
                Return $"API Error: {response.StatusCode} - {responseContent}"
            End If
        Catch ex As Exception
            Return $"Request failed: {ex.Message}"
        End Try
    End Using
End Function
```

> **Note**: The same pattern works for any OpenAI-compatible API provider. Just update `OPENAI_API_URL` and the `Model` name to match your provider's endpoint.

### Other OpenAI-Compatible Providers

Since both DeepSeek and OpenAI use the same chat completions API format, you can easily switch to any OpenAI-compatible provider by changing just the endpoint URL and model name:

| Provider | API URL | Example Model |
|----------|---------|---------------|
| **DeepSeek** (default) | `https://api.deepseek.com/chat/completions` | `deepseek-v4-flash` |
| **OpenAI** | `https://api.openai.com/v1/chat/completions` | `gpt-4o-mini`, `gpt-4o` |
| **Azure OpenAI** | `https://{your-resource}.openai.azure.com/openai/deployments/{deployment-name}/chat/completions?api-version=2024-08-01-preview` | deployment name |
| **Groq** | `https://api.groq.com/openai/v1/chat/completions` | `llama-3.3-70b-versatile` |
| **Together AI** | `https://api.together.xyz/v1/chat/completions` | `mistralai/Mixtral-8x7B-Instruct-v0.1` |
| **Anthropic** (via API proxy) | `https://api.anthropic.com/v1/messages` | `claude-3-5-sonnet-20241022` |

> **Note**: Anthropic uses a different request/response format. For Anthropic, you would need to adjust the request body structure to match their Messages API format.

## License

This project is licensed under the BSD 3-Clause License. See the [LICENSE](LICENSE) file for details.