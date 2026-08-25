using CraigSystemTest.Services;
// Explicit definition to due to 'System' and 'Gemini' namespace conflicts 
using File = System.IO.File;

string promptFile = Path.Combine(AppContext.BaseDirectory, "Data", "JudgeSystemPrompt.txt");
string systemPrompt = File.ReadAllText(promptFile);

Judge judge = new(systemPrompt);
TestCaseLoader loader = new();
TestRunner runner = new(judge, loader);

await runner.RunAsync(["IncidentManagementTests.json", "PingTests.json"]);





