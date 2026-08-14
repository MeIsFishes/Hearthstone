using System;
using System.Text;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using LitJson;

namespace BbxCommon
{
    public static class JsonApi
    {
        private static string m_TypeInfoKey = "Default.TypeInfo";
        private static string m_FullTypeKey = "FullType";
        private static string m_SpecialTypeKey = "SpecialType";
        private static string m_GenericType1Key = "GenericType1";
        private static string m_GenericType2Key = "GenericType2";

        #region Serialize

        #region API
        public static JsonData Serialize(object obj)
        {
            try
            {
                var jsonData = ConvertObjectToJsonData(obj);
                return jsonData;
            }
            catch (Exception e)
            {
                DebugApi.LogError("Json serialization of " + obj.GetType().FullName + " failed!");
                DebugApi.LogException(e);
                return null;
            }
        }

        public static string SerializeToString(object obj, bool prettyPrint = false)
        {
            try
            {
                var jsonData = ConvertObjectToJsonData(obj);
                if (prettyPrint == false)
                    return jsonData.ToJson();
                var jsonWriter = new JsonWriter(new StringBuilder());
                jsonWriter.PrettyPrint = true;
                jsonWriter.IndentValue = 4;
                JsonMapper.ToJson(jsonData, jsonWriter);
                return jsonWriter.TextWriter.ToString();
            }
            catch (Exception e)
            {
                DebugApi.LogError("Json serialization to string failed!");
                DebugApi.LogException(e);
                return null;
            }
        }

        public static JsonData Serialize(object obj, string absolutePath)
        {
            JsonData jsonData = null;
            StreamWriter streamWriter = null;
            try
            {
                absolutePath = FileApi.AddExtensionIfNot(absolutePath, ".json");
                jsonData = ConvertObjectToJsonData(obj);
                var jsonWriter = new JsonWriter(new StringBuilder());
                jsonWriter.PrettyPrint = true;
                jsonWriter.IndentValue = 4;
                JsonMapper.ToJson(jsonData, jsonWriter);
                FileApi.CreateAbsolutePathFile(absolutePath);
                streamWriter = new StreamWriter(absolutePath);
                streamWriter.Write(jsonWriter.TextWriter.ToString());
                DebugApi.Log("Serialized the current file to " + absolutePath + ".");
                return jsonData;
            }
            catch (Exception e)
            {
                DebugApi.LogError("Json serialization exports " + obj.GetType().FullName + " to " + absolutePath + " failed!");
                DebugApi.LogException(e);
                return null;
            }
            finally
            {
                if (streamWriter != null)
                {
                    streamWriter.Flush();
                    streamWriter.Close();
                }
            }
        }
        #endregion

        #region Body
        private static JsonData ConvertObjectToJsonData(object obj)
        {
            if (obj == null)
            {
                return new JsonData("null");
            }
            if (obj is Boolean booleanObj)
            {
                return new JsonData((bool)booleanObj);
            }
            if (obj is Char charObj)
            {
                return new JsonData(charObj.ToString());
            }
            if (obj is SByte sbyteObj)
            {
                return new JsonData((int)sbyteObj);
            }
            if (obj is Byte byteObj)
            {
                return new JsonData((int)byteObj);
            }
            if (obj is Int16 int16Obj)
            {
                return new JsonData((int)int16Obj);
            }
            if (obj is UInt16 uint16Obj)
            {
                return new JsonData((int)uint16Obj);
            }
            if (obj is Int32 int32Obj)
            {
                return new JsonData((int)int32Obj);
            }
            if (obj is UInt32 uint32Obj)
            {
                return new JsonData((long)uint32Obj);
            }
            if (obj is Int64 int64Obj)
            {
                return new JsonData((long)int64Obj);
            }
            if (obj is UInt64 uint64Obj)
            {
                return uint64Obj <= long.MaxValue
                    ? new JsonData((long)uint64Obj)
                    : new JsonData(uint64Obj.ToString(CultureInfo.InvariantCulture));
            }
            if (obj is Single singleObj)
            {
                return new JsonData((double)singleObj);
            }
            if (obj is Double doubleObj)
            {
                return new JsonData((double)doubleObj);
            }
            if (obj is Decimal decimalObj)
            {
                return new JsonData(decimalObj.ToString(CultureInfo.InvariantCulture));
            }
            if (obj is String stringObj)
            {
                return new JsonData((string)stringObj);
            }
            if (obj is Enum enumObj)
            {
                var enumJsonData = new JsonData();
                enumJsonData[m_TypeInfoKey] = new JsonData();
                enumJsonData[m_TypeInfoKey][m_FullTypeKey] = new JsonData(enumObj.GetType().FullName);
                enumJsonData["Value"] = new JsonData(Enum.GetName(enumObj.GetType(), obj));
                return enumJsonData;
            }
            if (obj is Delegate)
            {
                return new JsonData("null");
            }
            // special types
            var type = obj.GetType();
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
            {
                return ConvertListToJsonData(obj, type);
            }
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(HashSet<>))
            {
                return ConvertHashSetToJsonData(obj, type);
            }
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Dictionary<,>))
            {
                return ConvertDictionaryToJsonData(obj, type);
            }
            // serialize class
            var jsonData = new JsonData();
            jsonData[m_TypeInfoKey] = GenerateTypeInfo(type);
            while (type != null) // check if it has base class
            {
                foreach (var field in type.GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance))
                {
                    var value = field.GetValue(obj);
                    jsonData[field.Name] = ConvertObjectToJsonData(value);
                }
                type = type.BaseType;
            }
            return jsonData;
        }
        #endregion

        #region Special Types
        private static JsonData ConvertListToJsonData(object obj, Type type)
        {
            var listJsonData = new JsonData();
            listJsonData[m_TypeInfoKey] = GenerateTypeInfo(type);
            var enumerator = obj as IEnumerable;
            int index = 0;
            foreach (var item in enumerator)
            {
                listJsonData[index.ToString()] = ConvertObjectToJsonData(item);
                index++;
            }
            return listJsonData;
        }

        private static JsonData ConvertHashSetToJsonData(object obj, Type type)
        {
            var hashSetJsonData = new JsonData();
            hashSetJsonData[m_TypeInfoKey] = GenerateTypeInfo(type);
            var enumerator = obj as IEnumerable;
            int index = 0;
            foreach (var item in enumerator)
            {
                hashSetJsonData[index.ToString()] = ConvertObjectToJsonData(item);
                index++;
            }
            return hashSetJsonData;
        }

        private static JsonData ConvertDictionaryToJsonData(object obj, Type type)
        {
            var dicJsonData = new JsonData();
            dicJsonData[m_TypeInfoKey] = GenerateTypeInfo(type);
            var enumerator = (obj as IDictionary).GetEnumerator();
            int index = 0;
            while (enumerator.MoveNext())
            {
                dicJsonData[index.ToString() + ", Key"] = ConvertObjectToJsonData(enumerator.Key);
                dicJsonData[index.ToString() + ", Value"] = ConvertObjectToJsonData(enumerator.Value);
                index++;
            }
            return dicJsonData;
        }
        #endregion

        #region Type Info
        private static JsonData GenerateTypeInfo(Type type)
        {
            var jsonData = new JsonData();
            // special types
            if (type.IsGenericType)
            {
                if (type.GetGenericTypeDefinition() == typeof(List<>))
                {
                    jsonData[m_SpecialTypeKey] = new JsonData("List");
                    jsonData[m_GenericType1Key] = GenerateTypeInfo(type.GetGenericArguments()[0]);
                }
                else if (type.GetGenericTypeDefinition() == typeof(HashSet<>))
                {
                    jsonData[m_SpecialTypeKey] = new JsonData("HashSet");
                    jsonData[m_GenericType1Key] = GenerateTypeInfo(type.GetGenericArguments()[0]);
                }
                else if (type.GetGenericTypeDefinition() == typeof(Dictionary<,>))
                {
                    jsonData[m_SpecialTypeKey] = new JsonData("Dictionary");
                    jsonData[m_GenericType1Key] = GenerateTypeInfo(type.GetGenericArguments()[0]);
                    jsonData[m_GenericType2Key] = GenerateTypeInfo(type.GetGenericArguments()[1]);
                }
            }
            else
            {
                if (type == typeof(bool))
                    jsonData[m_SpecialTypeKey] = new JsonData("bool");
                else if (type == typeof(char))
                    jsonData[m_SpecialTypeKey] = new JsonData("char");
                else if (type == typeof(sbyte))
                    jsonData[m_SpecialTypeKey] = new JsonData("sbyte");
                else if (type == typeof(byte))
                    jsonData[m_SpecialTypeKey] = new JsonData("byte");
                else if (type == typeof(short))
                    jsonData[m_SpecialTypeKey] = new JsonData("short");
                else if (type == typeof(ushort))
                    jsonData[m_SpecialTypeKey] = new JsonData("ushort");
                else if (type == typeof(int))
                    jsonData[m_SpecialTypeKey] = new JsonData("int");
                else if (type == typeof(uint))
                    jsonData[m_SpecialTypeKey] = new JsonData("uint");
                else if (type == typeof(long))
                    jsonData[m_SpecialTypeKey] = new JsonData("long");
                else if (type == typeof(ulong))
                    jsonData[m_SpecialTypeKey] = new JsonData("ulong");
                else if (type == typeof(float))
                    jsonData[m_SpecialTypeKey] = new JsonData("float");
                else if (type == typeof(double))
                    jsonData[m_SpecialTypeKey] = new JsonData("double");
                else if (type == typeof(decimal))
                    jsonData[m_SpecialTypeKey] = new JsonData("decimal");
                else if (type == typeof(string))
                    jsonData[m_SpecialTypeKey] = new JsonData("string");
                else
                    jsonData[m_FullTypeKey] = type.FullName;
            }
            return jsonData;
        }
        #endregion

        #endregion

        #region Deserialize

        #region API
        public static object Deserialize(JsonData jsonData)
        {
            object res = null;
            try
            {
                res = ConvertJsonDataToObject(jsonData);
                return res;
            }
            catch (Exception e)
            {
                DebugApi.LogError("Json deserialization failed!");
                DebugApi.LogException(e);
                return null;
            }
        }

        public static T DeserializeFromString<T>(string jsonString)
        {
            try
            {
                return Deserialize<T>(JsonMapper.ToObject(jsonString));
            }
            catch (Exception e)
            {
                DebugApi.LogError("Json deserialization from string failed!");
                DebugApi.LogException(e);
                return default;
            }
        }

        public static object Deserialize(string absolutePath)
        {
            StreamReader streamReader = null;
            object res = null;
            try
            {
                absolutePath = FileApi.AddExtensionIfNot(absolutePath, ".json");
                streamReader = new StreamReader(absolutePath);
                var jsonString = streamReader.ReadToEnd();
                var jsonData = JsonMapper.ToObject(jsonString);
                res = Deserialize(jsonData);
                return res;
            }
            catch (Exception e)
            {
                DebugApi.LogException(e);
                return null;
            }
            finally
            {
                if (streamReader != null)
                    streamReader.Close();
                if (res == null)
                    DebugApi.LogError("Json deserialization failed! File path: " + absolutePath);
            }
        }

        public static void Deserialize<T>(string absolutePath, out T obj)
        {
            StreamReader streamReader = null;
            obj = default;
            bool succeeded = false;
            try
            {
                absolutePath = FileApi.AddExtensionIfNot(absolutePath, ".json");
                streamReader = new StreamReader(absolutePath);
                var jsonString = streamReader.ReadToEnd();
                var jsonData = JsonMapper.ToObject(jsonString);
                obj = (T)Deserialize(jsonData);
                succeeded = true;
            }
            catch (Exception e)
            {
                DebugApi.LogException(e);
            }
            finally
            {
                if (streamReader != null)
                    streamReader.Close();
                if (succeeded == false)
                    DebugApi.LogError("Json deserialization failed! File path: " + absolutePath);
            }
        }

        public static T Deserialize<T>(JsonData jsonData)
        {
            var obj = Deserialize(jsonData);
            if (obj == null)
            {
                return default;
            }
            var converted = ConvertToDeclaredType(obj, typeof(T));
            if (converted is T t)
            {
                return t;
            }
            else
            {
                DebugApi.LogError("Json deserialization succeeded, but its type is " + obj.GetType().FullName + ", as you require " + typeof(T).FullName + ".");
                return default;
            }
        }

        public static T Deserialize<T>(string absolutePath)
        {
            var obj = Deserialize(absolutePath);
            if (obj == null)
            {
                return default;
            }
            if (obj is T t)
            {
                return t;
            }
            else
            {
                DebugApi.LogError("Json deserialization succeeded, but its type is " + obj.GetType().FullName + ", as you require " + typeof(T).FullName + ".");
                return default;
            }
        }

        public static bool TryDeserialize<T>(JsonData jsonData, T res)
        {
            try
            {
                ConvertJsonDataToObject(jsonData, res);
                return true;
            }
            catch (Exception e)
            {
                DebugApi.LogException(e);
                return false;
            }
        }

        public static bool TryDeserialize<T>(string absolutePath, T obj)
        {
            StreamReader streamReader = null;
            bool succeeded = false;
            try
            {
                absolutePath = FileApi.AddExtensionIfNot(absolutePath, ".json");
                streamReader = new StreamReader(absolutePath);
                var jsonString = streamReader.ReadToEnd();
                var jsonData = JsonMapper.ToObject(jsonString);
                ConvertJsonDataToObject(jsonData, obj);
                succeeded = true;
                return true;
            }
            catch (Exception e)
            {
                DebugApi.LogException(e);
                return false;
            }
            finally
            {
                if (streamReader != null)
                    streamReader.Close();
                if (succeeded == false)
                    DebugApi.LogError("Json deserialization failed! File path: " + absolutePath);
            }
        }
        #endregion

        #region Body
        private static object ConvertJsonDataToObject(JsonData jsonData)
        {
            if (jsonData.GetJsonType() == JsonType.Object && jsonData.ContainsKey(m_TypeInfoKey))
            {
                Type type = DeserializeTypeInfo(jsonData[m_TypeInfoKey]);
                // enum
                if (type.IsEnum)
                {
                    var enumValue = Enum.Parse(type, (string)jsonData["Value"]);
                    return enumValue;
                }
                // delegate
                else if (type.IsSubclassOf(typeof(Delegate)))
                {
                    return null;
                }
                // special types
                else if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
                {
                    return ConvertJsonDataToList(jsonData, type);
                }
                else if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(HashSet<>))
                {
                    return ConvertJsonDataToHashSet(jsonData, type);
                }
                else if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Dictionary<,>))
                {
                    return ConvertJsonDataToDictionary(jsonData, type);
                }
                else
                {
                    var obj = Activator.CreateInstance(type);
                    foreach (var key in jsonData.Keys)
                    {
                        if (key == m_TypeInfoKey)
                            continue;
                        var field = type.GetField(key, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
                        if (field == null) // check if it is in base class
                        {
                            var baseType = type.BaseType;
                            while (baseType != null)
                            {
                                field = baseType.GetField(key, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
                                if (field != null)
                                    break;
                                baseType = baseType.BaseType;
                            }
                            if (field == null)  // the field may be deleted
                                continue;
                        }
                        var value = ConvertJsonDataToObject(jsonData[key]);
                        var finalValue = ConvertToDeclaredType(value, field.FieldType);
                        field.SetValue(obj, finalValue);
                    }
                    return obj;
                }
            }
            switch (jsonData.GetJsonType())
            {
                case JsonType.Boolean:
                    return (bool)jsonData;
                case JsonType.Int:
                    return (int)jsonData;
                case JsonType.Long:
                    return (long)jsonData;
                case JsonType.Double:
                    return (double)jsonData;
                case JsonType.String:
                    if ((string)jsonData == "null")
                        return null;
                    else
                        return (string)jsonData;
            }
            return null;
        }

        /// <summary>
        /// This function is for deserializing an object that has been created by the user, such as a class instance.
        /// <para>In some cases, you can only get "this" instance, for eaxample: JsonApi.TryDeserialize(path, this).</para>
        /// If so, you can use this function to deserialize the JsonData into the "this" instance. Otherwise it's not recommended.
        /// </summary>
        private static void ConvertJsonDataToObject(JsonData jsonData, object res)
        {
            Type type = res.GetType();
            if (type.IsClass == false)
            {
                DebugApi.LogError("Json Deserializer: You should only pass class references to ConvertJsonDataToObject(JsonData, object), but got " + type.FullName + ".");
            }
            foreach (var key in jsonData.Keys)
            {
                if (key == m_TypeInfoKey)
                    continue;
                var field = type.GetField(key, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
                if (field == null) // check if it is in base class
                {
                    var baseType = type.BaseType;
                    while (baseType != null)
                    {
                        field = baseType.GetField(key, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
                        if (field != null)
                            break;
                        baseType = baseType.BaseType;
                    }
                    if (field == null)  // the field may be deleted
                        continue;
                }
                var value = ConvertJsonDataToObject(jsonData[key]);
                var finalValue = ConvertToDeclaredType(value, field.FieldType);
                field.SetValue(res, finalValue);
            }
        }
        #endregion

        #region Special Types
        private static object ConvertJsonDataToList(JsonData jsonData, Type type)
        {
            var list = Activator.CreateInstance(type);
            var addMethod = type.GetMethod("Add");
            int index = 0;
            while (jsonData.ContainsKey(index.ToString()))
            {
                var element = ConvertToDeclaredType(ConvertJsonDataToObject(jsonData[index.ToString()]), type.GetGenericArguments()[0]);
                addMethod.Invoke(list, new object[] { element });
                index++;
            }
            return list;
        }

        private static object ConvertJsonDataToHashSet(JsonData jsonData, Type type)
        {
            var hashSet = Activator.CreateInstance(type);
            var addMethod = type.GetMethod("Add");
            int index = 0;
            while (jsonData.ContainsKey(index.ToString()))
            {
                var element = ConvertToDeclaredType(ConvertJsonDataToObject(jsonData[index.ToString()]), type.GetGenericArguments()[0]);
                addMethod.Invoke(hashSet, new object[] { element });
                index++;
            }
            return hashSet;
        }

        private static object ConvertJsonDataToDictionary(JsonData jsonData, Type type)
        {
            var dic = Activator.CreateInstance(type);
            var addMethod = type.GetMethod("Add");
            var genericTypes = type.GetGenericArguments();
            int index = 0;
            while (jsonData.ContainsKey(index.ToString() + ", Key"))
            {
                var key = ConvertToDeclaredType(ConvertJsonDataToObject(jsonData[index.ToString() + ", Key"]), genericTypes[0]);
                var value = ConvertToDeclaredType(ConvertJsonDataToObject(jsonData[index.ToString() + ", Value"]), genericTypes[1]);
                addMethod.Invoke(dic, new object[] { key, value });
                index++;
            }
            return dic;
        }

        private static object ConvertToDeclaredType(object value, Type targetType)
        {
            if (value == null)
                return targetType.IsValueType && Nullable.GetUnderlyingType(targetType) == null
                    ? Activator.CreateInstance(targetType)
                    : null;
            var actualTarget = Nullable.GetUnderlyingType(targetType) ?? targetType;
            if (actualTarget.IsInstanceOfType(value))
                return value;
            if (actualTarget.IsEnum)
                return value is string enumName
                    ? Enum.Parse(actualTarget, enumName)
                    : Enum.ToObject(actualTarget, value);
            if (actualTarget == typeof(char) && value is string charValue)
                return charValue.Length > 0 ? charValue[0] : default(char);
            return Convert.ChangeType(value, actualTarget, CultureInfo.InvariantCulture);
        }
        #endregion

        #region Type Info
        private static Type DeserializeTypeInfo(JsonData jsonData)
        {
            if (jsonData.ContainsKey(m_SpecialTypeKey))
            {
                Type type = null;
                switch ((string)jsonData[m_SpecialTypeKey])
                {
                    case "bool":
                        return typeof(bool);
                    case "char":
                        return typeof(char);
                    case "sbyte":
                        return typeof(sbyte);
                    case "byte":
                        return typeof(byte);
                    case "short":
                        return typeof(short);
                    case "ushort":
                        return typeof(ushort);
                    case "int":
                        return typeof(int);
                    case "uint":
                        return typeof(uint);
                    case "long":
                        return typeof(long);
                    case "ulong":
                        return typeof(ulong);
                    case "float":
                        return typeof(float);
                    case "double":
                        return typeof(double);
                    case "decimal":
                        return typeof(decimal);
                    case "string":
                        return typeof(string);
                    case "List":
                        type = typeof(List<>);
                        type = type.MakeGenericType(DeserializeTypeInfo(jsonData[m_GenericType1Key]));
                        return type;
                    case "HashSet":
                        type = typeof(HashSet<>);
                        type = type.MakeGenericType(DeserializeTypeInfo(jsonData[m_GenericType1Key]));
                        return type;
                    case "Dictionary":
                        type = typeof(Dictionary<,>);
                        type = type.MakeGenericType(DeserializeTypeInfo(jsonData[m_GenericType1Key]), DeserializeTypeInfo(jsonData[m_GenericType2Key]));
                        return type;
                }
            }
            else if (jsonData.ContainsKey(m_FullTypeKey))
            {
                var fullType = (string)jsonData[m_FullTypeKey];
                return ReflectionApi.GetType(fullType);
            }
            return null;
        }
        #endregion

        #endregion
    }
}
