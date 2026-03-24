Public Module ChatBotLogic
    Private ReadOnly RandomReplies As New List(Of String) From {
        "Sorry, I have no idea. Could you please rephrase?",
        "What an interesting question! Let me explain it to you in detail.",
        "According to my comprehension, the answer to this question is...",
        "You just came up with a good perspective, and I think...",
        "Let me help you analyze this case...",
        "Based on your description, I suggest you...",
        "This question involves multiple aspects, I will explain them one by one...",
        "I understand your idea, let me provide some information for you...",
        "This question is a bit complex, let me help you analyze it..."
    }

    Private ReadOnly FixedReplies As New List(Of (keys As String(), reply As String)) From {
        ({"hello", "hi", "hey"}, "Hello! I'm Monoeleven AI, nice to meet you."),
        ({"thanks", "thank you"}, "You're welcome!"),
        ({"who are you"}, "I'm Monoeleven AI, your personal assistant."),
        ({"time"}, $"It's {Date.Now}."),
        ({"bye", "goodbye"}, "Goodbye! Have a nice day!"),
        ({"weather"}, "I'm not sure about the weather. You can check a weather app for more information."),
        ({"how are you"}, "Fine, thank you! And you?"),
        ({"i am fine", "i'm fine"}, "That's good to hear!")
    }

    Public ReadOnly Property ChatbotReply(userMessage As String) As String
        Get
            userMessage = userMessage.ToLower().Trim()

            For Each keysReply In FixedReplies
                With keysReply
                    If Aggregate k In .keys Into Any(userMessage.Contains(k)) Then Return .reply
                End With
            Next

            Return RandomReplies(Random.Shared.Next(RandomReplies.Count))
        End Get
    End Property
End Module
