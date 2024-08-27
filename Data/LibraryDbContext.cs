using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.EntityFrameworkCore;
using BooksApi.Models;

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
            //modelBuilder.Entity<Book>().HasData(GetBooks());
            modelBuilder.Entity<Book>().OwnsOne(
            book => book.Partner, ownedNavigationBuilder =>
            {
                ownedNavigationBuilder.ToJson();
                ownedNavigationBuilder.OwnsMany(ally => ally.History);
            });//.HasData(GetBooks());
            
        }

        private static IEnumerable<Book> GetBooks()
        {
            string[] p = { Directory.GetCurrentDirectory(), "wwwroot", "Books_Assistant.csv" };
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
