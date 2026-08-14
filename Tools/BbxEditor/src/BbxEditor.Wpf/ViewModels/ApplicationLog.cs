using System.Collections.ObjectModel;
using BbxEditor.Diagnostics;
using BbxEditor.Domain;

namespace BbxEditor.Wpf.ViewModels;

public sealed record ApplicationLogEntry(
    DateTimeOffset Timestamp,
    DiagnosticSeverity Severity,
    string Message,
    string? Source)
{
    public string TimeText => Timestamp.ToLocalTime().ToString("HH:mm:ss");
    public string LevelText => Severity == DiagnosticSeverity.Info ? "Log" : Severity.ToString();
}

public sealed class ApplicationLog : ObservableObject
{
    private int _logCount;
    private int _warningCount;
    private int _errorCount;

    public ObservableCollection<ApplicationLogEntry> Entries { get; } = [];
    public int LogCount => _logCount;
    public int WarningCount => _warningCount;
    public int ErrorCount => _errorCount;
    public bool HasErrors => ErrorCount > 0;
    public string SummaryText => $"{LogCount} logs, {WarningCount} warnings, {ErrorCount} errors";

    public void Add(string message, DiagnosticSeverity severity = DiagnosticSeverity.Info, string? source = null)
    {
        if (string.IsNullOrWhiteSpace(message)) return;
        Entries.Add(new ApplicationLogEntry(DateTimeOffset.Now, severity, message, source));
        switch (severity)
        {
            case DiagnosticSeverity.Info:
                _logCount++;
                RaisePropertyChanged(nameof(LogCount));
                break;
            case DiagnosticSeverity.Warning:
                _warningCount++;
                RaisePropertyChanged(nameof(WarningCount));
                break;
            case DiagnosticSeverity.Error:
                _errorCount++;
                RaisePropertyChanged(nameof(ErrorCount));
                RaisePropertyChanged(nameof(HasErrors));
                break;
        }
        RaisePropertyChanged(nameof(SummaryText));
    }

    public void Add(Diagnostic diagnostic) =>
        Add(diagnostic.Message, diagnostic.Severity, diagnostic.Source);

    public void AddRange(IEnumerable<Diagnostic> diagnostics)
    {
        foreach (var diagnostic in diagnostics) Add(diagnostic);
    }
}
