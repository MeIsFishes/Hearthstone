using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using BbxCommon.Internal;
using UnityEngine;

namespace BbxCommon
{
    public abstract class CsvDataBase<T> : CsvDataBase where T : CsvDataBase<T>, new()
    {
        #region Body
        public override sealed void ReadFromAbsolutePath(string absolutePath)
        {
            ReadWithAbsolutePath<T>(absolutePath);
        }

        public override sealed void ReadFromString(string filePath, string content)
        {
            ReadFromString<T>(filePath, content);
        }
        #endregion
    }
}

namespace BbxCommon.Internal
{
    public abstract class CsvDataBase
    {
        #region Body
        public enum EDataLoad
        {
            Addition,
            Override,
        }

        public virtual EDataLoad GetDataLoadType() { return EDataLoad.Addition; }
        public virtual string GetDataGroup() { return "GameEngineDefault"; }
        public abstract string[] GetTableNames();

        internal static List<string> CsvKeys = new List<string>();
        internal static Dictionary<string, string> KeyValuePairs = new Dictionary<string, string>();

        // dynamic data
        internal static string CurrentPath;
        internal static int CurrentLineIndex;

        internal static void ReadWithAbsolutePath<T>(string absolutePath) where T : CsvDataBase<T>, new()
        {
            CurrentPath = FileApi.AddExtensionIfNot(absolutePath, ".csv");
            CsvKeys.Clear();
            KeyValuePairs.Clear();
            var streamReader = new StreamReader(CurrentPath);

            // read keys
            var firstLine = streamReader.ReadLine().TryRemoveEnd("\r");
            if (!TryParseCsvRecord(firstLine, out var keys, out var headerError))
            {
                DebugApi.LogError("Broken CSV header in " + CurrentPath + ". " + headerError);
                streamReader.Close();
                return;
            }
            for (int i = 0; i < keys.Count; i++) CsvKeys.Add(keys[i]);

            CurrentLineIndex = 1;
            while (true)
            {
                CurrentLineIndex++;
                var lineStr = streamReader.ReadLine().TryRemoveEnd("\r");
                if (lineStr == null)
                    break;

                if (SplitLineIntoKeyValue(lineStr) == false)
                    continue;

                var csvInstance = new T();
                try
                {
                    csvInstance.ReadLine();
                }
                catch (Exception e)
                {
                    DebugApi.LogError(e);
                }
            }

            streamReader.Close();
        }

        /// <summary>
        /// Read from a standard CSV string.
        /// </summary>
        /// <param name="filePath"> Use for logging broken file. </param>
        /// <param name="content"> The complete CSV string. </param>
        internal static void ReadFromString<T>(string filePath, string content) where T : CsvDataBase<T>, new()
        {
            CurrentPath = FileApi.AddExtensionIfNot(filePath, ".csv");
            CsvKeys.Clear();
            KeyValuePairs.Clear();
            var lines = content.SplitIntoLines();
            if (lines.Length == 0)
            {
                DebugApi.LogError("Invalid CSV string! The file path you pass in is " +  filePath);
                return;
            }

            // read keys
            var firstLine = lines[0];
            if (!TryParseCsvRecord(firstLine, out var keys, out var headerError))
            {
                DebugApi.LogError("Broken CSV header in " + CurrentPath + ". " + headerError);
                return;
            }
            for (int i = 0; i < keys.Count; i++) CsvKeys.Add(keys[i]);

            CurrentLineIndex = 1;
            for (int i = 1; i < lines.Length; i++)
            {
                CurrentLineIndex++;
                if (SplitLineIntoKeyValue(lines[i]) == false)
                    continue;

                var csvInstance = new T();
                try
                {
                    csvInstance.ReadLine();
                }
                catch (Exception e)
                {
                    DebugApi.LogError(e);
                }
            }
        }

        /// <summary>
        /// True means split successfully, false means skip this line.
        /// </summary>
        private static bool SplitLineIntoKeyValue(string lineStr)
        {
            if (lineStr == null)
                return false;

            if (lineStr.StartsWith("//"))   // line starts with "//" will be recognized as comment
                return false;

            if (!TryParseCsvRecord(lineStr, out var lineValues, out var parseError))
            {
                DebugApi.LogError("Broken line in " + CurrentPath + ", line index: " + CurrentLineIndex + ". " + parseError + "\nline content: " + lineStr);
                return false;
            }
            if (lineValues.Count != CsvKeys.Count)
            {
                DebugApi.LogError("Broken line in " + CurrentPath + ", line index: " + CurrentLineIndex + ". Expected " + CsvKeys.Count + " cells but found " + lineValues.Count + ".\nline content: " + lineStr);
                return false;
            }
            for (int i = 0; i < lineValues.Count; i++)
            {
                KeyValuePairs[CsvKeys[i]] = lineValues[i];
            }
            return true;
        }

        private static bool TryParseCsvRecord(string line, out List<string> values, out string error)
        {
            values = new List<string>();
            if (line == null)
            {
                error = "The record is null.";
                return false;
            }

            var value = new System.Text.StringBuilder();
            var inQuotes = false;
            var quoteClosed = false;
            for (var i = 0; i < line.Length; i++)
            {
                var current = line[i];
                if (inQuotes)
                {
                    if (current != '"')
                    {
                        value.Append(current);
                        continue;
                    }
                    if (i + 1 < line.Length && line[i + 1] == '"')
                    {
                        value.Append('"');
                        i++;
                        continue;
                    }
                    inQuotes = false;
                    quoteClosed = true;
                    continue;
                }

                if (current == ',')
                {
                    values.Add(value.ToString());
                    value.Clear();
                    quoteClosed = false;
                    continue;
                }
                if (current == '"')
                {
                    if (value.Length != 0 || quoteClosed)
                    {
                        error = "A quote appeared inside an unquoted field.";
                        return false;
                    }
                    inQuotes = true;
                    continue;
                }
                if (quoteClosed)
                {
                    error = "Only a comma may follow a closing quote.";
                    return false;
                }
                value.Append(current);
            }

            if (inQuotes)
            {
                error = "A quoted field was not closed.";
                return false;
            }
            values.Add(value.ToString());
            error = string.Empty;
            return true;
        }

        protected abstract void ReadLine();
        public abstract void ReadFromAbsolutePath(string absolutePath);
        public abstract void ReadFromString(string filePath, string content);
        #endregion

        #region Parse

        #region Parse Single Object
        protected string GetStringFromKey(string key)
        {
            return KeyValuePairs[key];
        }

        private bool ParseBool(string str, out bool result)
        {
            bool succeeded = false;
            if (str == "0" || str.ToUpper() == "FALSE")
            {
                succeeded = true;
                result = false;
            }
            else if (str == "1" || str.ToUpper() == "TRUE")
            {
                succeeded = true;
                result = true;
            }
            else
            {
                succeeded = false;
                result = false;
            }
            return succeeded;
        }

        protected bool ParseBoolFromKey(string key, bool defaultValue = false)
        {
            var str = KeyValuePairs[key];
            if (str == "")
            {
                return defaultValue;
            }
            if (ParseBool(str, out var result))
            {
                return result;
            }
            else
            {
                DebugApi.LogError("Broken CSV cell!\nfile path: " + CurrentPath + "\nline: " + CurrentLineIndex + "\nkey: " + key + "\nrequire type: bool\nvalue: " + str);
                return defaultValue;
            }
        }

        protected int ParseIntFromKey(string key, int defaultValue = 0)
        {
            var str = KeyValuePairs[key];
            if (str == "")
            {
                return defaultValue;
            }
            if (int.TryParse(str, out int result))
            {
                return result;
            }
            else
            {
                DebugApi.LogError("Broken CSV cell!\nfile path: " + CurrentPath + "\nline: " + CurrentLineIndex + "\nkey: " + key + "\nrequire type: int\nvalue: " + str);
                return defaultValue;
            }
        }

        protected uint ParseUintFromKey(string key, uint defaultValue = 0)
        {
            var str = KeyValuePairs[key];
            if (str == "")
            {
                return defaultValue;
            }
            if (uint.TryParse(str, out uint result))
            {
                return result;
            }
            else
            {
                DebugApi.LogError("Broken CSV cell!\nfile path: " + CurrentPath + "\nline: " + CurrentLineIndex + "\nkey: " + key + "\nrequire type: uint\nvalue: " + str);
                return defaultValue;
            }
        }

        protected long ParseLongFromKey(string key, long defaultValue = 0)
        {
            var str = KeyValuePairs[key];
            if (str == "")
            {
                return defaultValue;
            }
            if (long.TryParse(str, out long result))
            {
                return result;
            }
            else
            {
                DebugApi.LogError("Broken CSV cell!\nfile path: " + CurrentPath + "\nline: " + CurrentLineIndex + "\nkey: " + key + "\nrequire type: long\nvalue: " + str);
                return defaultValue;
            }
        }

        protected ulong ParseUlongFromKey(string key, ulong defaultValue = 0)
        {
            var str = KeyValuePairs[key];
            if (str == "")
            {
                return defaultValue;
            }
            if (ulong.TryParse(str, out ulong result))
            {
                return result;
            }
            else
            {
                DebugApi.LogError("Broken CSV cell!\nfile path: " + CurrentPath + "\nline: " + CurrentLineIndex + "\nkey: " + key + "\nrequire type: ulong\nvalue: " + str);
                return defaultValue;
            }
        }

        protected float ParseFloatFromKey(string key, float defaultValue = 0)
        {
            var str = KeyValuePairs[key];
            if (str == "")
            {
                return defaultValue;
            }
            if (float.TryParse(str, out float result))
            {
                return result;
            }
            else
            {
                DebugApi.LogError("Broken CSV cell!\nfile path: " + CurrentPath + "\nline: " + CurrentLineIndex + "\nkey: " + key + "\nrequire type: float\nvalue: " + str);
                return defaultValue;
            }
        }

        protected double ParseDoubleFromKey(string key, double defaultValue)
        {
            var str = KeyValuePairs[key];
            if (str == "")
            {
                return defaultValue;
            }
            if (double.TryParse(str, out double result))
            {
                return result;
            }
            else
            {
                DebugApi.LogError("Broken CSV cell!\nfile path: " + CurrentPath + "\nline: " + CurrentLineIndex + "\nkey: " + key + "\nrequire type: double\nvalue: " + str);
                return defaultValue;
            }
        }

        /// <summary>
        /// Parses a color from one CSV cell. Values must use #RRGGBB or #RRGGBBAA.
        /// </summary>
        protected Color ParseColorFromKey(string key, Color defaultValue = default)
        {
            var str = KeyValuePairs[key];
            if (string.IsNullOrEmpty(str))
            {
                return defaultValue;
            }

            if ((str.Length == 7 || str.Length == 9) && str[0] == '#' &&
                ColorUtility.TryParseHtmlString(str, out var result))
            {
                return result;
            }

            DebugApi.LogError("Broken CSV cell!\nfile path: " + CurrentPath + "\nline: " + CurrentLineIndex + "\nkey: " + key + "\nrequire type: Color (#RRGGBB or #RRGGBBAA)\nvalue: " + str);
            return defaultValue;
        }

        /// <summary>
        /// Parses a Vector2 from one semicolon-delimited (x;y) CSV cell.
        /// </summary>
        protected Vector2 ParseVector2FromKey(string key, Vector2 defaultValue = default)
        {
            return TryParseVectorComponentsFromKey(key, 2, out var values)
                ? new Vector2(values[0], values[1])
                : defaultValue;
        }

        /// <summary>
        /// Parses a Vector3 from one semicolon-delimited CSV cell.
        /// </summary>
        protected Vector3 ParseVector3FromKey(string key, Vector3 defaultValue = default)
        {
            return TryParseVectorComponentsFromKey(key, 3, out var values)
                ? new Vector3(values[0], values[1], values[2])
                : defaultValue;
        }

        /// <summary>
        /// Parses a Vector4 from one semicolon-delimited CSV cell.
        /// </summary>
        protected Vector4 ParseVector4FromKey(string key, Vector4 defaultValue = default)
        {
            return TryParseVectorComponentsFromKey(key, 4, out var values)
                ? new Vector4(values[0], values[1], values[2], values[3])
                : defaultValue;
        }

        /// <summary>
        /// Parses one typed Task blackboard injection collection from a single CSV cell.
        /// </summary>
        protected TaskBlackboardInjection ParseTaskBlackboardInjectionFromKey(
            string key,
            TaskBlackboardInjection defaultValue = null)
        {
            var str = KeyValuePairs[key];
            if (TaskBlackboardInjection.TryParse(str, out var result, out var error))
                return result;

            DebugApi.LogError("Broken CSV cell!\nfile path: " + CurrentPath + "\nline: " + CurrentLineIndex +
                              "\nkey: " + key + "\nrequire type: TaskBlackboardInjection (Key,Type,Value;...)" +
                              "\nvalue: " + str + "\nerror: " + error);
            return defaultValue ?? TaskBlackboardInjection.Empty;
        }

        private bool TryParseVectorComponentsFromKey(string key, int componentCount, out float[] result)
        {
            var str = KeyValuePairs[key];
            if (string.IsNullOrEmpty(str))
            {
                result = null;
                return false;
            }

            var components = str.Split(';');
            result = new float[componentCount];
            if (components.Length == componentCount)
            {
                for (var i = 0; i < componentCount; i++)
                {
                    if (!float.TryParse(components[i], NumberStyles.Float, CultureInfo.InvariantCulture, out result[i]))
                        break;

                    if (i == componentCount - 1)
                        return true;
                }
            }

            DebugApi.LogError("Broken CSV cell!\nfile path: " + CurrentPath + "\nline: " + CurrentLineIndex + "\nkey: " + key + "\nrequire type: Vector" + componentCount + " (semicolon-delimited)\nvalue: " + str);
            result = null;
            return false;
        }

        protected T ParseEnumFromKey<T>(string key, T defaultValue = default) where T : unmanaged, Enum
        {
            var str = KeyValuePairs[key];
            if (str == "")
            {
                return defaultValue;
            }
            if (Enum.TryParse(str, true, out T result))
            {
                return result;
            }
            else
            {
                DebugApi.LogError("Broken CSV cell!\nfile path: " + CurrentPath + "\nline: " + CurrentLineIndex + "\nkey: " + key + "\nrequire type: " + typeof(T).FullName + "\nvalue: " + str);
                return defaultValue;
            }
        }
        #endregion

        #region Parse Array
        /// <param name="ignoreSpace"> If true, all spaces will be removed. That means you can write array like "1; 2; 3". </param>
        private string[] SplitFromKey(string key, bool ignoreSpace, params char[] separators)
        {
            var str = KeyValuePairs[key];
            if (string.IsNullOrEmpty(str))
            {
                return Array.Empty<string>();
            }
            if (ignoreSpace)
                str = str.Replace(" ", "");
            return str.Split(separators);
        }

        protected string[] GetStringArrayFromKey(string key, bool ignoreSpace, params char[] separators)
        {
            return SplitFromKey(key, ignoreSpace, separators);
        }

        protected string[] GetStringArrayFromKey(string key)
        {
            return SplitFromKey(key, false, ';');
        }

        protected bool[] ParseBoolArrayFromKey(string key, bool ignoreSpace, params char[] separators)
        {
            var strs = SplitFromKey(key, ignoreSpace, separators);
            bool[] result = new bool[strs.Length];
            for (int i = 0; i < strs.Length; i++)
            {
                if (ParseBool(strs[i], out var boolResult))
                {
                    result[i] = boolResult;
                }
                else
                {
                    DebugApi.LogError("Broken CSV cell!\nfile path: " + CurrentPath + "\nline: " + CurrentLineIndex + "\nkey: " + key + "\nrequire type: BoolArray\nvalue: " + strs[i]);
                    return new bool[0];
                }
            }
            return result;
        }

        protected bool[] ParseBoolArrayFromKey(string key)
        {
            return ParseBoolArrayFromKey(key, true, ';');
        }

        protected int[] ParseIntArrayFromKey(string key, bool ignoreSpace, params char[] separators)
        {
            var strs = SplitFromKey(key, ignoreSpace, separators);
            int[] result = new int[strs.Length];
            for (int i = 0; i < strs.Length; i++)
            {
                if (int.TryParse(strs[i], out var intResult))
                {
                    result[i] = intResult;
                }
                else
                {
                    DebugApi.LogError("Broken CSV cell!\nfile path: " + CurrentPath + "\nline: " + CurrentLineIndex + "\nkey: " + key + "\nrequire type: IntArray\nvalue: " + strs[i]);
                    return new int[0];
                }
            }
            return result;
        }

        protected int[] ParseIntArrayFromKey(string key)
        {
            return ParseIntArrayFromKey(key, true, ';');
        }

        protected uint[] ParseUintArrayFromKey(string key, bool ignoreSpace, params char[] separators)
        {
            var strs = SplitFromKey(key, ignoreSpace, separators);
            uint[] result = new uint[strs.Length];
            for (int i = 0; i < strs.Length; i++)
            {
                if (uint.TryParse(strs[i], out var uintResult))
                {
                    result[i] = uintResult;
                }
                else
                {
                    DebugApi.LogError("Broken CSV cell!\nfile path: " + CurrentPath + "\nline: " + CurrentLineIndex + "\nkey: " + key + "\nrequire type: UIntArray\nvalue: " + strs[i]);
                    return new uint[0];
                }
            }
            return result;
        }

        protected uint[] ParseUintArrayFromKey(string key)
        {
            return ParseUintArrayFromKey(key, true, ';');
        }

        protected long[] ParseLongArrayFromKey(string key, bool ignoreSpace, params char[] separators)
        {
            var strs = SplitFromKey(key, ignoreSpace, separators);
            long[] result = new long[strs.Length];
            for (int i = 0; i < strs.Length; i++)
            {
                if (long.TryParse(strs[i], out var longResult))
                {
                    result[i] = longResult;
                }
                else
                {
                    DebugApi.LogError("Broken CSV cell!\nfile path: " + CurrentPath + "\nline: " + CurrentLineIndex + "\nkey: " + key + "\nrequire type: LongArray\nvalue: " + strs[i]);
                    return new long[0];
                }
            }
            return result;
        }

        protected long[] ParseLongArrayFromKey(string key)
        {
            return ParseLongArrayFromKey(key, true, ';');
        }

        protected ulong[] ParseUlongArrayFromKey(string key, bool ignoreSpace, params char[] separators)
        {
            var strs = SplitFromKey(key, ignoreSpace, separators);
            ulong[] result = new ulong[strs.Length];
            for (int i = 0; i < strs.Length; i++)
            {
                if (ulong.TryParse(strs[i], out var ulongResult))
                {
                    result[i] = ulongResult;
                }
                else
                {
                    DebugApi.LogError("Broken CSV cell!\nfile path: " + CurrentPath + "\nline: " + CurrentLineIndex + "\nkey: " + key + "\nrequire type: ULongArray\nvalue: " + strs[i]);
                    return new ulong[0];
                }
            }
            return result;
        }

        protected ulong[] ParseUlongArrayFromKey(string key)
        {
            return ParseUlongArrayFromKey(key, true, ';');
        }

        protected float[] ParseFloatArrayFromKey(string key, bool ignoreSpace, params char[] separators)
        {
            var strs = SplitFromKey(key, ignoreSpace, separators);
            float[] result = new float[strs.Length];
            for (int i = 0; i < strs.Length; i++)
            {
                if (float.TryParse(strs[i], out var floatResult))
                {
                    result[i] = floatResult;
                }
                else
                {
                    DebugApi.LogError("Broken CSV cell!\nfile path: " + CurrentPath + "\nline: " + CurrentLineIndex + "\nkey: " + key + "\nrequire type: FloatArray\nvalue: " + strs[i]);
                    return new float[0];
                }
            }
            return result;
        }

        protected float[] ParseFloatArrayFromKey(string key)
        {
            return ParseFloatArrayFromKey(key, true, ';');
        }

        protected double[] ParseDoubleArrayFromKey(string key, bool ignoreSpace, params char[] separators)
        {
            var strs = SplitFromKey(key, ignoreSpace, separators);
            double[] result = new double[strs.Length];
            for (int i = 0; i < strs.Length; i++)
            {
                if (double.TryParse(strs[i], out var doubleResult))
                {
                    result[i] = doubleResult;
                }
                else
                {
                    DebugApi.LogError("Broken CSV cell!\nfile path: " + CurrentPath + "\nline: " + CurrentLineIndex + "\nkey: " + key + "\nrequire type: DoubleArray\nvalue: " + strs[i]);
                    return new double[0];
                }
            }
            return result;
        }

        protected double[] ParseDoubleArrayFromKey(string key)
        {
            return ParseDoubleArrayFromKey(key, true, ';');
        }

        protected T[] ParseEnumArrayFromKey<T>(string key, bool ignoreSpace, params char[] separators) where T : unmanaged, Enum
        {
            var strs = SplitFromKey(key, ignoreSpace, separators);
            T[] result = new T[strs.Length];
            for (int i = 0; i < strs.Length; i++)
            {
                if (Enum.TryParse<T>(strs[i], true, out var enumResult))
                {
                    result[i] = enumResult;
                }
                else
                {
                    DebugApi.LogError("Broken CSV cell!\nfile path: " + CurrentPath + "\nline: " + CurrentLineIndex + "\nkey: " + key + "\nrequire type: Array of " + typeof(T).FullName + "\nvalue: " + strs[i]);
                    return new T[0];
                }
            }
            return result;
        }

        protected T[] ParseEnumArrayFromKey<T>(string key) where T : unmanaged, Enum
        {
            return ParseEnumArrayFromKey<T>(key, true, ';');
        }
        #endregion

        #endregion
    }
}
