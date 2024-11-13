
using System.Text.Json;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using PdfSharpCore.Pdf;
using PdfSharpCore.Drawing;
using PdfSharpCore.Drawing.Layout;
using System.Text;


    // What is the book or central idea of the text?
    // Who is the intended audience?
    // What questions does the author address?
    // How does the author structure the text?
    // What are the key parts of the text?
    // How do the key parts of the text interrelate?
    // How do the key parts of the text relate to the book?
    // What does the author do to generate interest in the argument?
    // How does the author convince the readers of their argument’s merit?
    // What evidence is provided in support of the book?
    // Is the evidence in the text convincing?
    // Has the author anticipated opposing views and countered them?
    // Is the author’s reasoning sound?


#pragma warning disable  SKEXP0001

[EnableCors("Policy")]
public class ChatService
{
    private readonly Kernel _kernel;
    private readonly IChatCompletionService _chatCompletionService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ChatService(Kernel kernel, IHttpContextAccessor httpContextAccessor)
    {
        _kernel = kernel;
        _httpContextAccessor = httpContextAccessor;
        _chatCompletionService = _kernel.GetRequiredService<IChatCompletionService>();
    }
    //create a get method for history
    public IResult GetHistory()
    {
        var session = _httpContextAccessor.HttpContext.Session;
        var historyJson = session.GetString("ChatHistory");
        var selectedBook = session.GetString("SelectedBook");
        var selBook = JsonSerializer.Deserialize<Book>(selectedBook);
        //var history = string.IsNullOrEmpty(historyJson) ? new ChatHistory($"You are a helpful assistant and you know about the author {selBook?.Author ?? "Stephen King"}, about the book {selBook?.Name ?? "Bag of Bones"} which was published during {selBook?.Description ?? "1998 "}") : JsonSerializer.Deserialize<ChatHistory>(historyJson);
        var history = new ChatHistory(selBook?.BooksDetails.AgentInstruction);
        return TypedResults.Ok(history);
    }
    
    public async Task<IResult> AIResponse(BookMessage message)
    {
        var session = _httpContextAccessor.HttpContext.Session;
        var historyJson = session.GetString("ChatHistory");
        var selectedBook = session.GetString("SelectedBook");
        var selBook = JsonSerializer.Deserialize<Book>(selectedBook);
        var history = string.IsNullOrEmpty(historyJson) ?  
                      new ChatHistory(selBook?.BooksDetails.AgentInstruction) : 
                      JsonSerializer.Deserialize<ChatHistory>(historyJson);
        //var history = new ChatHistory(selBook?.BooksDetails.AgentInstruction);
        history.AddUserMessage(message.Content);
       

        var result = await _chatCompletionService.GetChatMessageContentAsync(
            history,
            kernel: _kernel);
        var completionTokens = 0;
        var promptTokens = 0;
        var totalTokens = 0;
        //get the key value pair from the metadata "Usage" which is of type Azure.AI.OpenAI.CompletionsUsage
        if (result.Metadata is Dictionary<string, object> metadataDict 
        && metadataDict.TryGetValue("Usage", out var metadataObj) 
        && metadataObj is Dictionary<string, object> metadata)
        {
            if (metadata.TryGetValue("CompletionsUsage", out var completionsUsageObj) 
            && completionsUsageObj is Azure.AI.OpenAI.CompletionsUsage completionsUsage)
            {
                completionTokens = completionsUsage.CompletionTokens;
                promptTokens = completionsUsage.PromptTokens;
                totalTokens = completionsUsage.TotalTokens;
            }
        }


        history.AddAssistantMessage(result.Content);
        if (selBook?.BooksDetails?.BooksChat is List<BooksChat> booksChatList)
        {
            booksChatList.Add(new BooksChat("assistant", result.Content));
        }
        session.SetString("ChatHistory", JsonSerializer.Serialize(history));
        //send completionTokens and promptTokens to the client
        //return TypedResults.Ok(new { history, completionTokens, promptTokens, totalTokens });
       return TypedResults.Ok(history);
    }
    
    public IResult AddHistory(ChatHistory history)
    {
        var session = _httpContextAccessor.HttpContext.Session;
        var historyJson = session.GetString("ChatHistory");

        if (string.IsNullOrEmpty(historyJson))
        {
            // Add the new chat history if there isn't one
            session.SetString("ChatHistory", JsonSerializer.Serialize(history));
        }
        else
        {
            // Merge or update the chat history (optional: merge logic here)
            session.SetString("ChatHistory", JsonSerializer.Serialize(history));
        }

        // Return the history as JSON
        return Results.Json(history); // Proper JSON response
    }

    public byte[] DownloadPdf(ChatHistory history)
    {
        using (var memoryStream = new MemoryStream())
        {
            // Create a new PDF document
            PdfDocument pdf = new PdfDocument();
            pdf.Info.Title = "Chat History PDF";

            // Create an empty page
            PdfPage page = pdf.AddPage();
            XGraphics gfx = XGraphics.FromPdfPage(page);

            // Set up the fonts
            XFont roleFont = new XFont("Verdana", 12, XFontStyle.Bold);
            XFont messageFont = new XFont("Verdana", 10, XFontStyle.Regular);

            // Variables to track the position on the page
            double yPos = 20;
            const double lineHeight = 20;

            // Iterate through the chat history and add to the document
            foreach (var chat in history)
            {
                // Add the role (e.g., assistant/user)
                string roleLabel = chat.Role.Label;
                gfx.DrawString($"{roleLabel}:", roleFont, XBrushes.Black, new XRect(10, yPos, page.Width, page.Height), XStringFormats.TopLeft);
                yPos += lineHeight;

                // Add the message text
                foreach (var item in chat.Items)
                {
                    // Initialize text formatter for word wrapping
                    XTextFormatter tf = new XTextFormatter(gfx);

                    // Define the rectangle for text to wrap within
                    XRect textRect = new XRect(10, yPos, page.Width - 20, page.Height - yPos);

                    // Draw the text inside the rectangle
                    tf.DrawString(item.ToString(), messageFont, XBrushes.Black, textRect, XStringFormats.TopLeft);

                    // Move the yPos for the next line
                    yPos += lineHeight;
                }

                // Add some spacing between messages
                yPos += lineHeight;

                // Create a new page if the current one is full
                if (yPos >= page.Height - 40)
                {
                    page = pdf.AddPage();
                    gfx = XGraphics.FromPdfPage(page);
                    yPos = 20;
                }
            }

            // Save the document to the memory stream
            pdf.Save(memoryStream);
            return memoryStream.ToArray(); // Return the PDF as byte array
        }
    }

}
