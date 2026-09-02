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
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        PropertyNameCaseInsensitive = true,
    };
    

    /// <summary>
    /// Builds a Gemini client (the judge) to evaluate test cases, and prepares the 
    /// response schema and generation config. 
    /// </summary>
    /// <param name="systemPrompt">The grading instructions given to the judge as its system prompt.</param>
    public Judge(string systemPrompt) {
        DotNetEnv.Env.Load();
        _gemini = BuildGeminiJudge();

        _judgeResultSchema = BuildJudgeResultSchema();

        _config = BuildConfig(systemPrompt, _judgeResultSchema);
    }

    /// <summary>
    /// Reads the GEMINI_API_KEY environment variable and returns a Gemini client built from it.
    /// </summary>
    /// <returns>A configured Gemini client.</returns>
    /// <exception cref="InvalidOperationException">Thrown if GEMINI_API_KEY is not set.</exception>
    private static Client BuildGeminiJudge()
    {
        string apiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY") ?? throw new InvalidOperationException();  
        return new Client(apiKey: apiKey);
    }
    
    /// <summary>
    /// Defines the JSON output schema for the judge's responses: an object with TestCaseId,
    /// Justification, and Score, all required. This forces the model to return structured JSON.
    /// </summary>
    /// <returns>The response schema for the judge's output.</returns>
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

    /// <summary>
    /// Builds the generation config for each judge call: temperature set to 0 to reduce variation,
    /// JSON output constrained to the given schema, and the system prompt as the instructions.
    /// </summary>
    /// <param name="systemPrompt">The grading instructions to set as the system prompt.</param>
    /// <param name="judgeResultSchema">The schema the judge's JSON reply must match.</param>
    /// <returns>The generation config used for every evaluation.</returns>
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

    /// <summary>
    /// Sends the given test case to the judge and parses the judge's JSON reply 
    /// into an EvaluatedTestCase. If the judge returns no text or the reply cannot 
    /// be parsed, returns a fallback result with a score of 0 and a reason.
    /// </summary>
    /// <param name="testCase">The test case to grade.</param>
    /// <returns>An EvaluatedTestCase object representing the graded result, or a score-0 fallback if the judge's reply was unusable.</returns>
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