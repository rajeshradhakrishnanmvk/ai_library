using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging.Console;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

var configuration = builder.Configuration;
builder.Services.Configure<GroqApiSettings>(configuration.GetSection("GroqApiSettings"));
builder.Services.Configure<OpenApiSettings>(configuration.GetSection("OpenApiSettings"));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var openApiSettings = new OpenApiSettings();
configuration.GetSection("OpenApiSettings").Bind(openApiSettings);

builder.Services.AddOpenAIChatCompletion(
    modelId: openApiSettings.ModelId,
    apiKey: openApiSettings.ApiKey,
    orgId: openApiSettings.OrgId
);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<LibraryDbContext>(option => option.UseSqlite(connectionString));
builder.Services.AddTransient<IBookBulkInserter>(provider =>
    new BookBulkInserter(provider.GetRequiredService<LibraryDbContext>()));
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
builder.Services.AddHttpClient();
//builder.Services.AddTransient<GroqApiSettings>();
builder.Services.AddTransient<GroqApiSettings>((provider) =>
{
    var config = provider.GetService<IConfiguration>();
        return new GroqApiSettings()
    {
        ApiKey = config.GetValue<string>("GroqApiSettings:ApiKey"),
        // Add other settings as needed
    };

});
builder.Services.AddTransient<Groqlet>(provider =>
{
    var httpClient = provider.GetRequiredService<IHttpClientFactory>().CreateClient();
    var apiSettings = provider.GetRequiredService<GroqApiSettings>();
    var logger = provider.GetService<ILogger<Groqlet>>();
    return new Groqlet(httpClient, apiSettings.ApiKey, logger);
});


builder.Services.AddTransient<IGroqAgentService, GroqAgentService>();

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

//app.MapGet("/api/books/", BookService.GetAllBooks);
//app.MapGet("/api/books/library/{library}", BookService.GeBooksByLibrary);
app.MapGet("/api/books/selected/{id:int}", BookService.GetBookById);
app.MapGet("/api/books/{cursor:int}", async (int cursor, LibraryDbContext db) => 
{
    return await BookService.CursorPagination(cursor, db);
});



app.MapPost("/api/books/search", async (SearchRequest request, LibraryDbContext db) =>
{
   return await BookService.GeBooksByName(request.Search, db);;
});

app.MapPost("/api/books/", BookService.InsertBook);
app.MapPut("/api/books/{id:int}", BookService.UpdateBook);
app.MapDelete("/api/books/{id:int}", BookService.DeleteBook);

app.MapGet("/api/books/{id:int}/update",BookService.GetBookById);

app.MapGet("/api/chat",  (ChatService chatService) =>
{
    var result =  chatService.GetHistory();
    return result;
});

app.MapPost("/api/chat", async (BookMessage message, ChatService chatService) =>
{
    var result = await chatService.AIResponse(message);
    return result;
});

app.MapPost("/api/exportChat",  (ChatHistory message, ChatService chatService) =>
{
    var result =  chatService.AddHistory(message);
    return result;
});

app.MapPost("/api/upload", async (HttpRequest request, IBookBulkInserter inserter) =>
{
    if (!request.HasFormContentType || !request.Form.Files.Any())
    {
        return Results.BadRequest("No file provided.");
    }

    var file = request.Form.Files[0];
    var uploadsPath = Path.Combine(Directory.GetCurrentDirectory(), "uploads");
    
    if (!Directory.Exists(uploadsPath))
    {
        Directory.CreateDirectory(uploadsPath);
    }

    // Save the file
    var filePath = Path.Combine(uploadsPath, file.FileName);
    
    await using (var stream = new FileStream(filePath, FileMode.Create))
    {
        await file.CopyToAsync(stream);
    }

    // Now bulk insert the books from the uploaded CSV file
    await inserter.BulkInsertBooksFromCsv(filePath);

    return Results.Ok("File uploaded and books inserted successfully.");
});

//create a route for the agent /api/agent/ask
app.MapPost("/api/agent/ask", async (GroqAgentRequest request) =>
{
    var agentService = app.Services.GetRequiredService<IGroqAgentService>() as GroqAgentService;
    var result = await agentService.AskAgent(request.Query);
    return result;
});
app.Run();
