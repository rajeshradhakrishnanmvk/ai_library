namespace BooksApi.Models;

public class Book
{

    public int BookId { get; set; }
    public string? Name { get; set; }
    public string? Author { get; set; }
    public string? Description { get; set; }

    public string? Library { get; set; }

}
