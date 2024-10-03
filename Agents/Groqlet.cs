using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;


public class Groqlet
{
    private readonly HttpClient _httpClient;
    private const string GroqApiUrl = "https://api.groq.com/openai/v1/chat/completions";
    private readonly ILogger<Groqlet> _logger;

    public Groqlet(HttpClient httpClient, string apiKey, ILogger<Groqlet> logger)
    {
        _httpClient = httpClient;
        _httpClient.BaseAddress = new Uri(GroqApiUrl);
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
         _logger = logger;
    }

    public async Task<string> SendChatCompletionRequestAsync(string userMessage, string model = "llama3-8b-8192", int maxTokens = 4096)
    {
        var requestBody = new
        {
            messages = new[]
            {
                new { role = "user", content = userMessage }
            },
            max_tokens = maxTokens,
            model
        };

            // Convert the request body to JSON
            var json = JsonSerializer.Serialize(requestBody);

            // Create a new request
            var request = new HttpRequestMessage(HttpMethod.Post, GroqApiUrl)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };

            // Send the request and get the response
            var response = await _httpClient.SendAsync(request);

            // Check if the response was successful
            if (response.IsSuccessStatusCode)
            {
                // Read the response content as JSON
                _logger.LogInformation($"Received response from Groq endpoint");
                return await response.Content.ReadAsStringAsync();;
            }
            return string.Empty;
    }
}
