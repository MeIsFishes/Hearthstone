using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace BbxCommon
{
    public enum ETaskBlackboardInjectionValueType
    {
        Bool,
        Int,
        Long,
        Float,
        Double,
        String,
    }

    public readonly struct TaskBlackboardInjectionEntry
    {
        internal TaskBlackboardInjectionEntry(
            string key,
            ETaskBlackboardInjectionValueType valueType,
            long longValue,
            double doubleValue,
            string stringValue)
        {
            Key = key;
            ValueType = valueType;
            LongValue = longValue;
            DoubleValue = doubleValue;
            StringValue = stringValue;
        }

        public string Key { get; }
        public ETaskBlackboardInjectionValueType ValueType { get; }
        public long LongValue { get; }
        public double DoubleValue { get; }
        public string StringValue { get; }
    }

    /// <summary>
    /// A typed collection written in one CSV cell as Key,Type,Value;Key,Type,Value.
    /// Backslash escapes backslashes, commas, and semicolons inside keys or string values.
    /// </summary>
    public sealed class TaskBlackboardInjection
    {
        private static readonly TaskBlackboardInjection s_Empty =
            new TaskBlackboardInjection(Array.Empty<TaskBlackboardInjectionEntry>());

        private readonly IReadOnlyList<TaskBlackboardInjectionEntry> m_Entries;

        private TaskBlackboardInjection(IReadOnlyList<TaskBlackboardInjectionEntry> entries)
        {
            m_Entries = entries;
        }

        public static TaskBlackboardInjection Empty => s_Empty;
        public IReadOnlyList<TaskBlackboardInjectionEntry> Entries => m_Entries;
        public int Count => m_Entries.Count;
        public TaskBlackboardInjectionEntry this[int index] => m_Entries[index];

        public static bool TryParse(string text, out TaskBlackboardInjection result, out string error)
        {
            if (string.IsNullOrEmpty(text))
            {
                result = Empty;
                error = string.Empty;
                return true;
            }

            if (!TrySplitEscaped(text, ';', int.MaxValue, out var rawEntries, out error))
            {
                result = Empty;
                return false;
            }

            var entries = new List<TaskBlackboardInjectionEntry>(rawEntries.Count);
            var keys = new HashSet<string>(StringComparer.Ordinal);
            for (var entryIndex = 0; entryIndex < rawEntries.Count; entryIndex++)
            {
                var rawEntry = rawEntries[entryIndex];
                if (rawEntry.Length == 0)
                {
                    result = Empty;
                    error = $"Entry {entryIndex + 1} is empty.";
                    return false;
                }

                if (!TrySplitEscaped(rawEntry, ',', 3, out var fields, out error) || fields.Count != 3)
                {
                    result = Empty;
                    error = $"Entry {entryIndex + 1} must contain Key,Type,Value. {error}".TrimEnd();
                    return false;
                }

                if (!TryUnescape(fields[0], out var key, out error) ||
                    !TryUnescape(fields[1], out var typeName, out error) ||
                    !TryUnescape(fields[2], out var value, out error))
                {
                    result = Empty;
                    error = $"Entry {entryIndex + 1}: {error}";
                    return false;
                }

                key = key.Trim();
                typeName = typeName.Trim();
                if (key.Length == 0)
                {
                    result = Empty;
                    error = $"Entry {entryIndex + 1} has an empty key.";
                    return false;
                }
                if (!keys.Add(key))
                {
                    result = Empty;
                    error = $"Entry {entryIndex + 1} repeats key '{key}'.";
                    return false;
                }
                if (!TryCreateEntry(key, typeName, value, out var entry, out error))
                {
                    result = Empty;
                    error = $"Entry {entryIndex + 1}: {error}";
                    return false;
                }
                entries.Add(entry);
            }

            result = entries.Count == 0 ? Empty : new TaskBlackboardInjection(entries);
            error = string.Empty;
            return true;
        }

        public long GetLong(string key, long defaultValue = 0)
        {
            for (var i = 0; i < m_Entries.Count; i++)
            {
                var entry = m_Entries[i];
                if (entry.Key == key && IsLongType(entry.ValueType))
                    return entry.LongValue;
            }
            return defaultValue;
        }

        public double GetDouble(string key, double defaultValue = 0d)
        {
            for (var i = 0; i < m_Entries.Count; i++)
            {
                var entry = m_Entries[i];
                if (entry.Key == key && IsDoubleType(entry.ValueType))
                    return entry.DoubleValue;
            }
            return defaultValue;
        }

        public string GetString(string key, string defaultValue = "")
        {
            for (var i = 0; i < m_Entries.Count; i++)
            {
                var entry = m_Entries[i];
                if (entry.Key == key && entry.ValueType == ETaskBlackboardInjectionValueType.String)
                    return entry.StringValue;
            }
            return defaultValue;
        }

        public string Serialize()
        {
            if (m_Entries.Count == 0)
                return string.Empty;

            var builder = new StringBuilder();
            for (var i = 0; i < m_Entries.Count; i++)
            {
                if (i > 0) builder.Append(';');
                var entry = m_Entries[i];
                builder.Append(Escape(entry.Key));
                builder.Append(',');
                builder.Append(GetTypeName(entry.ValueType));
                builder.Append(',');
                builder.Append(Escape(GetValueText(entry)));
            }
            return builder.ToString();
        }

        public override string ToString() => Serialize();

        private static bool TryCreateEntry(
            string key,
            string typeName,
            string value,
            out TaskBlackboardInjectionEntry entry,
            out string error)
        {
            switch (typeName.ToLowerInvariant())
            {
                case "bool":
                    if (value == "1" || bool.TryParse(value, out var boolValue) && boolValue)
                    {
                        entry = new TaskBlackboardInjectionEntry(key, ETaskBlackboardInjectionValueType.Bool, 1, 0d, null);
                        error = string.Empty;
                        return true;
                    }
                    if (value == "0" || bool.TryParse(value, out boolValue) && !boolValue)
                    {
                        entry = new TaskBlackboardInjectionEntry(key, ETaskBlackboardInjectionValueType.Bool, 0, 0d, null);
                        error = string.Empty;
                        return true;
                    }
                    break;
                case "int":
                    if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var intValue))
                    {
                        entry = new TaskBlackboardInjectionEntry(key, ETaskBlackboardInjectionValueType.Int, intValue, 0d, null);
                        error = string.Empty;
                        return true;
                    }
                    break;
                case "long":
                    if (long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var longValue))
                    {
                        entry = new TaskBlackboardInjectionEntry(key, ETaskBlackboardInjectionValueType.Long, longValue, 0d, null);
                        error = string.Empty;
                        return true;
                    }
                    break;
                case "float":
                    if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var floatValue) &&
                        !float.IsNaN(floatValue) && !float.IsInfinity(floatValue))
                    {
                        entry = new TaskBlackboardInjectionEntry(key, ETaskBlackboardInjectionValueType.Float, 0, floatValue, null);
                        error = string.Empty;
                        return true;
                    }
                    break;
                case "double":
                    if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var doubleValue) &&
                        !double.IsNaN(doubleValue) && !double.IsInfinity(doubleValue))
                    {
                        entry = new TaskBlackboardInjectionEntry(key, ETaskBlackboardInjectionValueType.Double, 0, doubleValue, null);
                        error = string.Empty;
                        return true;
                    }
                    break;
                case "string":
                    entry = new TaskBlackboardInjectionEntry(key, ETaskBlackboardInjectionValueType.String, 0, 0d, value);
                    error = string.Empty;
                    return true;
                default:
                    entry = default;
                    error = $"Unsupported type '{typeName}'. Supported types: bool, int, long, float, double, string.";
                    return false;
            }

            entry = default;
            error = $"Value '{value}' is invalid for type '{typeName}'.";
            return false;
        }

        private static bool TrySplitEscaped(
            string text,
            char delimiter,
            int maximumParts,
            out List<string> parts,
            out string error)
        {
            parts = new List<string>();
            var builder = new StringBuilder();
            for (var i = 0; i < text.Length; i++)
            {
                var current = text[i];
                if (current == '\\')
                {
                    if (i + 1 >= text.Length)
                    {
                        error = "A trailing backslash is not a valid escape.";
                        return false;
                    }
                    builder.Append(current);
                    builder.Append(text[++i]);
                    continue;
                }
                if (current == delimiter && parts.Count < maximumParts - 1)
                {
                    parts.Add(builder.ToString());
                    builder.Clear();
                    continue;
                }
                builder.Append(current);
            }
            parts.Add(builder.ToString());
            error = string.Empty;
            return true;
        }

        private static bool TryUnescape(string text, out string result, out string error)
        {
            if (text.IndexOf('\\') < 0)
            {
                result = text;
                error = string.Empty;
                return true;
            }

            var builder = new StringBuilder(text.Length);
            for (var i = 0; i < text.Length; i++)
            {
                var current = text[i];
                if (current != '\\')
                {
                    builder.Append(current);
                    continue;
                }
                if (++i >= text.Length || text[i] != '\\' && text[i] != ',' && text[i] != ';')
                {
                    result = string.Empty;
                    error = "Only \\\\, \\, and \\; are valid escapes.";
                    return false;
                }
                builder.Append(text[i]);
            }
            result = builder.ToString();
            error = string.Empty;
            return true;
        }

        private static string Escape(string text)
        {
            return (text ?? string.Empty)
                .Replace("\\", "\\\\")
                .Replace(",", "\\,")
                .Replace(";", "\\;");
        }

        private static bool IsLongType(ETaskBlackboardInjectionValueType type)
        {
            return type == ETaskBlackboardInjectionValueType.Bool ||
                   type == ETaskBlackboardInjectionValueType.Int ||
                   type == ETaskBlackboardInjectionValueType.Long;
        }

        private static bool IsDoubleType(ETaskBlackboardInjectionValueType type)
        {
            return type == ETaskBlackboardInjectionValueType.Float ||
                   type == ETaskBlackboardInjectionValueType.Double;
        }

        private static string GetTypeName(ETaskBlackboardInjectionValueType type)
        {
            switch (type)
            {
                case ETaskBlackboardInjectionValueType.Bool: return "bool";
                case ETaskBlackboardInjectionValueType.Int: return "int";
                case ETaskBlackboardInjectionValueType.Long: return "long";
                case ETaskBlackboardInjectionValueType.Float: return "float";
                case ETaskBlackboardInjectionValueType.Double: return "double";
                case ETaskBlackboardInjectionValueType.String: return "string";
                default: throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }

        private static string GetValueText(TaskBlackboardInjectionEntry entry)
        {
            switch (entry.ValueType)
            {
                case ETaskBlackboardInjectionValueType.Bool:
                    return entry.LongValue == 0 ? "false" : "true";
                case ETaskBlackboardInjectionValueType.Int:
                case ETaskBlackboardInjectionValueType.Long:
                    return entry.LongValue.ToString(CultureInfo.InvariantCulture);
                case ETaskBlackboardInjectionValueType.Float:
                    return ((float)entry.DoubleValue).ToString("R", CultureInfo.InvariantCulture);
                case ETaskBlackboardInjectionValueType.Double:
                    return entry.DoubleValue.ToString("R", CultureInfo.InvariantCulture);
                case ETaskBlackboardInjectionValueType.String:
                    return entry.StringValue ?? string.Empty;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}
