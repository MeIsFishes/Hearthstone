using System.Diagnostics;
using BbxDeployer.Core;

namespace BbxDeployer.Services;

public sealed class UnityProjectCreator(ProjectLocator projectLocator) : IUnityProjectCreator
{
    public async Task CreateAsync(
        UnityEditorInstallation editor,
        string unityProjectRoot,
        CancellationToken cancellationToken)
    {
        if (!File.Exists(editor.ExecutablePath))
        {
            throw new FileNotFoundException(
                $"Unity {editor.Version} is no longer installed.",
                editor.ExecutablePath);
        }

        if (Directory.Exists(unityProjectRoot)
            && Directory.EnumerateFileSystemEntries(unityProjectRoot).Any())
        {
            throw new InvalidOperationException(
                "Unity project creation requires a missing or empty Game Project folder.");
        }

        var parent = Directory.GetParent(unityProjectRoot)?.FullName
            ?? throw new InvalidOperationException("The Game Project folder has no parent.");
        Directory.CreateDirectory(parent);

        var logDirectory = Path.Combine(
            Path.GetTempPath(),
            "BbxDeployer",
            "UnityLogs");
        Directory.CreateDirectory(logDirectory);
        var logPath = Path.Combine(
            logDirectory,
            $"create-project-{DateTime.UtcNow:yyyyMMdd-HHmmss}-{Guid.NewGuid():N}.log");

        var startInfo = new ProcessStartInfo
        {
            FileName = editor.ExecutablePath,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        startInfo.ArgumentList.Add("-batchmode");
        startInfo.ArgumentList.Add("-quit");
        startInfo.ArgumentList.Add("-createProject");
        startInfo.ArgumentList.Add(unityProjectRoot);
        startInfo.ArgumentList.Add("-logFile");
        startInfo.ArgumentList.Add(logPath);

        using var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException($"Could not start Unity {editor.Version}.");
        try
        {
            await process.WaitForExitAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
                await process.WaitForExitAsync(CancellationToken.None);
            }

            throw;
        }

        if (process.ExitCode != 0 || !projectLocator.IsUnityProject(unityProjectRoot))
        {
            var detail = ReadLogTail(logPath);
            throw new InvalidOperationException(
                $"Unity {editor.Version} could not create the project (exit code "
                + $"{process.ExitCode}). Log: {logPath}"
                + (string.IsNullOrWhiteSpace(detail) ? string.Empty : $"{Environment.NewLine}{detail}"));
        }
    }

    private static string ReadLogTail(string logPath)
    {
        try
        {
            if (!File.Exists(logPath))
            {
                return string.Empty;
            }

            return string.Join(
                Environment.NewLine,
                File.ReadLines(logPath).TakeLast(12));
        }
        catch (IOException)
        {
            return string.Empty;
        }
    }
}
