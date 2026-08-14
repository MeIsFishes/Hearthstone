using System.Text;
using System.Text.RegularExpressions;

namespace BbxDeployer.Services;

internal sealed class GitIgnorePattern
{
    private readonly Regex _regex;

    private GitIgnorePattern(
        string original,
        Regex regex,
        bool isNegation,
        bool directoryOnly)
    {
        Original = original;
        _regex = regex;
        IsNegation = isNegation;
        DirectoryOnly = directoryOnly;
    }

    public string Original { get; }

    public bool IsNegation { get; }

    public bool DirectoryOnly { get; }

    public bool IsMatch(string portableRelativePath)
    {
        return _regex.IsMatch(portableRelativePath.TrimStart('/'));
    }

    public static bool TryParse(
        string line,
        out GitIgnorePattern? pattern,
        out string? error)
    {
        pattern = null;
        error = null;

        if (string.IsNullOrWhiteSpace(line))
        {
            return false;
        }

        var value = TrimUnescapedTrailingSpaces(line);
        if (value.Length == 0 || value[0] == '#')
        {
            return false;
        }

        var escapedPrefix = value.StartsWith(@"\#", StringComparison.Ordinal)
            || value.StartsWith(@"\!", StringComparison.Ordinal);

        var isNegation = !escapedPrefix && value.StartsWith('!');
        if (isNegation)
        {
            value = value[1..];
        }
        else if (escapedPrefix)
        {
            value = value[1..];
        }

        if (value.Length == 0)
        {
            error = $"Invalid empty pattern: {line}";
            return false;
        }

        var anchored = value.StartsWith('/');
        if (anchored)
        {
            value = value[1..];
        }

        var directoryOnly = EndsWithUnescapedSlash(value);
        if (directoryOnly)
        {
            value = value[..^1];
        }

        if (value.Length == 0)
        {
            error = $"Invalid root-only pattern: {line}";
            return false;
        }

        try
        {
            var containsSlash = value.Contains('/');
            var body = ConvertGlobToRegex(value);
            var prefix = anchored || containsSlash ? "^" : @"(?:^|.*/)";
            var suffix = directoryOnly ? @"(?:/.*)?$" : @"(?:$|/.*$)";
            var regex = new Regex(
                prefix + body + suffix,
                RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

            pattern = new GitIgnorePattern(line, regex, isNegation, directoryOnly);
            return true;
        }
        catch (ArgumentException exception)
        {
            error = $"Invalid pattern '{line}': {exception.Message}";
            return false;
        }
    }

    private static string ConvertGlobToRegex(string pattern)
    {
        var builder = new StringBuilder();

        for (var index = 0; index < pattern.Length; index++)
        {
            var character = pattern[index];

            if (character == '\\' && index + 1 < pattern.Length)
            {
                builder.Append(Regex.Escape(pattern[++index].ToString()));
                continue;
            }

            if (character == '*')
            {
                var isDoubleStar = index + 1 < pattern.Length && pattern[index + 1] == '*';
                if (isDoubleStar)
                {
                    index++;
                    if (index + 1 < pattern.Length && pattern[index + 1] == '/')
                    {
                        index++;
                        builder.Append("(?:.*/)?");
                    }
                    else
                    {
                        builder.Append(".*");
                    }
                }
                else
                {
                    builder.Append("[^/]*");
                }

                continue;
            }

            if (character == '?')
            {
                builder.Append("[^/]");
                continue;
            }

            if (character == '[')
            {
                var closingIndex = FindClosingBracket(pattern, index + 1);
                if (closingIndex < 0)
                {
                    throw new ArgumentException("Character range has no closing bracket.");
                }

                var content = pattern[(index + 1)..closingIndex];
                if (content.StartsWith('!'))
                {
                    content = "^" + content[1..];
                }

                builder.Append('[').Append(content).Append(']');
                index = closingIndex;
                continue;
            }

            builder.Append(Regex.Escape(character.ToString()));
        }

        return builder.ToString();
    }

    private static int FindClosingBracket(string value, int start)
    {
        for (var index = start; index < value.Length; index++)
        {
            if (value[index] == ']' && index > start)
            {
                return index;
            }
        }

        return -1;
    }

    private static string TrimUnescapedTrailingSpaces(string value)
    {
        var end = value.Length;
        while (end > 0 && value[end - 1] == ' ')
        {
            var slashCount = 0;
            for (var index = end - 2; index >= 0 && value[index] == '\\'; index--)
            {
                slashCount++;
            }

            if (slashCount % 2 == 1)
            {
                break;
            }

            end--;
        }

        return value[..end];
    }

    private static bool EndsWithUnescapedSlash(string value)
    {
        if (!value.EndsWith('/'))
        {
            return false;
        }

        var slashCount = 0;
        for (var index = value.Length - 2; index >= 0 && value[index] == '\\'; index--)
        {
            slashCount++;
        }

        return slashCount % 2 == 0;
    }
}
