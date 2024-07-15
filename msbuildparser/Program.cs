using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.Linq;
using System.IO;

class MSBuildOutputHandler
{
    public class BuildIssue
    {
        public string SourceFile { get; set; }
        public int Line { get; set; }
        public string Code { get; set; }
        public string Message { get; set; }
        public string Type { get; set; }
    }

    static readonly Regex IssueRegex = new Regex(@"(.*?)\((\d+)\):\s*(warning|error)\s*([\w\d]+):\s*(.*)");
    static readonly Regex BuildStatusRegex = new Regex(@"Build (FAILED|succeeded)\.");

    static void ProcessMSBuildOutput(string line, List<BuildIssue> issues)
    {
        Match issueMatch = IssueRegex.Match(line);
        if (issueMatch.Success)
        {
            var issue = new BuildIssue
            {
                SourceFile = Path.GetFileName(issueMatch.Groups[1].Value),
                Line = int.Parse(issueMatch.Groups[2].Value),
                Type = issueMatch.Groups[3].Value,
                Code = issueMatch.Groups[4].Value,
                Message = issueMatch.Groups[5].Value
            };
            issues.Add(issue);
            Console.ForegroundColor = issue.Type == "error" ? ConsoleColor.Red : ConsoleColor.Yellow;
            Console.WriteLine($"{issue.Type.ToUpper()} {issue.Code}: {issue.SourceFile}({issue.Line}): {issue.Message}");
            Console.ResetColor();
        }
        else
        {
            Match buildStatusMatch = BuildStatusRegex.Match(line);
            if (buildStatusMatch.Success)
            {
                Console.ForegroundColor = buildStatusMatch.Groups[1].Value == "FAILED" ? ConsoleColor.Red : ConsoleColor.Green;
                Console.WriteLine(line);
                Console.ResetColor();
            }
        }
    }

    static void ExecuteMSBuild()
    {
        string command = "powershell.exe";
        string arguments = "-ExecutionPolicy Bypass -File \"C:\\github\\Projects\\Xbox 360 UI Manager\\build.ps1\"";

        ProcessStartInfo psi = new ProcessStartInfo
        {
            FileName = command,
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        List<BuildIssue> issues = new List<BuildIssue>();

        using (Process process = new Process { StartInfo = psi })
        {
            process.OutputDataReceived += (sender, e) =>
            {
                if (e.Data != null)
                    ProcessMSBuildOutput(e.Data, issues);
            };
            process.ErrorDataReceived += (sender, e) =>
            {
                if (e.Data != null)
                    ProcessMSBuildOutput(e.Data, issues);
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            process.WaitForExit();
        }

        int errorCount = issues.Count(i => i.Type == "error");
        int warningCount = issues.Count(i => i.Type == "warning");

        Console.WriteLine("\nBuild Summary:");
        Console.WriteLine($"Errors: {errorCount}");
        Console.WriteLine($"Warnings: {warningCount}");
    }

    static void Main(string[] args)
    {
        Console.WriteLine("Starting MSBuild...");
        ExecuteMSBuild();
        Console.Read();
    }
}