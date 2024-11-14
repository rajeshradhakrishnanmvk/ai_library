using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
public class GroqAgentService : IGroqAgentService
{

    private Groqlet _client; 
    private static readonly string systemPrompt = @"
    You are a highly intelligent literary agent, helping readers choose their next book based on textual analysis.\n\n 
    Individuals ask you questions about which book to read next,\n
    and you run in a loop of Thought, Action, PAUSE, Observation.\n
    At the end of the loop, you output an Answer to guide the reader.\n\n
    
    Your task is to perform a deep analysis of book recommendations by evaluating available texts and identifying key insights.\n
    You will assess texts based on their themes, target audience, writing style,\n
    and how well they engage the reader.\n\n
    
    You will follow this process:\n\n
    
    Thought: Describe your thoughts about the user's query and the textual content you are analyzing.\n 
    Action: Perform one of the available actions to extract insights from the text or data, then return PAUSE. \n
    Observation: Reflect on the results of your action and refine your understanding. \n
    Answer: Provide a recommendation to the user based on your analysis. \n
    
    Available actions: \n
    textual_analysis: input a book name in the self but not yet read.\n
    Analyze input book name the structure, thesis, and evidence in a given book or text.\n
    get_books_self: input the author name, Retrieve a list of books of the input author\n
     relevant to the user'\''s interests or past reading history. \n
    
    Example session: \n\n
    
    Question: What book should I read next, considering I enjoy deep philosophical texts and self-reflection?\n\n
    Thought: This user enjoys philosophical texts, so I need to analyze a selection of books in that genre, \n
    focusing on their core themes, structure, and audience. \n\n
    Action: get_books_self: Salems Lot, The Shining, The Stand, Rage, The Long Walk\n

    PAUSE \n

    You will be called again with this:\n\n

    Observation: I have retrieved a list of books on philosophy and self-reflection.\n
    Now, I need to analyze the key themes and relevance of each book for the user. 

    Thought: Based on my initial analysis, these books explore deep philosophical questions.\n
    I will use textual analysis to better understand their themes and impact.\n\n

    Action: 
    textual_analysis: a random excerpt from the book Salems lot is: The town knew about darkness. It knew the secrets kept in cellars and attics, in places behind closed doors. In the Lot, darkness wasn’t just a condition; it was something that seemed to have weight, like fog. It was something that pressed down, that isolated.\n
    
    PAUSE \n
    You will be called again with this:\n\n 

    Observation: Stephen King'\''s writing masterfully brings out a feeling of dread and small-town familiarity—elements that make this horror novel so haunting,\n
    aligning well with the user'\''s preferences.\n\n
    
    If you have the answer, output it as the Answer. 
    
    Answer: Based on your interest in philosophical and reflective texts, I recommend Salems Lot.\n
    It delves into existentialism and self-identity in a thought-provoking and engaging manner, \n
    which I believe aligns perfectly with your interests.\n\n
    
    Now it's your turn:";  
    public GroqAgentService(Groqlet client)
    {
        _client = client;
    }

    public async IAsyncEnumerable<string> AskAgent(string query, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
       await foreach (var result in LoopAsync(query, cancellationToken: cancellationToken))
       {
           yield return result;
       }
    }


    public async IAsyncEnumerable<string> LoopAsync(
    string query, 
    int maxIterations = 5, 
    [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var agent = new GroqAgent(_client, systemPrompt.Trim());
        var tools = new List<string> { "textual_analysis", "get_books_self" };
        var nextPrompt = query;

        // Exit after `maxIterations` of the `while` loop or if the answer is found
        for (int iterationCount = 0; iterationCount < maxIterations; iterationCount++)
        {
            if (cancellationToken.IsCancellationRequested)
                yield break;

            for (int i = 0; i < maxIterations; i++)
            {
                var result = await agent.CallAsync(nextPrompt);
                yield return result;

                // Break the loop if an answer is found
                if (result.Contains("Answer"))
                {
                    yield break;
                }

                if (result.Contains("PAUSE") && result.Contains("Action"))
                {
                    var actionMatch = Regex.Match(result, @"Action: ([a-z_]+): (.+)", RegexOptions.IgnoreCase);
                    if (actionMatch.Success)
                    {
                        var chosenTool = actionMatch.Groups[1].Value;
                        var arg = actionMatch.Groups[2].Value;

                        if (tools.Contains(chosenTool))
                        {
                            var resultTool = await ExecuteToolAsync(chosenTool, arg);
                            nextPrompt = $"Observation: {resultTool}";
                        }
                        else
                        {
                            nextPrompt = "Observation: Tool not found";
                        }
                        yield return nextPrompt;
                        continue;
                    }
                }


            }
        }
    }
    private static async Task<string> ExecuteToolAsync(string toolName, string argument)
    {
        // Simulate tool execution based on the tool name and argument
        switch (toolName.ToLower())
        {
            case "textual_analysis":
                return await TextualAnalysisAsync(argument);
            case "get_books_self":
                return await GetBooksSelfAsync(argument);
            default:
                return await Task.FromResult($"Unknown tool: {toolName}");
        }
    }

    private static Task<string> TextualAnalysisAsync(string text)
    {
        Console.WriteLine($"TextualAnalysisAsync {text}");
        return Task.FromResult($"a random excerpt from the book '{text}' is");
    }

    private static Task<string> GetBooksSelfAsync(string author)
    {
        Console.WriteLine($"GetBooksSelfAsync {author}");
        return Task.FromResult($"retrieved a list of five books by {author}");
    }
}
