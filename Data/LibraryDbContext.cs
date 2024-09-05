using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;


namespace BooksApi.Data
{
    public class LibraryDbContext : DbContext
    {
        public DbSet<Book> Books => Set<Book>();

        public LibraryDbContext(DbContextOptions<LibraryDbContext> options)
                : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<Book>().HasData(GetBooks());

            
        }

        private static IEnumerable<Book> GetBooks()
        {
            string[] p = { Directory.GetCurrentDirectory(), "wwwroot", "Books.csv" };
            var csvFilePath = Path.Combine(p);

            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                PrepareHeaderForMatch = args => args.Header.ToLower(),
            };

            var data = new List<Book>().AsEnumerable();
            using (var reader = new StreamReader(csvFilePath))
            {
                using (var csvReader = new CsvReader(reader, config))
                {
                    data = (csvReader.GetRecords<Book>()).ToList();
                }
            }

            return data;
        }

    }

}
