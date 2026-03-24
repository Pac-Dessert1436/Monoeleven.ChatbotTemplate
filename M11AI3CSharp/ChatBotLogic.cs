namespace M11AI3CSharp;

public static class ChatBotLogic
{
    private static readonly List<string> RandomReplies =
    [
        "Sorry, I have no idea. Could you please rephrase?",
        "What an interesting question! Let me explain it to you in detail.",
        "According to my comprehension, the answer to this question is...",
        "You just came up with a good perspective, and I think...",
        "Let me help you analyze this case...",
        "Based on your description, I suggest you...",
        "This question involves multiple aspects, I will explain them one by one...",
        "I understand your idea, let me provide some information for you...",
        "This question is a bit complex, let me help you analyze it..."
    ];

    private static readonly List<(string[] keys, string reply)> FixedReplies =
    [
        (new[] { "hello", "hi", "hey" }, "Hello! I'm Monoeleven AI, nice to meet you."),
        (new[] { "thanks", "thank you" }, "You're welcome!"),
        (new[] { "who are you" }, "I'm Monoeleven AI, your personal assistant."),
        (new[] { "time" }, $"It's {DateTime.Now}."),
        (new[] { "bye", "goodbye" }, "Goodbye! Have a nice day!"),
        (new[] { "weather" }, "I'm not sure about the weather. You can check a weather app for more information."),
        (new[] { "how are you" }, "Fine, thank you! And you?"),
        (new[] { "i am fine", "i'm fine" }, "That's good to hear!")
    ];

    public static string GetChatbotReply(string userMessage)
    {
        userMessage = userMessage.ToLower().Trim();
        
        foreach (var (keys, reply) in FixedReplies)
        {
            if (keys.Any(key => userMessage.Contains(key)))
            {
                return reply;
            }
        }

        return RandomReplies[Random.Shared.Next(RandomReplies.Count)];
    }
}