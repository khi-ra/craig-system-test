using System.Runtime.CompilerServices;
using System.Text.Json;
using CraigSystemTest.Models;

using Google.GenAI;
using Google.GenAI.Types;

using Superpower.Model;

using Type = Google.GenAI.Types.Type;

public class Judge
{
    private const string GEMINI_MODEL = "gemini-2.5-flash";
    private readonly Client _gemini;

    public Judge(string apiKey, string systemPrompt) {
        _gemini = new Client(apiKey: apiKey);

        Schema judgeResultSchema = new Schema
        {
            Type = Type.Object,
            Properties = new Dictionary<string, Schema>
            {
                { "TestCaseId",   new Schema { Type = Type.String } },
                { "Justification", new Schema { Type = Type.String } },
                { "Score",        new Schema { Type = Type.Integer } }
            },
            PropertyOrdering = new List<string> { "TestCaseId", "Justification", "Score" },
            Required = new List<string> { "TestCaseId", "Justification", "Score" }
        };
        
        var config = new GenerateContentConfig
        {
            Temperature = 0,
            ResponseSchema = judgeResultSchema,
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

    public async Task<EvaluatedTestCase> evaluateAsync(TestCase testCase)
    {
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        var judgeResponse = await _gemini.Models.GenerateContentAsync(
                model: GEMINI_MODEL, contents: JsonSerializer.Serialize<TestCase>(testCase, options)
        );

        string? judgeResponseText = judgeResponse.Candidates[0].Content.Parts[0].Text;
        if (string.IsNullOrWhiteSpace(judgeResponseText))
        {
            return new EvaluatedTestCase
            {
                TestCaseId = testCase.Id,
                Score = 0,
                Justification = "Judge returned no text."
            };
        }

        EvaluatedTestCase? evaluatedTestCase = JsonSerializer.Deserialize<EvaluatedTestCase>(judgeResponseText, options);
        if (evaluatedTestCase is null)
        {
            return new EvaluatedTestCase
            {
                TestCaseId = testCase.Id,
                Score = 0,
                Justification = "Judge response could not be parsed."
            };
        }
        
        return evaluatedTestCase;
    }
}