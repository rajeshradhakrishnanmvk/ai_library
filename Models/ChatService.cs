
using System.Text.Json;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

#pragma warning disable  SKEXP0001

[EnableCors("Policy")]
public class ChatService
{
    private readonly Kernel _kernel;
    private readonly IChatCompletionService _chatCompletionService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ChatService(Kernel kernel, IHttpContextAccessor httpContextAccessor)
    {
        _kernel = kernel;
        _httpContextAccessor = httpContextAccessor;
        _chatCompletionService = _kernel.GetRequiredService<IChatCompletionService>();
    }
    //create a get method for history
    public IResult GetHistory()
    {
        var session = _httpContextAccessor.HttpContext.Session;
        var historyJson = session.GetString("ChatHistory");
        var history = string.IsNullOrEmpty(historyJson) ? new ChatHistory("You are a helpful assistant and you know about the author Stephen King, he is the king of horror") : JsonSerializer.Deserialize<ChatHistory>(historyJson);

        return TypedResults.Ok(history);
    }
    public async Task<IResult> AIResponse(BookMessage message)
    {
        var session = _httpContextAccessor.HttpContext.Session;
        var historyJson = session.GetString("ChatHistory");
        var history = string.IsNullOrEmpty(historyJson) ? new ChatHistory("You are a helpful assistant and you know about the author Stephen King, he is the king of horror") : JsonSerializer.Deserialize<ChatHistory>(historyJson);

        history.AddUserMessage(message.Content);

        var result = await _chatCompletionService.GetChatMessageContentAsync(
            history,
            kernel: _kernel);

        history.AddAssistantMessage(result.Content);
        session.SetString("ChatHistory", JsonSerializer.Serialize(history));

        return TypedResults.Ok(history);
    }
    
    public IResult AddHistory(ChatHistory history)
    {
        var session = _httpContextAccessor.HttpContext.Session;
        var historyJson = session.GetString("ChatHistory");
        if (string.IsNullOrEmpty(historyJson))
        {
            session.SetString("ChatHistory", JsonSerializer.Serialize(history));
        }
        else
        {
            var existingHistory = JsonSerializer.Deserialize<ChatHistory>(historyJson);
            // Optionally, you can merge the existing history with the new history here
            session.SetString("ChatHistory", JsonSerializer.Serialize(history));
        }
        return TypedResults.Ok(history);
    }

}
