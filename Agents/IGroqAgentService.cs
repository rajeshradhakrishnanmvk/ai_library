public interface IGroqAgentService
{
    Task<string> AskAgent(string query);
}
