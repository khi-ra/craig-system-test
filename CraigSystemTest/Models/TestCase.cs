namespace CraigSystemTest.Models;
public class TestCase
{
    public string? Id { get; set; }
    public string? InputPrompt { get; set; }
    public string? Response { get; set; }
    public string? Reference { get; set; }
    public List<string>? Criteria { get; set; }
}