using System;
using System.Diagnostics;
using System.Text.Json;
using System.Xml.Serialization;
using CraigSystemTest.Models;
using System.Threading.Tasks;
using Google.GenAI;
using Google.GenAI.Types;
// Explicit definitions to due to 'System' and 'Gemini' namespace conflicts 
using File = System.IO.File;
using Environment = System.Environment;

DotNetEnv.Env.Load();
Judge judge;

// create Gemini client
try
{
    string apiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY") ?? throw new InvalidOperationException();   
    string systemPromptFile = Path.Combine(AppContext.BaseDirectory, "Data", "JudgeSystemPrompt.txt");
    string systemPrompt = File.ReadAllText(systemPromptFile);

    judge = new Judge(apiKey, systemPrompt);
}
catch (Exception ex)
{
    Console.WriteLine($"Error creating Gemini client: {ex.Message}");
    throw;
}


// deserialize json test files

var options = new JsonSerializerOptions
{
    PropertyNameCaseInsensitive = true
};
List<TestCase> testCases;
try
{
    string testFile = Path.Combine(AppContext.BaseDirectory, "Data", "IncidentManagementTests.json");
    string testFileContent = File.ReadAllText(testFile);

    TestCaseFile? file = JsonSerializer.Deserialize<TestCaseFile>(testFileContent, options);
    testCases = file?.TestCases ?? throw new InvalidOperationException($"No test cases found in {testFile}");
}
catch (Exception ex)
{
    Console.WriteLine($"Error reading/deserialising JSON file: {ex.Message}");
    throw;
}

// have judge evaluate test cases and print output
var evaluatedTestCases = new List<EvaluatedTestCase>();
try
{
    int i = 1;
    foreach (var testCase in testCases)
    {
        Console.WriteLine($"Running test {i}...\n");
        try
        {
            var result = await judge.evaluateAsync(testCase);
            evaluatedTestCases.Add(result);
        }
        catch (Exception ex)
        {
            evaluatedTestCases.Add(new EvaluatedTestCase
            {
                TestCaseId = testCase.Id,
                Score = 0,
                Justification = $"Evaluation error: {ex.Message}"
            });
        }

        i++;
    }
}
catch (Exception ex)
{
    Console.WriteLine($"Error evaluating test cases: {ex.Message}");
    throw;
}

for (int i = 0; i < evaluatedTestCases.Count(); i++)
{
    TestCase test = testCases[i];
    EvaluatedTestCase evalTest = evaluatedTestCases[i];

    Console.WriteLine("=============================");
    Console.WriteLine($"EVALUATION OF TEST CASE {i + 1}");
    Console.WriteLine("=============================");
    Console.WriteLine(
        $"Id = {test.Id}\n\nInput = {test.InputPrompt}\n\nResponse = {test.Response}\n\nReference = {test.Reference}\n\n"
    );
    Console.WriteLine(
        $"Score = {evalTest.Score}\n\nPass = {evalTest.Pass}\n\nJustification = {evalTest.Justification}\n\n"
    );
}




