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
builder.Services.AddScoped<ChatService>();
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
   return await BookService.GetBooksByName(request.Search, db);
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

app.MapPost("/api/downloadpdf", (ChatHistory message, ChatService chatService) =>
{
    // Call the service to generate the PDF
    var pdfFile = chatService.DownloadPdf(message);

    // Return the PDF file to the client
    return Results.File(pdfFile, "application/pdf", "ChatHistory.pdf");
});
//create an post endpoint for updating the chat
app.MapPost("/api/updateChat", async (LibraryDbContext db, ChatService chatService) =>
{
    var result = await chatService.UpdateBooksChat();
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

app.MapGet("/api/agent/ask", async (HttpContext httpContext) =>
{
    httpContext.Response.ContentType = "text/event-stream";
    var query = httpContext.Request.Query["query"].ToString();

    if (string.IsNullOrWhiteSpace(query))
    {
        await httpContext.Response.WriteAsync("data: ERROR: Query parameter is required.\n\n");
        await httpContext.Response.Body.FlushAsync();
        return;
    }

    var agentService = httpContext.RequestServices.GetRequiredService<IGroqAgentService>() as GroqAgentService;
    var cancellationToken = httpContext.RequestAborted;

    try
    {
        // Iterate through the asynchronous enumerable returned by AskAgent
        int idx = 0;
        await foreach (var result in agentService.AskAgent(query, cancellationToken))
        {
            var sanitizedResult = result.Replace("\n", "||");
            //Console.WriteLine($"Batch: {idx}, Sending result: {sanitizedResult}");
            // Write each result to the response in the correct SSE format
            await httpContext.Response.WriteAsync($"data: {sanitizedResult}\n\n", cancellationToken);
            await httpContext.Response.Body.FlushAsync(cancellationToken);
        }
        // Indicate end of stream
        await httpContext.Response.WriteAsync("data: END||\n\n", cancellationToken);
        await httpContext.Response.Body.FlushAsync(cancellationToken);
    }
    catch (Exception ex)
    {
        var errorMessage = ex.Message.Replace("\n", "||");
        // Handle errors by sending them as SSE messages and then flushing
        await httpContext.Response.WriteAsync($"data: ERROR: {ex.Message}\n\n", cancellationToken);
        await httpContext.Response.Body.FlushAsync(cancellationToken);
    }
});




app.Run();
