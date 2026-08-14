using System.Text;

namespace BbxDeployer.Services;

public sealed class RunLogger
{
    private readonly StringBuilder _content = new();

    public void Write(string message)
    {
        _content.Append(DateTimeOffset.Now.ToString("O"))
            .Append(' ')
            .AppendLine(message);
    }

    public async Task<string> SaveAsync(CancellationToken cancellationToken = default)
    {
        var directory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "BbxDeployer",
            "logs");
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, $"{DateTime.Now:yyyyMMdd-HHmmss-fff}.log");
        await File.WriteAllTextAsync(path, _content.ToString(), cancellationToken);
        return path;
    }
}
