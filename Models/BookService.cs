namespace BooksApi.Models;

[EnableCors("Policy")]
public class BookService
{    
    // Constants for pagination
    private const int PageSize = 2; // Number of items per page

    // Cursor-based pagination method
    public static async Task<IResult> CursorPagination(int cursor, LibraryDbContext db)
    {

        // Query to select the necessary book details
        var query = db.Books
            .Select(b => new Book
            {
                BookId = b.BookId,
                Name = b.Name,
                Author = b.Author,
                Description = b.Description,
                Library = b.Library
            });

        // Get the paged results based on the cursor and page size
        var pagedBooks = await query
            .Where(b => b.BookId > cursor)
            .Take(PageSize)
            .ToListAsync();

        // Get the next cursor if there are books
        var nextCursor = pagedBooks.Count > 0 ? pagedBooks[^1].BookId : (int?)null;

        // Return the paged data and the next cursor for pagination
        return TypedResults.Ok(new { pagedBooks, nextCursor });
    }
    public static async Task<IResult> GetAllBooks(LibraryDbContext db)
    {
        return TypedResults.Ok(await db.Books.ToListAsync());
    }

    public static async Task<IResult> GetBooksByName(string Name, LibraryDbContext db)
    {
        int nextCursor = 0;
        // Check if Name is empty or null
        if (string.IsNullOrWhiteSpace(Name))
        {
            // Return all books
            var allBooks = await db.Books.ToListAsync();
            
            return TypedResults.Ok(new { pagedBooks = allBooks, nextCursor });
        }
        
        var pagedBooks = await db.Books
                                .Where(b => b.Name!.ToLower().StartsWith(Name.ToLower()))
                                .ToListAsync();

        
        return TypedResults.Ok(new { pagedBooks, nextCursor });
    }

    public static async Task<IResult> GeBooksByLibrary(string Library, LibraryDbContext db)
    {
        return TypedResults.Ok(await db.Books.Where(t => t.Library!.ToLower() == Library.ToLower()).ToListAsync());
    }

    public static async Task<IResult> GetBookById(int id, LibraryDbContext db)
    {
        return TypedResults.Ok(await db.Books.FindAsync(id)
            is Book Book ? Results.Ok(Book) : Results.NotFound());
    }

    public static async Task<IResult> InsertBook(Book Book, LibraryDbContext db)
    {
        string instructions =  $"You are a helpful assistant and you know about the author {Book?.Author ?? "Stephen King"}, about the book {Book.Name ?? "Bag of Bones"} which was published during {Book?.Description ?? "1998 "}";

        Book.BooksDetails = new BooksDetails 
                            { AgentName = $"Agent-{Book.Author}", 
                              AgentInstruction =instructions,
                              BooksChat = new List<BooksChat> { new BooksChat("system", instructions) }
                              };
        db.Books.Add(Book);
        await db.SaveChangesAsync();

        return TypedResults.Created($"/Books/{Book.BookId}", Book);
    }

    public static async Task<IResult> UpdateBook(int id, Book inputBook, LibraryDbContext db)
    {
        var Book = await db.Books.FindAsync(id);

        if (Book is null) return Results.NotFound();

        Book.Name = inputBook.Name;
        Book.Author = inputBook.Author;
        Book.Library = inputBook.Library;

        await db.SaveChangesAsync();

        return TypedResults.Ok(Book);
    }

    public static async Task<IResult> DeleteBook(int id, LibraryDbContext db)
    {
        if (await db.Books.FindAsync(id) is Book Book)
        {
            db.Books.Remove(Book);
            await db.SaveChangesAsync();
            return TypedResults.Ok();
        }

        return TypedResults.NotFound();
    }
}
