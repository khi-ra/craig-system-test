# Craig System Test

An automated test harness for **CRAiG**, the AI assistant on the Crises Control mass notification and emergency management platform.

It runs a set of predefined test cases through an LLM judge (Google Gemini) and scores CRAiG's responses against a reference answer. It uses the "LLM-as-a-judge" approach because CRAiG's answers are not fixed strings.

---

## What it does

1. Loads test cases from JSON files in the `Data` folder.
2. Sends each test case to a Gemini judge, along with a system prompt that tells the judge how to grade.
3. The judge returns a JSON result for each case: an ID, a justification, and a score from 0 to 5.
4. Prints each result to the console and prints the average score for each test file.

The judge does **not** answer the user's question or talk to CRAiG. It only grades one already-recorded response at a time.

---

## How it works

+ `TestRunner` takes a list of JSON file names and hands the filepath of each file creates to `TestCaseLoader`.
+ `TestCaseLoader` reads the JSON file and deserialises it into a list of `TestCase` objects.
+ `TestRunner` then sends each test case from that list to the `Judge` to evaluate.
+ `Judge` evaluates the test case using Gemini and returns an `EvaluatedTestCase`.
+ `TestRunner` prints each `TestCase`, it's corresponding `EvaluatedTestCase`, and the average score for each file.
+ `Program.cs` is the entry point. It wires everything up and chooses which test files to run.

The judge is configured with:
- Model: `gemini-3.6-flash`
- Temperature: `0` (to make grading as repeatable as possible)
- A response schema that forces the output into JSON with the fields `TestCaseId`, `Justification`, and `Score`.

---

## Project structure

```
CraigSystemTest/
├── Program.cs                  Entry point. Chooses which test files to run.
├── CraigSystemTest.csproj      Project file and package references.
├── .env                        Holds the Gemini API key (not committed).
│
├── Models/
│   ├── TestCase.cs             Represents one test case (input to the judge).
│   ├── EvaluatedTestCase.cs    Represents the graded result of a test case (output from the judge).
│   └── TestCaseFile.cs         Wrapper holding a list of TestCase objects.
│
├── Services/
│   ├── Judge.cs                Talks to Gemini and returns an evaluated test case.
│   ├── TestCaseLoader.cs       Reads and parses a test-case JSON file.
│   └── TestRunner.cs           Runs all cases and prints results.
│
└── Data/
    ├── JudgeSystemPrompt.txt       The grading instructions given to the judge.
    ├── IncidentManagementTests.json
    ├── PingTests.json
    ├── TaskManagementTests.json
    ├── LocationTests.json
    └── ... (one JSON file per capability being tested)
```

Everything in `Data` is copied to the build output folder automatically, so the running program can find it at `Data/...` relative to the executable.

---

## Requirements

- **.NET 10 SDK** (the project targets `net10.0`).
- A **Google Gemini API key**.
- NuGet packages (restored automatically on build):
  - `DotNetEnv` 3.2.0 
  - `Google.GenAI` 1.18.0

---

## Setup

1. Create a file named `.env` in the `CraigSystemTest` project folder with your Gemini API key:

   ```
   GEMINI_API_KEY=your_key_here
   ```

2. Restore packages:

   ```bash
   dotnet restore
   ```

---

## Running

From the `CraigSystemTest` folder:

```bash
dotnet run
```

By default this runs the test files listed in `Program.cs`. Right now that is:

```csharp
await runner.RunAsync(["IncidentManagementTests.json", "PingTests.json"]);
```

To run different files, edit that list. The other JSON files in `Data` exist but are not run unless you add them here.

---

## Test case format

Each JSON file contains a `TestCases` array. Each test case has these fields:

| Field         | Meaning                                                                 |
|---------------|-------------------------------------------------------------------------|
| `Id`          | A unique ID for the test case (for example `P01`).                      |
| `InputPrompt` | The user's question to CRAiG.                                            |
| `Response`    | CRAiG's answer, exactly as the user received it. This is what gets graded. |
| `Reference`   | The correct answer, or instructions the response must satisfy.          |
| `Criteria`    | The list of criteria to grade against for this case.                    |

Example:

```json
{
  "TestCases": [
    {
      "Id": "P01",
      "InputPrompt": "Can I attach a photo to a ping, and how do people open it?",
      "Response": "Yes, you can attach a photo to a ping message...",
      "Reference": "Yes, you can attach a photo to a ping message. When creating a ping, a user can attach photos by clicking the 'upload' button...",
      "Criteria": ["Knowledge Base", "Hallucination"]
    }
  ]
}
```

The `Response` and `Reference` do not have to match word for word. The judge checks whether they agree in substance.

---

## Evaluation criteria

The judge only applies the criteria listed in a test case's `Criteria` array. There are three recognised criteria:

- **Knowledge Base Accuracy**: Does the response say the same thing as the reference?
- **Hallucination**: Does the response add anything the reference does not support?
- **Error Handling**: Used when the prompt is invalid, out of scope, ambiguous, missing needed information, or asks for something that cannot be done.

The full grading rules, edge cases, and scoring scale live in `Data/JudgeSystemPrompt.txt`. If you change how grading should work, that file is where you change it.

---

## Scoring

Each criterion is scored from 1 to 5. The overall score for a test case is the **lowest** of its per-criterion scores. A score of 0 means the case could not be evaluated (for example, a missing reference or a malformed test case).

Score scale:

| Score | Meaning                                                                |
|-------|------------------------------------------------------------------------|
| 5     | Fully correct and complete. Nothing unsupported.                       |
| 4     | Correct and usable. Minor omission or wording that would not mislead.  |
| 3     | Partially correct. A material gap, ambiguity, or tone problem.         |
| 2     | Mostly wrong or misleading, or adds an unsupported factual claim.      |
| 1     | Wrong, empty, unusable, or falsely claims an action was completed.     |
| 0     | Not evaluable (empty/null reference or a malformed test case).         |

A result counts as a **pass** when its score is **4 or higher** (`EvaluatedTestCase.Pass`).

The judge's result for each case looks like this:

```json
{
  "TestCaseId": "P01",
  "Justification": "The response invented a camera icon and a secure-link flow not in the reference.",
  "Score": 2
}
```

---

## Adding new test cases

1. Add a new object to the `TestCases` array in an existing `Data/*.json` file, or create a new JSON file in `Data` using the same structure.
2. If you created a new file, add its filename to the `RunAsync([...])` call in `Program.cs`.
3. Run `dotnet run`.

New files placed in `Data` are copied to the output folder on the next build.

---

## Notes and current limitations

- **The test cases are hardcoded.** Since this is a PoC, there's a few files with only 1-2 test cases each. This is not comprehensive enough yet.
- **Function calls and latency are not tested yet.** At the moment the program only evaluates written answers, not all of CRAiG's.
- **Single run, no averaging across runs yet.** Each run grades each case once and averages per file. If you want to average scores over several runs to smooth out judge variation, that is not built in yet.
- **Scores depend on the reference.** The judge grades strictly against the `Reference` field. A weak or wrong reference will produce weak or wrong scores, so the quality of the references matters as much as the code.

---

## Security

The `.env` file contains a live API key. Do not commit it. Add it to `.gitignore`, and if a key has already been committed or shared, rotate it (generate a new one and revoke the old one).