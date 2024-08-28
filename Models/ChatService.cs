using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;

namespace BooksApi.Models
{
    #pragma warning disable SKEXP0001, SKEXP0003, SKEXP0010, SKEXP0011, SKEXP0050, SKEXP0052

    public static class ChatService
    {
        private static ChatHistory messages = new ChatHistory();
        private static IChatCompletionService chat;
        static ChatService()
        {
            var builder = Kernel.CreateBuilder();
            builder.AddOpenAIChatCompletion(
                modelId: "phi3",
                endpoint: new Uri("http://localhost:11434"),
                apiKey: "apikey");
            var kernel = builder.Build();
            chat = kernel.GetRequiredService<IChatCompletionService>();
            string sp = "You are a helpful and concise assistant. repond in one word";
            var systemMessage = new ChatMessageContent
            {
                Content = sp,
                Metadata = new Dictionary<string, object>
                {
                    { "role", "system" }
                }
            };
            messages.AddSystemMessage(systemMessage.Content);
        }

        public static string ChatMessage(int msgIdx)
        {
            var msg = messages[msgIdx];

            string text = string.IsNullOrEmpty(msg.Content) ? "..." : msg.Content;
            bool generating = msg.IsGenerating();

            string userImage = "https://gramener.com/comicgen/v1/comic?name=dee&angle=side&emotion=happy&pose=explaining&box=&boxcolor=%23000000&boxgap=&mirror=";
            string assistantImage = "https://gramener.com/comicgen/v1/comic?name=ava&emotion=angry&pose=angry&shirt=%23b1dbf2&box=&boxcolor=%23000000&boxgap=&mirror=";

            string imgRole = msg.GetAuthorRole() == AuthorRole.User ? userImage : assistantImage;
            string chatClass = $"chat-message {(msg.GetAuthorRole() == AuthorRole.User ? "user" : "assistant")}";

            string streamArgs = generating ? $"hx-trigger='every 0.1s' hx-swap='outerHTML' hx-get='/chat_message/{msgIdx}'" : "";

            return $@"
<div class='{chatClass}' id='chat-message-{msgIdx}' {streamArgs}>
    <img src='{imgRole}' />
    <div class='bubble'>
        <p>{text}</p>
    </div>
</div>";
        }

        public static string ChatInput()
        {
            return @"
                    <input required type='text' name='msg' id='msg-input' 
                        placeholder='Type a message' class='form-control' 
                        hx-swap-oob='true' aria-label='Type your message here' 
                        aria-describedby='sendButton' />";
        }

        public static async Task GetResponseAsync(Task<ChatHistory> stream, int idx)
        {
            var response = await stream;
            foreach (var chunk in response)
            {
                if (!string.IsNullOrEmpty(chunk.Content))
                {
                    var currentMessage = messages[idx];
                    var newContent = currentMessage.Content + chunk.Content;
                    currentMessage.Content = newContent;
                }
            }

            var message = messages[idx];
            message.SetGeneratingFlag(false);
        }

        public static async Task<(string userMessage, string assistantMessage, string chatInput)> PostAsync(Rat msg)
        {
            int idx = messages.Count;

            var userMessage = new ChatMessageContent
            {
                Content = msg.message,
                Metadata = new Dictionary<string, object>
                {
                    { "role", "user" }
                }
            };
            userMessage.SetAuthorRole(AuthorRole.User);
            messages.AddUserMessage(userMessage.Content); // Assuming AddUserMessage only accepts string content

            foreach (var message in messages)
            {
                message.SetGeneratingFlag(false);
            }

            var stream = SimulateChatResponse(messages); // Simulate the chat response generation

            var assistantMessage = new ChatMessageContent
            {
                Content = "",
                Metadata = new Dictionary<string, object>
                {
                    { "role", "assistant" },
                    { "generating", true }
                }
            };
            assistantMessage.SetAuthorRole(AuthorRole.Assistant);
            assistantMessage.SetGeneratingFlag(true);
            messages.AddAssistantMessage(assistantMessage.Content); // Assuming AddAssistantMessage only accepts string content
            try
            {
                await GetResponseAsync(stream, idx + 1);
            }
            catch (TaskCanceledException ex)
            {
                // Handle the cancellation here
                Console.WriteLine("GetResponseAsync Task was canceled: " + ex.Message);
            }
            

            return (ChatMessage(idx), ChatMessage(idx + 1), ChatInput());
        }

        private static async Task<ChatHistory> SimulateChatResponse(ChatHistory messages)
        {
            var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30)); // 30-second timeout
            try
            {
                // Await your task here
                var result = await chat.GetChatMessageContentsAsync(messages, cancellationToken:cts.Token);
                Console.WriteLine(result[^1].Content);
                messages.Add(result[^1]);
            }
            catch (TaskCanceledException ex)
            {
                // Handle the cancellation here
                Console.WriteLine("Task was canceled: " + ex.Message);
            }

            return messages;
        }
    }

    public static class ChatMessageExtensions
    {
        public static AuthorRole GetAuthorRole(this ChatMessageContent message)
        {
            if (message.Metadata.ContainsKey("role"))
            {
                var role = message.Metadata["role"] as string;
                if (Enum.TryParse(role, out AuthorRole authorRole))
                {
                    return authorRole;
                }
            }
            // Return a default value (e.g., system or user) if "role" is not set or invalid
            return AuthorRole.System; // or AuthorRole.User
        }

        public static void SetAuthorRole(this ChatMessageContent message, AuthorRole role)
        {
            var newMetadata = new Dictionary<string, object>(message.Metadata)
            {
                ["role"] = role.ToString()
            };
            message.Metadata = newMetadata;
        }

        public static bool IsGenerating(this ChatMessageContent message)
        {
            return message.Metadata.ContainsKey("generating") && (bool)message.Metadata["generating"];
        }

        public static void SetGeneratingFlag(this ChatMessageContent message, bool value)
        {
            // Ensure that Metadata is initialized
            if (message.Metadata == null)
            {
                message.Metadata = new Dictionary<string, object>();
            }

            // Create a new dictionary by copying the existing Metadata
            var newMetadata = new Dictionary<string, object>(message.Metadata)
            {
                ["generating"] = value
            };

            // Assign the updated dictionary back to the Metadata property
            message.Metadata = newMetadata;
        }
    }
}
