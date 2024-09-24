using CsvHelper;
using CsvHelper.Configuration;
using System.Globalization;

public interface IBookBulkInserter
{
    Task BulkInsertBooksFromCsv(string csvFilePath);
}

public class BookBulkInserter : IBookBulkInserter
{
    private readonly LibraryDbContext _context;

    public BookBulkInserter(LibraryDbContext dbContext)
    {
        _context = dbContext;
    }

    public async Task BulkInsertBooksFromCsv(string csvFilePath)
    {
        var books = new List<Book>();

        var csvConfig = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            PrepareHeaderForMatch = args => args.Header.ToLower(), // Ensure header matching is case-insensitive
        };

        // Step 1: Read the CSV file
        using (var reader = new StreamReader(csvFilePath))
        using (var csv = new CsvReader(reader, csvConfig))
        {
            books = csv.GetRecords<Book>().ToList();
        }

        // Step 2: Delete all existing books from the database
        var existingBooks = _context.Books.ToList();
        
        if (existingBooks.Any())
        {
            _context.Books.RemoveRange(existingBooks);
            await _context.SaveChangesAsync(); // Save changes to delete books
        }

        // Step 3: Add the new books to the database
        foreach (var book in books)
        {
            _context.Books.Add(book);
        }

        // Step 4: Save all changes (insert new books)
        await _context.SaveChangesAsync();
    }

            
}
