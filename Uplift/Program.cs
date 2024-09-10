// Import packages
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

//'Microsoft.SemanticKernel.ChatMessageContent.AuthorName' is for evaluation purposes only and is subject to change or removal in future updates
#pragma warning disable SKEXP0001

//gpt-4o-mini,gpt-3.5-turbo
// Create a kernel with OpenAI chat completion
var builder = Kernel.CreateBuilder().AddOpenAIChatCompletion("gpt-3.5-turbo", "");

// Add enterprise components
builder.Services.AddLogging(services => services.AddConsole().SetMinimumLevel(LogLevel.Trace));

// Build the kernel
Kernel kernel = builder.Build();
var chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();

// Add a plugin (the LightsPlugin class is defined below)
//kernel.Plugins.AddFromType<LightsPlugin>("Lights");

// Enable planning
OpenAIPromptExecutionSettings openAIPromptExecutionSettings = new() 
{
    ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions
};

// Create a history store the conversation
//var history = new ChatHistory();

// Create a chat history object
ChatHistory chatHistory = [];

// chatHistory.AddSystemMessage("You are a helpful assistant and you know about the author Stephen King, he is the king of horror");
// chatHistory.AddUserMessage("What's his latest three books?");
// chatHistory.AddAssistantMessage("His last three books are 'The Bazaar of Bad Dreams', 'If It Bleeds', and 'You Like It Darker'. What would you like to read next?");
// chatHistory.AddUserMessage("I'd like to read the second book, please.");

//Adding richer messages to a chat history

// Add system message
chatHistory.Add(
    new() {
        Role = AuthorRole.System,
        Content = "You are a helpful assistant and you know about the author Stephen King, he is the king of horror"
    }
);

// Add user message with an image
chatHistory.Add(
    new() {
        Role = AuthorRole.User,
        AuthorName = "RajeshRadhakrishnan",
        Items = [
            new TextContent { Text = "What's his latest three books?" },
            //new ImageContent { Uri = new Uri("https://en.wikipedia.org/wiki/File:Stephen_King,_Comicon.jpg") }
        ]
    }
);

// Add assistant message
chatHistory.Add(
    new() {
        Role = AuthorRole.Assistant,
        AuthorName = "HorrorBookAssistant",
        Content = "His last three books are 'The Bazaar of Bad Dreams', 'If It Bleeds', and 'You Like It Darker'. What would you like to read next?"
    }
);

// Add additional message from a different user
chatHistory.Add(
    new() {
        Role = AuthorRole.User,
        AuthorName = "RameshRadhakrishnan",
        Content = "I'd like to read the second book, please."
    }
);

// Add a simulated function call from the assistant
chatHistory.Add(
    new() {
        Role = AuthorRole.Assistant,
        Items = [
            new FunctionCallContent(
                functionName: "get_user_allergies",
                pluginName: "User",
                id: "0001",
                arguments: new () { {"username", "rajeshradhakrishnan"} }
            ),
            new FunctionCallContent(
                functionName: "get_user_allergies",
                pluginName: "User",
                id: "0002",
                arguments: new () { {"username", "rameshradhakrishnan"} }
            )
        ]
    }
);

// Add a simulated function results from the tool role
chatHistory.Add(
    new() {
        Role = AuthorRole.Tool,
        Items = [
            new FunctionResultContent(
                functionName: "get_user_pefer",
                pluginName: "User",
                callId: "0001",
                result: "{ \"pefer\": [\"horror\", \"fantasy\"] }"
            )
        ]
    }
);
chatHistory.Add(
    new() {
        Role = AuthorRole.Tool,
        Items = [
            new FunctionResultContent(
                functionName: "get_user_pefer",
                pluginName: "User",
                callId: "0002",
                result: "{ \"pefer\": [\"supernatural\", \"fiction\"] }"
            )
        ]
    }
);

chatHistory.Add(
        new() {
        Role = AuthorRole.User,
        Content = "Please recommend me a book"
    }
);
// ChatHistory chatHistory = [
//     new() {
//         Role = AuthorRole.User,
//         Content = "Please recommend me a book"
//     }
// ];

// Get the current length of the chat history object
int currentChatHistoryLength = chatHistory.Count;

// Get the chat message content
ChatMessageContent results = await chatCompletionService.GetChatMessageContentAsync(
    chatHistory,
    kernel: kernel
);

// Get the new messages added to the chat history object
for (int i = currentChatHistoryLength; i < chatHistory.Count; i++)
{
    Console.WriteLine(chatHistory[i]);
}

// Print the final message
Console.WriteLine(results);

// Add the final message to the chat history object
chatHistory.Add(results);