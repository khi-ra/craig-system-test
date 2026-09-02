namespace CraigSystemTest.Services;
using CraigSystemTest.Models;
using System.Text.Json;
public class TestCaseLoader
{
    private readonly JsonSerializerOptions _options = new() { PropertyNameCaseInsensitive = true };

    /// <summary>
    /// Reads a JSON file from disk and deserialises it into a list of test cases.
    /// </summary>
    /// <param name="filePath">Full path to the JSON file to load.</param>
    /// <returns>A list of TestCase objects, each corresponding to a test case in the given file.</returns>
    /// <exception cref="InvalidDataException">Thrown if the file contains no test cases.</exception>
    public List<TestCase> Load(string filePath)
    {
        string testFileContent = File.ReadAllText(filePath);

        TestCaseFile? file = JsonSerializer.Deserialize<TestCaseFile>(testFileContent, _options);

        List<TestCase> testCases = file?.TestCases ?? new List<TestCase>();

        return testCases.Count == 0 ? throw new InvalidDataException($"No test cases in {filePath}") : testCases;
    }
}