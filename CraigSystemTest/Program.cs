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
string geminiModel = "gemini-2.5-flash";

// Create Gemini client
try
{
    string apiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY");
    geminiClient = new Client(apiKey: apiKey);
    var config = new GenerateContentConfig
    {
        Temperature = 0,
        ResponseMimeType = "application/json",
        SystemInstruction = new Content
        {
            Parts = new List<Part>
            {
                new Part { Text = "You are a helpful coding assistant. Always answer in C# and keep it short." }
            }
        }
    };
}
catch (Exception ex)
{
    Console.WriteLine($"Error loading Gemini API key: {ex.Message}");
    throw;
}

try
{
    string path = Path.Combine(AppContext.BaseDirectory, "Data", "tests.json");
    var testCaseJson = File.ReadAllText(path);

    TestCase testCase = JsonSerializer.Deserialize<TestCase>(testCaseJson);
    Console.WriteLine($"DESERIALIZED OBJECT:\n{testCase.Id}\n{testCase.InputPrompt}\n{testCase.Response}\n");
}
catch (Exception ex)
{
    Console.WriteLine($"Error reading/deserialising JSON file: {ex.Message}");
    throw;
}





