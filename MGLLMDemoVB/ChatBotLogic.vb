Imports System.IO
Imports System.Net.Http
Imports System.Net.Http.Json
Imports System.Text
Imports System.Text.Json
Imports System.Text.Json.Serialization
Imports Microsoft.VisualBasic
Imports YamlDotNet.Serialization
Imports YamlDotNet.Serialization.NamingConventions

Public NotInheritable Class Message
    Public Property Role As String
    Public Property Content As String
End Class

Public NotInheritable Class DeepSeekRequest
    Public Property Model As String
    Public Property Messages As List(Of Message)
    Public Property Temperature As Double
    Public Property MaxTokens As Integer
    Public Property FrequencyPenalty As Double
    Public Property PresencePenalty As Double
End Class

Public NotInheritable Class Choice
    Public Property Message As Message
End Class

Public NotInheritable Class DeepSeekResponse
    Public Property Choices As List(Of Choice)
End Class

Public NotInheritable Class VoiceConfig
    <JsonPropertyName("assistant")> Public Property Assistant As String
    <JsonPropertyName("user")> Public Property User As String
End Class

Public NotInheritable Class ModelParams
    <JsonPropertyName("temperature")> Public Property Temperature As Double
    <JsonPropertyName("max_tokens")> Public Property MaxTokens As Integer
    <JsonPropertyName("frequency_penalty")> Public Property FrequencyPenalty As Double
    <JsonPropertyName("presence_penalty")> Public Property PresencePenalty As Double
End Class

Public NotInheritable Class AppConfig
    <JsonPropertyName("api_key")> Public Property ApiKey As String
    <JsonPropertyName("system_prompt")> Public Property SystemPrompt As List(Of String)
    <JsonPropertyName("model_params")> Public Property ModelParams As ModelParams
End Class

Public NotInheritable Class ChatLogEntry
    Public Property Index As Integer
    Public Property Timestamp As String
    Public Property UserMessage As String
    Public Property ChatbotResponse As String
End Class

Public Module ChatBotLogic
    Private _currentIndex As Integer = 0

    Private Property Config As AppConfig

    Public Sub LoadAppConfig()
        Dim configPath = Path.Combine(Directory.GetCurrentDirectory(), "mgllm-config.json")
        If File.Exists(configPath) Then
            Dim jsonContent = File.ReadAllText(configPath)
            Config = JsonSerializer.Deserialize(Of AppConfig)(jsonContent)
        Else
            Config = CreateDefaultAppConfig()
            Try
                Dim prompt = "Please enter your DeepSeek API key. This will not be saved to disk by default.

If you leave this blank the application will still run but API requests will fail."
                Dim entered = InputBox(prompt, "DeepSeek API Key", "")
                If Not String.IsNullOrWhiteSpace(entered) Then Config.ApiKey = entered.Trim()
            Catch ex As Exception
                MsgBox(ex.ToString(), Title:=ex.GetType().Name)
            End Try
        End If
        ApplyConfigFallbacks()
    End Sub

    Private Function CreateDefaultAppConfig() As AppConfig
        Return New AppConfig With {
            .ApiKey = String.Empty,
            .SystemPrompt = New List(Of String),
            .ModelParams = New ModelParams With {
                .Temperature = 0.7,
                .MaxTokens = 500,
                .FrequencyPenalty = 0.3,
                .PresencePenalty = 0.3
            }
        }
    End Function

    Private Sub ApplyConfigFallbacks()
        Dim defaultAppConfig = CreateDefaultAppConfig()
        If Config Is Nothing Then
            Config = defaultAppConfig
            Exit Sub
        End If
        With Config
            If .SystemPrompt Is Nothing Then .SystemPrompt = defaultAppConfig.SystemPrompt
            If .ModelParams Is Nothing Then .ModelParams = defaultAppConfig.ModelParams
        End With
    End Sub

    Private Const DEEPSEEK_API_URL As String = "https://api.deepseek.com/chat/completions"

    Private ReadOnly Property ChatLogYamlFilePath As String
        Get
            Return Path.Combine(Directory.GetCurrentDirectory(), "mgllm-chatlog.yml")
        End Get
    End Property

    Public Async Function GetDeepSeekResponseAsync _
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

            Dim requestBody As New DeepSeekRequest With {
                .Model = "deepseek-v4-flash",
                .Messages = messages,
                .Temperature = Config.ModelParams.Temperature,
                .MaxTokens = Config.ModelParams.MaxTokens,
                .FrequencyPenalty = Config.ModelParams.FrequencyPenalty,
                .PresencePenalty = Config.ModelParams.PresencePenalty
            }

            Dim options As New JsonSerializerOptions With {
                .PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            }
            Dim jsonContent As String = JsonSerializer.Serialize(requestBody, options)
            Dim content As New StringContent(jsonContent, Encoding.UTF8, "application/json")

            Try
                Dim response = Await httpClient.PostAsync(DEEPSEEK_API_URL, content)
                Dim responseContent = Await response.Content.ReadAsStringAsync()

                If response.IsSuccessStatusCode Then
                    Dim jsonResponse =
                        JsonSerializer.Deserialize(Of DeepSeekResponse)(responseContent, options)
                    Return jsonResponse.Choices(0).Message.Content
                Else
                    Return $"API Error: {response.StatusCode} - {responseContent}"
                End If
            Catch ex As Exception
                Return $"Request failed: {ex.Message}"
            End Try
        End Using
    End Function

    Public ReadOnly Property NextConversationIndex As Integer
        Get
            _currentIndex += 1
            Return _currentIndex
        End Get
    End Property

    Public Sub SaveChatLog(userMessage As String, botResponse As String, index As Integer)
        Dim timestamp As String = Date.Now.ToString("yyyy-MM-dd HH:mm:ss")
        Dim namingConvention = CamelCaseNamingConvention.Instance
        Dim entries As New List(Of ChatLogEntry)

        If File.Exists(ChatLogYamlFilePath) Then
            Dim yamlContent As String = File.ReadAllText(ChatLogYamlFilePath)
            Dim deserializer =
                (New DeserializerBuilder).WithNamingConvention(namingConvention).Build()
            entries = deserializer.Deserialize(Of List(Of ChatLogEntry))(yamlContent)
        End If
        If entries Is Nothing Then entries = New List(Of ChatLogEntry)
        entries.Add(New ChatLogEntry With {
            .Index = index,
            .Timestamp = timestamp,
            .UserMessage = userMessage,
            .ChatbotResponse = botResponse
        })

        Dim serializer = (New SerializerBuilder).WithNamingConvention(namingConvention).Build()
        File.WriteAllText(ChatLogYamlFilePath, serializer.Serialize(entries), Encoding.UTF8)
    End Sub
End Module