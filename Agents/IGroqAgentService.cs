using System.Runtime.CompilerServices;
public interface IGroqAgentService
{
    //Task<string> AskAgent(string query);
    IAsyncEnumerable<string> AskAgent(string query, [EnumeratorCancellation] CancellationToken cancellationToken = default);
}
