namespace BooksApi.Models;

// public class Book
// {

//     public int BookId { get; set; }
//     public string? Name { get; set; }
//     public string? Author { get; set; }
//     public string? Description { get; set; }

//     public string? Library { get; set; }

// }


using System.Text.Json;
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
