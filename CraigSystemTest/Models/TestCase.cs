namespace CraigSystemTest.Models;
public class TestCase
{
    public string? Id { get; init; }
    public string? InputPrompt { get; set; }
    public string? Response { get; set; }
    public string? Reference { get; set; }
    public List<string>? Criteria { get; set; }
}