namespace CraigSystemTest.Services;

using System.Management;
using System.Text.Json;

using CraigSystemTest.Models;

public class TestRunner(Judge judge, TestCaseLoader loader)
{
    private readonly TestCaseLoader _loader = loader;
    private readonly Judge _judge = judge;

    public async Task RunAsync(string[] testFiles)
    {
        foreach (string file in testFiles)
        {
            string filePath = Path.Combine(AppContext.BaseDirectory, "Data", file);

            List<TestCase> tests = _loader.Load(filePath);
            
            List<EvaluatedTestCase> evaluatedTests = await RunTestCasesAsync(tests);

            for (int i = 0; i < evaluatedTests.Count; i++)
            {
                PrintEvaluatedTests(tests[i], evaluatedTests[i]);
            }
        }
    }

    public async Task<List<EvaluatedTestCase>> RunTestCasesAsync(List<TestCase> testCases)
    {
        var results = new List<EvaluatedTestCase>();
        foreach (var testCase in testCases)
        {
            Console.WriteLine($"Evaluating test case {testCase.Id}\n");
            try
            {
                var result = await _judge.EvaluateAsync(testCase);
                results.Add(result);
            }
            catch (Exception ex)
            {
                results.Add(new EvaluatedTestCase
                {
                    TestCaseId = testCase.Id,
                    Score = 0,
                    Justification = $"Evaluation error: {ex.Message}"
                });
            }
        }
        return results;
    }

    private static void PrintEvaluatedTests(TestCase test, EvaluatedTestCase evaluatedTest)
    {
        JsonSerializerOptions options = new()
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = true
        };

        Console.WriteLine("=============================");
        Console.WriteLine($"EVALUATION OF TEST CASE {evaluatedTest.TestCaseId}");
        Console.WriteLine("=============================");

        Console.WriteLine($"Test Case:{JsonSerializer.Serialize<TestCase>(test, options)}\n\n");
        Console.WriteLine($"Evaluation: {JsonSerializer.Serialize<EvaluatedTestCase>(evaluatedTest, options)}\n\n");
    } 
}