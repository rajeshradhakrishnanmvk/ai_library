namespace BooksApi.Models;
using System.Text.Json.Serialization;
public class BooksDetails
{
    [JsonPropertyName("BooksChat")]
    public BooksChat BooksChat { get; set; } = null!;

    [JsonPropertyName("AgentName")]
    public string? AgentName { get; set; }

    [JsonPropertyName("AgentInstruction")]
    public string? AgentInstruction { get; set; }
}