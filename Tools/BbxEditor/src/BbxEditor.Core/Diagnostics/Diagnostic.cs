namespace BbxEditor.Diagnostics;

public enum DiagnosticSeverity
{
    Info,
    Warning,
    Error,
}

public sealed record Diagnostic(DiagnosticSeverity Severity, string Code, string Message, string? Source = null);

public sealed class OperationResult<T>
{
    public T? Value { get; set; }
    public List<Diagnostic> Diagnostics { get; } = [];
    public bool Success => Value is not null && Diagnostics.All(item => item.Severity != DiagnosticSeverity.Error);

    public static OperationResult<T> FromValue(T value) => new() { Value = value };
}
