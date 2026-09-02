namespace CraigSystemTest.Services;

using System.Management;
using System.Text.Json;
using System.Linq;
using CraigSystemTest.Models;
using System.Text.Encodings.Web;

public class TestRunner(Judge judge, TestCaseLoader loader)
{
    private readonly TestCaseLoader _loader = loader;
    private readonly Judge _judge = judge;

    /// <summary>
    /// Loads each file from the given list into a list of TestCase objects 
    /// and hands the list off to be evaluated. For each file, prints each test case 
    /// and it's result, and average score of the file.
    /// </summary>
    /// <param name="testFiles">An array of file names containing test cases.</param>
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

            double averageScore = evaluatedTests.Count > 0 ? evaluatedTests.Average(e => e.Score) : 0.0;
            Console.WriteLine($"The average score of the {Path.GetFileNameWithoutExtension(filePath)} test cases = {averageScore}\n");
        }
    }

    /// <summary>
    /// Passes each test case to the judge, building a list of EvaluatedTestCase objects
    /// from the judge's responses. If the judge throws an exception on a test case, the 
    /// corrsesponding EvaluatedTestCase will have a score of 0 and will hold the error message.
    /// </summary>
    /// <param name="testCases">The test cases to grade.</param>
    /// <returns>A list of evaluated test cases corresponding to the list of given test cases.</returns>
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

    /// <summary>
    /// Prints one test case and its evaluation to the console as indented JSON, under a header.
    /// </summary>
    /// <param name="test">The test case that was graded.</param>
    /// <param name="evaluatedTest">The grading result for that test case.</param>
    private static void PrintEvaluatedTests(TestCase test, EvaluatedTestCase evaluatedTest)
    {
        JsonSerializerOptions options = new()
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
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