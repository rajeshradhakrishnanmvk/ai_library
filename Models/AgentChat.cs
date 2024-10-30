    namespace BooksApi.Models;
    
    public class BooksChat
    {
        public BooksChat(string role, string message)
        {
            Role = role;
            Message = message;
        }

        public string Role { get; set; }
        public string Message { get; set; }
    }