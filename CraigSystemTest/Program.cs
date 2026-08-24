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
Client geminiClient;
const string GEMINI_MODEL = "gemini-2.5-flash";

// create Gemini client
try
{
    string? apiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY");   
    geminiClient = new Client(apiKey: apiKey);

    string systemPromptFile = Path.Combine(AppContext.BaseDirectory, "Data", "JudgeSystemPrompt.txt");
    string systemPrompt = File.ReadAllText(systemPromptFile);

    var config = new GenerateContentConfig
    {
        Temperature = 0,
        ResponseMimeType = "application/json",
        SystemInstruction = new Content
        {
            Parts = new List<Part>
            {
                new Part { Text = systemPrompt }
            }
        }
    };
}
catch (Exception ex)
{
    Console.WriteLine($"Error creating Gemini client: {ex.Message}");
    throw;
}


// deserialize json test files
List<TestCase>? testCases;
try
{
    string testFile = Path.Combine(AppContext.BaseDirectory, "Data", "IncidentManagementTests.json");
    string testFileContent = File.ReadAllText(testFile);

    var options = new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true
    };
    TestCaseFile? file = JsonSerializer.Deserialize<TestCaseFile>(testFileContent, options);
    testCases = file?.TestCases ?? new List<TestCase>();
}
catch (Exception ex)
{
    Console.WriteLine($"Error reading/deserialising JSON file: {ex.Message}");
    throw;
}

// use gemini to evaluate test cases
try
{
    
}




