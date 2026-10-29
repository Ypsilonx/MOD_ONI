using System;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using IronPython.Runtime;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace PyCPU.Scripting
{
    /// <summary>Chyba hodnoty, kterou skript předal hře (io.out, storage). Hláška je pro hráče.</summary>
    internal sealed class ScriptValueException : Exception
    {
        public ScriptValueException(string message) : base(message) { }
    }

    /// <summary>
    /// Převod slovníku <c>storage</c> mezi Pythonem a JSON (uložení do savu).
    /// Povolené typy: dict s klíči str, list, tuple (uloží se jako list), str, int (max. 64 bitů), float, bool, None.
    /// Newtonsoft ve hře nepodporuje BigInteger, proto limit 64 bitů.
    /// </summary>
    internal static class StorageConverter
    {
        /// <summary>Načte storage z JSON; null nebo prázdný řetězec dá prázdný dict.</summary>
        public static PythonDictionary FromJson(string json)
        {
            if (string.IsNullOrEmpty(json))
                return new PythonDictionary();
            return (PythonDictionary)FromToken(JObject.Parse(json));
        }

        /// <summary>Uloží storage do JSON.</summary>
        /// <exception cref="ScriptValueException">storage obsahuje typ, který nejde uložit.</exception>
        public static string ToJson(PythonDictionary storage) =>
            ToToken(storage, "storage").ToString(Formatting.None);

        /// <summary>Převede JSON uzel na Python hodnotu.</summary>
        private static object FromToken(JToken token)
        {
            switch (token.Type)
            {
                case JTokenType.Object:
                    var dict = new PythonDictionary();
                    foreach (JProperty p in ((JObject)token).Properties())
                        dict[p.Name] = FromToken(p.Value);
                    return dict;
                case JTokenType.Array:
                    var list = new PythonList();
                    foreach (JToken item in (JArray)token)
                        list.append(FromToken(item));
                    return list;
                case JTokenType.Integer:
                    long l = token.Value<long>();
                    return l >= int.MinValue && l <= int.MaxValue ? (object)(int)l : new BigInteger(l);
                case JTokenType.Float:
                    return token.Value<double>();
                case JTokenType.Boolean:
                    return token.Value<bool>();
                case JTokenType.Null:
                    return null;
                default:
                    return token.Value<string>();
            }
        }

        /// <summary>Převede Python hodnotu na JSON uzel; path slouží jen pro chybovou hlášku.</summary>
        private static JToken ToToken(object value, string path)
        {
            switch (value)
            {
                case null:
                    return JValue.CreateNull();
                case bool b:
                    return new JValue(b);
                case int i:
                    return new JValue(i);
                case BigInteger big when big >= long.MinValue && big <= long.MaxValue:
                    return new JValue((long)big);
                case BigInteger _:
                    throw new ScriptValueException($"{path}: int větší než 64 bitů nejde uložit.");
                case double d:
                    return new JValue(d);
                case string s:
                    return new JValue(s);
                case PythonDictionary dict:
                    var obj = new JObject();
                    foreach (KeyValuePair<object, object> item in dict)
                    {
                        if (!(item.Key is string key))
                            throw new ScriptValueException(
                                $"{path}: klíč {PythonScript.TypeName(item.Key)} nejde uložit, klíče musí být str.");
                        obj[key] = ToToken(item.Value, $"{path}['{key}']");
                    }
                    return obj;
                case IList list:
                    var array = new JArray();
                    for (int n = 0; n < list.Count; n++)
                        array.Add(ToToken(list[n], $"{path}[{n}]"));
                    return array;
                default:
                    throw new ScriptValueException(
                        $"{path}: typ {PythonScript.TypeName(value)} nejde uložit " +
                        "(jen dict, list, tuple, str, int, float, bool, None).");
            }
        }
    }
}
