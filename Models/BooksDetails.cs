    namespace BooksApi.Models;
    public class BooksDetails
    {
        public BooksChat BooksChat { get; set; } = null!;
        public string? AgentName { get; set; }
        public string? AgentInstruction { get; set; }
    }