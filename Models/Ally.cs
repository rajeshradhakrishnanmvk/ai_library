public class Ally
{
    public string? Name { get; set; }
    public string? Instructions { get; set; }

    public List<ConversationHistory> History { get; } = new();
}
public class ConversationHistory
{
    public string Question { get; set; }
    public string? Answer { get; set; }
}
