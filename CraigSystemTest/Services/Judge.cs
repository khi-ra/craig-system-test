namespace CraigSystemTest.Services;
using System.Text.Json;
using CraigSystemTest.Models;
using Google.GenAI;
using Google.GenAI.Types;
using System.Text.Encodings.Web;
using Type = Google.GenAI.Types.Type;
using Environment = System.Environment;
public class Judge
{
    private const string GEMINI_MODEL = "gemini-3.6-flash";
    private readonly Client _gemini;
    private readonly Schema _judgeResultSchema;
    private readonly GenerateContentConfig _config;
    private readonly JsonSerializerOptions _options = new() 
    { 
        PropertyNameCaseInsensitive = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };
    

    public Judge(string systemPrompt) {
        DotNetEnv.Env.Load();
        _gemini = BuildGeminiJudge();

        _judgeResultSchema = BuildJudgeResultSchema();

        _config = BuildConfig(systemPrompt, _judgeResultSchema);
    }

    private static Client BuildGeminiJudge()
    {
        string apiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY") ?? throw new InvalidOperationException();  
        return new Client(apiKey: apiKey);
    }
    
    private static Schema BuildJudgeResultSchema()
    {
        return new Schema
        {
            Type = Type.Object,
            Properties = new Dictionary<string, Schema> 
            {
                { "TestCaseId",   new Schema { Type = Type.String } },
                { "Justification", new Schema { Type = Type.String } },
                { "Score",        new Schema { Type = Type.Integer } }
            },
            Required = ["TestCaseId", "Justification", "Score"]
        };
    }

    private static GenerateContentConfig BuildConfig(string systemPrompt, Schema judgeResultSchema)
    {
        return new GenerateContentConfig
        {
            Temperature = 0,
            ResponseSchema = judgeResultSchema,
            ResponseMimeType = "application/json",
            SystemInstruction = new Content
            {
                Parts = [ new() { Text = systemPrompt } ]
            }
        };
    }

    public async Task<EvaluatedTestCase> EvaluateAsync(TestCase testCase)
    {
        string? testCaseJson = JsonSerializer.Serialize<TestCase>(testCase, _options);

        var judgeResponse = await _gemini.Models.GenerateContentAsync(
                model: GEMINI_MODEL, contents: testCaseJson, config: _config
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

        EvaluatedTestCase? evaluatedTestCase = JsonSerializer.Deserialize<EvaluatedTestCase>(judgeResponseText, _options);
        return evaluatedTestCase ?? new EvaluatedTestCase
            {
                TestCaseId = testCase.Id,
                Score = 0,
                Justification = "Judge response could not be parsed."
            };
    }
}