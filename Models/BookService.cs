namespace BooksApi.Models;

[EnableCors("Policy")]
public class BookService
{
    public static async Task<IResult> GetAllBooks(LibraryDbContext db)
    {
        return TypedResults.Ok(await db.Books.ToListAsync());
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
