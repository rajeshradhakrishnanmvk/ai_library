using System.Text.Json;


namespace BooksApi.Middleware;
public class JsonToHtmlMiddleware
{
    private readonly RequestDelegate _next;
    private  HttpContext _context;
    public JsonToHtmlMiddleware(RequestDelegate next)
    {
        _next = next;
    }


    public async Task InvokeAsync(HttpContext context)
    {
        _context = context;
        // Capture the original response body stream
        var originalBodyStream = context.Response.Body;

        // Use a memory stream to temporarily store the response
        using (var newBodyStream = new MemoryStream())
        {


            context.Response.Body = newBodyStream;

            // Continue processing the request
            await _next(context);



            // Check if the response content type is JSON
            if (context.Response.ContentType != null && context.Response.ContentType.Contains("application/json"))
            {
                // Read the JSON from the temporary body stream
                newBodyStream.Seek(0, SeekOrigin.Begin);
                var jsonResponse = await new StreamReader(newBodyStream).ReadToEndAsync();
                var htmlResponse = string.Empty;
                if (null != context 
                    && context.Request.Path.StartsWithSegments("/api/books") 
                    && context.Request.Path.Value.EndsWith("/update"))
                {
                    var idString = context.Request.Path.Value.Split('/')[3];
                    if (int.TryParse(idString, out int id))
                    {
                        htmlResponse = ConvertJsonToHtml(jsonResponse, "update");
                    }

                }
                else  if (context.Request.Path.StartsWithSegments("/api/chat") ||
                context.Request.Path.StartsWithSegments("/api/exportChat"))
                {
                    htmlResponse = ConvertChatToHtml(jsonResponse);
                    // await _next(context);
                    // return;
                }
                else{
                    // Convert the JSON to HTML
                    htmlResponse = ConvertJsonToHtml(jsonResponse);
                    
                }
                // Replace the response content with the HTML
                context.Response.ContentType = "text/html";
                context.Response.Body = originalBodyStream;
                await context.Response.WriteAsync(htmlResponse);
            }
            else
            {
                // If not JSON, just copy the original stream back
                newBodyStream.Seek(0, SeekOrigin.Begin);
                await newBodyStream.CopyToAsync(originalBodyStream);
            }
        }
    }
    private string ConvertChatToHtml(string jsonResponse)
    {
        var jsonDocument = JsonDocument.Parse(jsonResponse);
        var htmlBuilder = new System.Text.StringBuilder();
        //var history = jsonDocument.RootElement.GetProperty("history");
        // var completionTokens = jsonDocument.RootElement.GetProperty("completionTokens").GetInt32();
        // var promptTokens = jsonDocument.RootElement.GetProperty("promptTokens").GetInt32();
        // var totalTokens = jsonDocument.RootElement.GetProperty("totalTokens").GetInt32();

        var arrayItems = jsonDocument.RootElement.EnumerateArray().ToList();

        foreach (var item in arrayItems)
        {
            var messageId = $"chat-message-{arrayItems.IndexOf(item)}";
            var author = item.GetProperty("role").GetProperty("label").GetString();
            var content = item.GetProperty("items")[0].GetProperty("text").GetString();
            var imgSrc = author == "user" 
                ? "https://gramener.com/comicgen/v1/comic?name=dee&angle=side&emotion=happy&pose=explaining&box=&boxcolor=%23000000&boxgap=&mirror=" 
                : "https://gramener.com/comicgen/v1/comic?name=ava&emotion=angry&pose=angry&shirt=%23b1dbf2&box=&boxcolor=%23000000&boxgap=&mirror=";

            htmlBuilder.AppendFormat("<div id='{0}' class='chat-message {1}'>", messageId, author.ToLower());
            htmlBuilder.AppendFormat("<img src='{0}' />", imgSrc);
            htmlBuilder.Append("<div class='bubble'>");
            htmlBuilder.AppendFormat("<p>{0}</p>", content);
            // if (author == "assistant")
            // {
            //     htmlBuilder.Append("<div class='completion-tokens'>");
            //     htmlBuilder.AppendFormat("<p>Completion Tokens: {0}</p>", completionTokens);
            //     htmlBuilder.AppendFormat("<p>Prompt Tokens: {0}</p>", promptTokens);
            //     htmlBuilder.AppendFormat("<p>Total Tokens: {0}</p>", totalTokens);
            //     htmlBuilder.Append("</div>");
            // }
            htmlBuilder.Append("</div>");
            htmlBuilder.Append("</div>");
        }

        return htmlBuilder.ToString();
    }
    private string ConvertJsonToHtml(string jsonResponse, string mode)
    {
        var jsonDocument = JsonDocument.Parse(jsonResponse);
        var res = jsonDocument.RootElement.TryGetProperty("value", out var valueElement) ? valueElement : jsonDocument.RootElement;
        var htmlxContent = $@"
            <tr hx-trigger='cancel' class='editing' hx-get='/api/book/{res.GetProperty("bookId").GetInt32()}' >
                <td><input name='bookId' value='{res.GetProperty("bookId").GetInt32()}' disabled></td>
                <td><input name='name' value='{res.GetProperty("name").GetString()}'></td>
                <td><input name='author' value='{res.GetProperty("author").GetString()}'></td>
                <td><input name='description' value='{res.GetProperty("description").GetString()}'></td>
                <td><input name='library' value='{res.GetProperty("library").GetString()}'></td>
                <td>
                    <button class='btn danger' hx-get='/api/books/{res.GetProperty("bookId").GetInt32()}'>
                        Cancel
                    </button>
                    <button class='btn danger' hx-put='/api/books/{res.GetProperty("bookId").GetInt32()}' hx-ext='bookjson' hx-include='closest tr'>
                        Save
                    </button>
                </td>
            </tr>"; 
        return htmlxContent;
    }
    private string ConvertJsonToHtml(string jsonResponse)
    {
        var jsonDocument = JsonDocument.Parse(jsonResponse);
        var htmlBuilder = new System.Text.StringBuilder();

        // If root element is an object, check for pagedBooks and nextCursor
        if (jsonDocument.RootElement.ValueKind == JsonValueKind.Object)
        {
            // Check if 'pagedBooks' exists and is an array
            if (jsonDocument.RootElement.TryGetProperty("pagedBooks", out JsonElement pagedBooksElement) &&
                pagedBooksElement.ValueKind == JsonValueKind.Array)
            {
                var pagedBooks = pagedBooksElement;
                var nextCursor = jsonDocument.RootElement.GetProperty("nextCursor").GetInt32();

                // Process the array of books

                if (pagedBooks.GetArrayLength() > 0)
                {


                    // Add rows for each book in pagedBooks array
                    foreach (var item in pagedBooks.EnumerateArray())
                    {

                        int bookId = item.GetProperty("bookId").GetInt32();
                        //hx-get='/api/books/selected/{0}' hx-trigger='click' hx-target='#details' hx-swap='innerHTML'
                        htmlBuilder.AppendFormat("<tr>");
                        htmlBuilder.AppendFormat("<td><button class='btn primary' hx-get='/api/books/selected/{0}' hx-target='#details' hx-swap='innerHTML'>Select</button></td>", bookId);
                        foreach (var value in item.EnumerateObject())
                        {
                            htmlBuilder.AppendFormat("<td>{0}</td>", value.Value);
                        }


                        htmlBuilder.Append("<td><button class='btn danger'");
                        htmlBuilder.AppendFormat(" hx-get='/api/books/{0}/update'", bookId);
                        htmlBuilder.Append(" hx-trigger='edit'");
                        htmlBuilder.Append(" onClick=\"let editing = document.querySelector('.editing');\n");
                        htmlBuilder.Append("if(editing) {\n");
                        htmlBuilder.Append("Swal.fire({title: 'Already Editing',\n");
                        htmlBuilder.Append("showCancelButton: true,\n");
                        htmlBuilder.Append("confirmButtonText: 'Yep, Edit This Row!',\n");
                        htmlBuilder.Append("text:'Hey! You are already editing a row! Do you want to cancel that edit and continue?'})\n");
                        htmlBuilder.Append(".then((result) => {\n");
                        htmlBuilder.Append("if(result.isConfirmed) {\n");
                        htmlBuilder.Append("htmx.trigger(editing, 'cancel');\n");
                        htmlBuilder.Append("htmx.trigger(this, 'edit');\n");
                        htmlBuilder.Append("}\n");
                        htmlBuilder.Append("});\n");
                        htmlBuilder.Append("} else {\n");
                        htmlBuilder.Append("htmx.trigger(this, 'edit');\n");
                        htmlBuilder.Append("}\">Edit</button></td>");
                        htmlBuilder.AppendFormat("<td><button class='btn danger' hx-delete='/api/books/{0}'>Delete</button></td>", bookId);
                        htmlBuilder.Append("</tr>");
                    }

                    // Add "Load More" button if nextCursor exists
                        htmlBuilder.Append("<tr id='replaceMe'>");
                        htmlBuilder.Append("<td colspan='6'>");
                        htmlBuilder.AppendFormat("<button class='btn primary' hx-get='/api/books/{0}' hx-target='#replaceMe' hx-swap='outerHTML'>Load More Books...</button>", nextCursor);
                        htmlBuilder.Append("</td>");
                        htmlBuilder.Append("</tr>");


                }
            }
            else if (jsonDocument.RootElement.TryGetProperty("value", out var bookElement) &&
                bookElement.ValueKind == JsonValueKind.Object)
            {
                // Deserialize the JSON response to get book details
                var bookDetails = JsonSerializer.Deserialize<Book>(bookElement.GetRawText().Trim());

                if (bookDetails != null)
                {
                    // Serialize the book details back to a JSON string
                    var correctedJson = JsonSerializer.Serialize(bookDetails);

                    // Set the session variable with the corrected JSON string
                    _context.Session.SetString("SelectedBook", correctedJson);
                }

                htmlBuilder.Append("<div>");
                htmlBuilder.AppendFormat("<h3>{0}:{1}</h3>", bookElement.GetProperty("bookId").GetInt32(),bookElement.GetProperty("name").GetString());
                htmlBuilder.AppendFormat("<p>Author: {0}</p>",bookElement.GetProperty("author").GetString());
                htmlBuilder.AppendFormat("<p>Description: {0}</p>",bookElement.GetProperty("description").GetString());
                htmlBuilder.AppendFormat("<p>Library: {0}</p>",bookElement.GetProperty("library").GetString());
                htmlBuilder.Append("</div>");

            }
            else
            {
                // Single book case (Object without pagedBooks array)
                var book = jsonDocument.RootElement;
                int bookId = book.GetProperty("bookId").GetInt32();
                htmlBuilder.AppendFormat("<td><button class='btn primary' hx-get='/api/books/selected/{0}' hx-target='#details' hx-swap='innerHTML'>Select</button></td>", bookId);
                foreach (var value in book.EnumerateObject())
                {
                    htmlBuilder.AppendFormat("<td>{0}</td>", value.Value);
                }

                
                htmlBuilder.Append("<td><button class='btn danger'");
                htmlBuilder.AppendFormat(" hx-get='/api/books/{0}/update'", bookId);
                htmlBuilder.Append(" hx-trigger='edit'>Edit</button></td>");
                htmlBuilder.AppendFormat("<td><button class='btn danger' hx-delete='/api/books/{0}'>Delete</button></td>", bookId);
                htmlBuilder.Append("</tr>");


            }
        }

        return htmlBuilder.ToString();
    }

}
