using System.Text.Json;

public class GroqAgent
{

    private readonly Groqlet _client;
    private readonly string _system;
    private List<Dictionary<string, string>> _messages;

    public GroqAgent(Groqlet client, string system = "")
    {
        _client = client;
        _system = system;
        _messages = new List<Dictionary<string, string>>();

        if (!string.IsNullOrWhiteSpace(system))
        {
            _messages.Add(new Dictionary<string, string>
            {
                { "role", "system" },
                { "content", system }
            });
        }
    }

    public async Task<string> CallAsync(string message = "")
    {
        if (!string.IsNullOrEmpty(message))
        {
            _messages.Add(new Dictionary<string, string> { { "role", "user" }, { "content", message } });
        }
        var result = await ExecuteAsync();
        _messages.Add(new Dictionary<string, string> { { "role", "assistant" }, { "content", result } });
        return result;
    }

    private async Task<string> ExecuteAsync()
    {

        var completion = await _client.SendChatCompletionRequestAsync(
            userMessage: JsonSerializer.Serialize(_messages),
            model: "llama3-8b-8192"
        );
        var responseJson = JsonSerializer.Deserialize<OpenAIApiResponse>(completion);
        Console.WriteLine($"Queue Time: {responseJson.usage.queue_time} seconds, Prompt Tokens: {responseJson.usage.prompt_tokens}, Prompt Time: {responseJson.usage.prompt_time} seconds, Completion Tokens: {responseJson.usage.completion_tokens}, Completion Time: {responseJson.usage.completion_time} seconds, Total Tokens: {responseJson.usage.total_tokens}, Total Time: {responseJson.usage.total_time} seconds");
        return responseJson.choices[0].message.content;
    }

}



public class OpenAIApiResponse
    {
        public string id { get; set; }

        //public string obj { get; set; }
        public int created { get; set; }
        public string model { get; set; }
        public List<Choice> choices { get; set; }
        public Usage usage { get; set; }
        public string system_fingerprint { get; set; }
        public XGroq x_groq { get; set; }
    }

    public class Choice
    {
        public int index { get; set; }
        public Message message { get; set; }
       // public JObject logprobs { get; set; }
        public string finish_reason { get; set; }
    }

    public class Message
    {
        public string role { get; set; }
        public string content { get; set; }
        public List<ToolCall> tool_calls { get; set; }
    }

    public class ToolCall
    {
        public string id { get; set; }
        public string type { get; set; }
        public Function function { get; set; }
        
    }

    public class Function
    {
        public string name { get; set; }
        public string arguments { get; set; }
    }

    public class Idea
    {
        public string idea { get; set; }
    }
    public class Usage
    {
        public double queue_time { get; set; }
        public int prompt_tokens { get; set; }
        public double prompt_time { get; set; }
        public int completion_tokens { get; set; }
        public double completion_time { get; set; }
        public int total_tokens { get; set; }
        public double total_time { get; set; }
    }

    public class XGroq
    {
        public string id { get; set; }
    }
