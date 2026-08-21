namespace CraigSystemTest.Models;
public class EvaluatedTestCase
{
    public TestCase? TestCase { get; set; }
    public int Score { get; set; }
    public string? Explanation { get; set; }
    public bool Pass { get; set; }
}