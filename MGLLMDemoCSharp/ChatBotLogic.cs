using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using Microsoft.VisualBasic;

namespace MGLLMDemoCSharp;

public sealed class Message
{
    public string? Role { get; set; }
    public string? Content { get; set; }
}

public sealed class DeepSeekRequest
{
    public string? Model { get; set; }
    public List<Message>? Messages { get; set; }
    public double Temperature { get; set; }
    public int MaxTokens { get; set; }
    public double FrequencyPenalty { get; set; }
    public double PresencePenalty { get; set; }
}

public sealed class Choice
{
    public Message? Message { get; set; }
}

public sealed class DeepSeekResponse
{
    public List<Choice>? Choices { get; set; }
}

public sealed class VoiceConfig
{
    [JsonPropertyName("assistant")]
    public string? Assistant { get; set; }
    [JsonPropertyName("user")]
    public string? User { get; set; }
}

public sealed class ModelParams
{
    [JsonPropertyName("temperature")]
    public double Temperature { get; set; }
    [JsonPropertyName("max_tokens")]
    public int MaxTokens { get; set; }
    [JsonPropertyName("frequency_penalty")]
    public double FrequencyPenalty { get; set; }
    [JsonPropertyName("presence_penalty")]
    public double PresencePenalty { get; set; }
}

public sealed class AppConfig
{
    [JsonPropertyName("api_key")]
    public string? ApiKey { get; set; }
    [JsonPropertyName("system_prompt")]
    public List<string>? SystemPrompt { get; set; }
    [JsonPropertyName("model_params")]
    public ModelParams? ModelParams { get; set; }
}

public sealed class ChatLogEntry
{
    public int Index { get; set; }
    public string? Timestamp { get; set; }
    public string? UserMessage { get; set; }
    public string? ChatbotResponse { get; set; }
}

public static class ChatBotLogic
{
    private static int _currentIndex = 0;

    private static AppConfig? Config { get; set; }

    public static void LoadAppConfig()
    {
        string configPath = Path.Combine(Directory.GetCurrentDirectory(), "mgllm-config.json");
        if (File.Exists(configPath))
        {
            string jsonContent = File.ReadAllText(configPath);
            Config = JsonSerializer.Deserialize<AppConfig>(jsonContent);
        }
        else
        {
            Config = CreateDefaultAppConfig();
            try
            {
                string prompt = @"Please enter your DeepSeek API key. This will not be saved to disk by default.

If you leave this blank the application will still run but API requests will fail.";
                string entered = Interaction.InputBox(prompt, "DeepSeek API Key", "");
                if (!string.IsNullOrWhiteSpace(entered))
                    Config.ApiKey = entered.Trim();
            }
            catch (Exception ex)
            {
                Interaction.MsgBox(ex.ToString(), Title: ex.GetType().Name);
            }
        }
        ApplyConfigFallbacks();
    }

    private static AppConfig CreateDefaultAppConfig()
    {
        return new AppConfig()
        {
            ApiKey = string.Empty,
            SystemPrompt = [],
            ModelParams = new ModelParams()
            {
                Temperature = 0.7,
                MaxTokens = 500,
                FrequencyPenalty = 0.3,
                PresencePenalty = 0.3
            }
        };
    }

    private static void ApplyConfigFallbacks()
    {
        var defaultAppConfig = CreateDefaultAppConfig();
        if (Config is null)
        {
            Config = defaultAppConfig;
            return;
        }
        Config.SystemPrompt ??= defaultAppConfig.SystemPrompt;
        Config.ModelParams ??= defaultAppConfig.ModelParams;
    }

    private const string DeepSeekApiUrl = "https://api.deepseek.com/chat/completions";

    private static string ChatLogYamlFilePath =>
        Path.Combine(Directory.GetCurrentDirectory(), "mgllm-chatlog.yml");

    public async static Task<string> GetDeepSeekResponseAsync(string userMessage, List<Message> conversationHistory = null!)
    {
        using HttpClient httpClient = new();
        httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {Config!.ApiKey}");

        List<Message> messages =
        [
            new()
            {
                Role = "system",
                Content = string.Join("\n", Config.SystemPrompt!)
            }
        ];

        if (conversationHistory is not null)
        {
            foreach (Message msg in conversationHistory)
                messages.Add(new Message() { Role = msg.Role, Content = msg.Content });
        }
        messages.Add(new Message() { Role = "user", Content = userMessage });

        DeepSeekRequest requestBody = new()
        {
            Model = "deepseek-v4-flash",
            Messages = messages,
            Temperature = Config.ModelParams!.Temperature,
            MaxTokens = Config.ModelParams.MaxTokens,
            FrequencyPenalty = Config.ModelParams.FrequencyPenalty,
            PresencePenalty = Config.ModelParams.PresencePenalty
        };

        JsonSerializerOptions options = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        string jsonContent = JsonSerializer.Serialize(requestBody, options);
        StringContent content = new(jsonContent, Encoding.UTF8, "application/json");

        try
        {
            var response = await httpClient.PostAsync(DeepSeekApiUrl, content);
            string responseContent = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var jsonResponse = JsonSerializer.Deserialize<DeepSeekResponse>(responseContent, options);
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

    public static int NextConversationIndex
    {
        get
        {
            _currentIndex += 1;
            return _currentIndex;
        }
    }

    public static void SaveChatLog(string userMessage, string botResponse, int index)
    {
        string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        var namingConvention = CamelCaseNamingConvention.Instance;
        List<ChatLogEntry> entries = [];

        if (File.Exists(ChatLogYamlFilePath))
        {
            string yamlContent = File.ReadAllText(ChatLogYamlFilePath);
            var deserializer = new DeserializerBuilder().WithNamingConvention(namingConvention).Build();
            entries = deserializer.Deserialize<List<ChatLogEntry>>(yamlContent);
        }
        entries ??= [];
        entries.Add(new ChatLogEntry()
        {
            Index = index,
            Timestamp = timestamp,
            UserMessage = userMessage,
            ChatbotResponse = botResponse
        });

        var serializer = new SerializerBuilder().WithNamingConvention(namingConvention).Build();
        File.WriteAllText(ChatLogYamlFilePath, serializer.Serialize(entries), Encoding.UTF8);
    }
}