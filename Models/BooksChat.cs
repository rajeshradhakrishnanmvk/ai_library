    namespace BooksApi.Models;
    using System.Text.Json.Serialization;
    public class BooksChat
    {
        public BooksChat(string role, string message)
        {
            Role = role;
            Message = message;
        }

        [JsonPropertyName("Role")]
        public string Role { get; set; }

        [JsonPropertyName("Message")]
        public string Message { get; set; }
    }