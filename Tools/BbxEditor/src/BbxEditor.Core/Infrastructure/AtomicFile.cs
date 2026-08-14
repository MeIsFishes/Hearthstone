using System.Text;

namespace BbxEditor.Infrastructure;

internal static class AtomicFile
{
    public static void WriteAllText(string path, string content, bool utf8Bom = false)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path) ?? ".");
        var temp = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            File.WriteAllText(temp, content, new UTF8Encoding(utf8Bom));
            File.Move(temp, path, true);
        }
        finally
        {
            if (File.Exists(temp)) File.Delete(temp);
        }
    }
}
