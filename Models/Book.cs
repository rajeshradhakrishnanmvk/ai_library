namespace BooksApi.Models;
using System.Text.Json.Serialization;

public class Book
{
    [JsonPropertyName("bookId")]
    public int BookId { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("author")]
    public string? Author { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("library")]
    public string? Library { get; set; }
}
