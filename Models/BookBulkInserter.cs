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
        var books = new List<BulkBook>();

        var csvConfig = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            PrepareHeaderForMatch = args => args.Header.ToLower(), // Ensure header matching is case-insensitive
        };

        // Step 1: Read the CSV file
        using (var reader = new StreamReader(csvFilePath))
        using (var csv = new CsvReader(reader, csvConfig))
        {
            books = csv.GetRecords<BulkBook>().ToList();
        }

        // Step 2: Delete all existing books from the database
        _context.Books.RemoveRange(_context.Books);


        // Step 3: Add the new books to the database
        foreach (var book in books)
        {
            string instructions =  $"You are a helpful assistant and you know about the author {book?.Author ?? "Stephen King"}, about the book {book.Name ?? "Bag of Bones"} which was published during {book?.Description ?? "1998 "}";
            Book aiBook = new Book();
            aiBook.BookId = book.BookId;
            aiBook.Name = book?.Name;
            aiBook.Author = book?.Author;
            aiBook.Description = book?.Description;
            aiBook.Library = book?.Library;
            aiBook.BooksDetails = new BooksDetails 
                                { AgentName = $"Agent-{book?.Author}", 
                                AgentInstruction =instructions,
                                BooksChat = new List<BooksChat> { new BooksChat("system", instructions) }
                                };
            _context.Books.Add(aiBook);
        }

        // Step 4: Save all changes (insert new books)
        await _context.SaveChangesAsync();
    }

            
}
