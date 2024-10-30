using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
public class GroqAgentService : IGroqAgentService
{

    private Groqlet _client; 
    private static readonly string systemPrompt = @"You are a highly intelligent literary agent, helping readers choose their next book based on textual analysis. Individuals ask you questions about which book to read next, and you run in a loop of Thought, Action, PAUSE, Observation. At the end of the loop, you output an Answer to guide the reader. Your task is to perform a deep analysis of book recommendations by evaluating available texts and identifying key insights. You will assess texts based on their themes, target audience, writing style, and how well they engage the reader.You will follow this process:Thought: Describe your thoughts about the user's query and the textual content you are analyzing. Action: Perform one of the available actions to extract insights from the text or data, then return PAUSE. Observation: Reflect on the results of your action and refine your understanding. Answer: Provide a recommendation to the user based on your analysis. Available actions: textual_analysis: Analyze the structure, thesis, and evidence in a given book or text. get_books_self: Retrieve a list of books relevant to the user'\''s interests or past reading history. Example session: Question: What book should I read next, considering I enjoy deep philosophical texts and self-reflection? Thought: This user enjoys philosophical texts, so I need to analyze a selection of books in that genre, focusing on their core themes, structure, and audience. Action: get_books_self: philosophical and reflective books PAUSE You will be called again with this: Observation: I have retrieved a list of books on philosophy and self-reflection. Now, I need to analyze the key themes and relevance of each book for the user. Thought: Based on my initial analysis, these books explore deep philosophical questions. I will use textual analysis to better understand their themes and impact. Action: textual_analysis: Book Title 1: A deep exploration of existentialism and personal identity PAUSE You will be called again with this: Observation: The book offers profound insights into personal identity and existentialism, aligning well with the user'\''s preferences. If you have the answer, output it as the Answer. Answer: Based on your interest in philosophical and reflective texts, I recommend Book Title 1. It delves into existentialism and self-identity in a thought-provoking and engaging manner, which I believe aligns perfectly with your interests. Now it's your turn:";  
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
    int maxIterations = 2, 
    [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var agent = new GroqAgent(_client, systemPrompt);
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
                //Console.WriteLine($"Result: {result}");
                yield return result;


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
                        //Console.WriteLine($"Next Prompt: {nextPrompt}");
                        yield return nextPrompt;
                        continue;
                    }
                }

                // Break the loop if an answer is found
                if (result.Contains("Answer"))
                {
                    Console.WriteLine($"Answer found: {result}");
                    yield break;
                    //break;
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
        return Task.FromResult($"from the recommended book, {text}, select key excerpts that represent central themes, arguments, or ideas. Evaluate the structure, thesis, intended audience, and evidence presented. Identify recurring patterns, relationships between ideas, and the author's techniques to persuade or inform the reader, comparing these across the books.");
    }

    private static Task<string> GetBooksSelfAsync(string query)
    {
        Console.WriteLine($"GetBooksSelfAsync {query}");
        return Task.FromResult($"Salems Lot, The Shining, The Stand, Rage, The Long Walk");
    }
}
