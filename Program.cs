var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<LibraryDbContext>(option => option.UseSqlite(connectionString));

// Add Cors
builder.Services.AddCors(o => o.AddPolicy("Policy", builder => {
  builder.AllowAnyOrigin()
    .AllowAnyMethod()
    .AllowAnyHeader();
}));


var app = builder.Build();

app.UseStaticFiles();  // For the wwwroot folder

app.UseCors("Policy");

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<JsonToHtmlMiddleware>();

var books = app.MapGroup("/api/books");

app.MapGet("/api/books/", BookService.GetAllBooks);
app.MapGet("/api/books/library/{library}", BookService.GeBooksByLibrary);
app.MapGet("/api/books/{id:int}", BookService.GetBookById);

app.MapPost("/api/books/", BookService.InsertBook);
app.MapPut("/api/books/{id:int}", BookService.UpdateBook);
app.MapDelete("/api/books/{id:int}", BookService.DeleteBook);

app.MapGet("/api/books/{id:int}/update",BookService.GetBookById);


app.Run();
