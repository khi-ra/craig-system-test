namespace CraigSystemTest.Services;
using CraigSystemTest.Models;
using System.Text.Json;
public class TestCaseLoader
{
    private readonly JsonSerializerOptions _options = new() { PropertyNameCaseInsensitive = true };

    public List<TestCase> Load(string filePath)
    {
        string testFileContent = File.ReadAllText(filePath);

        TestCaseFile? file = JsonSerializer.Deserialize<TestCaseFile>(testFileContent, this._options);

        List<TestCase> testCases = file?.TestCases ?? new List<TestCase>();

        if (testCases.Count == 0)
        {
            throw new InvalidDataException($"No test cases in {filePath}");
        }
        
        return testCases;
    }
}