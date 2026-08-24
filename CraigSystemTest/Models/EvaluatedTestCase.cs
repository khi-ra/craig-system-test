namespace CraigSystemTest.Models;
public class EvaluatedTestCase
{
    public string? TestCaseId { get; set; }
    public string? Justification { get; set; }
    public int Score { get; set; }
    public bool Pass { get; set; }
}