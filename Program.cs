using Microsoft.SemanticKernel;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddOpenAIChatCompletion(
    modelId: "gpt-3.5-turbo",
    apiKey: "",
    orgId: ""
);
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<LibraryDbContext>(option => option.UseSqlite(connectionString));

// Add Cors
builder.Services.AddCors(o => o.AddPolicy("Policy", builder => {
  builder.AllowAnyOrigin()
    .AllowAnyMethod()
    .AllowAnyHeader();
}));

builder.Services.AddTransient((serviceProvider)=> {
    return new Kernel(serviceProvider);
});

// Register ChatService and other necessary services
builder.Services.AddSingleton<ChatService>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();

app.UseSession();

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

app.MapPost("/api/chat", async (BookMessage message, ChatService chatService) =>
{
    var result = await chatService.AIResponse(message);
    return result;
});


app.Run();
