namespace BooksApi.Models;
using System.Text.Json.Serialization;
public class BooksDetails
{
    [JsonPropertyName("AgentName")]
    public string? AgentName { get; set; }

    [JsonPropertyName("AgentInstruction")]
    public string? AgentInstruction { get; set; }

    [JsonPropertyName("BooksChat")]
    public IEnumerable<BooksChat> BooksChat { get; set; } = new List<BooksChat>();

}