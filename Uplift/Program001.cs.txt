// Import packages
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;

//gpt-4o-mini,gpt-3.5-turbo
// Create a kernel with OpenAI chat completion
var builder = Kernel.CreateBuilder().AddOpenAIChatCompletion("gpt-3.5-turbo", "");

// Add enterprise components
builder.Services.AddLogging(services => services.AddConsole().SetMinimumLevel(LogLevel.Trace));

// Build the kernel
Kernel kernel = builder.Build();
var chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();

// Add a plugin (the LightsPlugin class is defined below)
kernel.Plugins.AddFromType<LightsPlugin>("Lights");

// Enable planning
OpenAIPromptExecutionSettings openAIPromptExecutionSettings = new() 
{
    ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions
};

// Create a history store the conversation
var history = new ChatHistory();

// Initiate a back-and-forth chat
string? userInput;
do {
    // Collect user input
    Console.Write("User > ");
    userInput = Console.ReadLine();

    // Add user input
    history.AddUserMessage(userInput);
    // Non-streaming chat completion
    // Get the response from the AI
    var result = await chatCompletionService.GetChatMessageContentAsync(
        history,
        executionSettings: openAIPromptExecutionSettings,
        kernel: kernel);

    // Print the results
    Console.WriteLine("Assistant > " + result);

    //Streaming chat completion
    // var response = chatCompletionService.GetStreamingChatMessageContentsAsync(
    //                     chatHistory: history,
    //                     kernel: kernel
    //                 );

    // await foreach (var chunk in response)
    // {
    //     Console.Write(chunk);
    // }

    // Add the message from the agent to the chat history
    history.AddMessage(result.Role, result.Content ?? string.Empty);
} while (userInput is not null);