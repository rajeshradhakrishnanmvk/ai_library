
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
    public async Task<IResult> AIResponse(BookMessage message)
    {
        var session = _httpContextAccessor.HttpContext.Session;
        var historyJson = session.GetString("ChatHistory");
        var history = string.IsNullOrEmpty(historyJson) ? new ChatHistory("You are a helpful assistant and you know about the author Stephen King, he is the king of horror") : JsonSerializer.Deserialize<ChatHistory>(historyJson);

        history.AddUserMessage(message.Content);

        var result = await _chatCompletionService.GetChatMessageContentAsync(
            history,
            kernel: _kernel);

        session.SetString("ChatHistory", JsonSerializer.Serialize(history));
        history.AddAssistantMessage(result.Content);
        return TypedResults.Ok(history);
    }
}