using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Reflection;

using JsonPair = System.Collections.Generic.KeyValuePair<string, System.Json.JsonValue>;
using JsonPairEnumerable = System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<string, System.Json.JsonValue>>;

namespace System.Json
{
    public class JsonObject : JsonValue, IDictionary<string, JsonValue>, ICollection<JsonPair>
    {
        Dictionary<string, JsonValue> map;

        public JsonObject(params JsonPair[] items)
        {
            map = new Dictionary<string, JsonValue>();

            if (items != null)
                AddRange(items);
        }

        public JsonObject(JsonPairEnumerable items)
        {
            if (items == null)
                throw new ArgumentNullException("items");

            map = new Dictionary<string, JsonValue>();
            AddRange(items);
        }

        public override int Count
        {
            get { return map.Count; }
        }

        public IEnumerator<JsonPair> GetEnumerator()
        {
            return map.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return map.GetEnumerator();
        }

        public override sealed JsonValue this[string key]
        {
            get { return map[key]; }
            set { map[key] = value; }
        }

        public override JsonType JsonType
        {
            get { return JsonType.Object; }
        }

        public ICollection<string> Keys
        {
            get { return map.Keys; }
        }

        public ICollection<JsonValue> Values
        {
            get { return map.Values; }
        }

        public void Add(string key, JsonValue value)
        {
            if (key == null)
                throw new ArgumentNullException("key");

            map.Add(key, value);
        }

        public void Add(JsonPair pair)
        {
            Add(pair.Key, pair.Value);
        }

        public void AddRange(JsonPairEnumerable items)
        {
            if (items == null)
                throw new ArgumentNullException("items");

            foreach (KeyValuePair<string, JsonValue> pair in items)
                map.Add(pair.Key, pair.Value);
        }

        public void AddRange(params JsonPair[] items)
        {
            AddRange((JsonPairEnumerable)items);
        }

        public void Clear()
        {
            map.Clear();
        }

        bool ICollection<JsonPair>.Contains(JsonPair item)
        {
            return (map as ICollection<JsonPair>).Contains(item);
        }

        bool ICollection<JsonPair>.Remove(JsonPair item)
        {
            return (map as ICollection<JsonPair>).Remove(item);
        }

        public override bool ContainsKey(string key)
        {
            if (key == null)
                throw new ArgumentNullException("key");

            return map.ContainsKey(key);
        }

        public void CopyTo(JsonPair[] array, int arrayIndex)
        {
            (map as ICollection<JsonPair>).CopyTo(array, arrayIndex);
        }

        public bool Remove(string key)
        {
            if (key == null)
                throw new ArgumentNullException("key");

            return map.Remove(key);
        }

        bool ICollection<JsonPair>.IsReadOnly
        {
            get { return false; }
        }

        public override void Save(Stream stream)
        {
            if (stream == null)
                throw new ArgumentNullException("stream");
            stream.WriteByte((byte)'{');
            foreach (JsonPair pair in map)
            {
                stream.WriteByte((byte)'"');
                byte[] bytes = Encoding.UTF8.GetBytes(EscapeString(pair.Key));
                stream.Write(bytes, 0, bytes.Length);
                stream.WriteByte((byte)'"');
                stream.WriteByte((byte)',');
                stream.WriteByte((byte)' ');
                if (pair.Value == null)
                {
                    stream.WriteByte((byte)'n');
                    stream.WriteByte((byte)'u');
                    stream.WriteByte((byte)'l');
                    stream.WriteByte((byte)'l');
                }
                else
                    pair.Value.Save(stream);
            }
            stream.WriteByte((byte)'}');
        }

        public bool TryGetValue(string key, out JsonValue value)
        {
            return map.TryGetValue(key, out value);
        }


        public static JsonValue toJsonValue(object input)
        {
            if (input == null) return null;

            if (input is IList)
            {
                return toJsonArray((IList)input);
            }

            Type oT = input.GetType();
            if (oT.IsPrimitive || oT == typeof(Decimal) || oT == typeof(String))
            {
                return new JsonPrimitive(input);
            }

            JsonObject jv = new JsonObject();

            System.Reflection.FieldInfo[] propInfo = oT.GetFields(BindingFlags.Public | BindingFlags.Instance);

            foreach (System.Reflection.FieldInfo prop in propInfo)
            {
                Type t = prop.FieldType;
                object va = prop.GetValue(input);
                string fieldName = prop.Name;

                object[] attrs = prop.GetCustomAttributes(typeof(JsonMapFieldName), true);

                foreach (object jmfn in attrs)
                {
                    if (jmfn is JsonMapFieldName)
                    {
                        fieldName = (jmfn as JsonMapFieldName).name;
                        break;
                    }
                }

                if (t.IsPrimitive || t == typeof(Decimal) || t == typeof(String))
                {
                    jv.Add(fieldName, new JsonPrimitive(va));
                }
                else if (va is IList)
                {
                    jv.Add(fieldName, toJsonArray(va as IList));
                }
                else if (va is IDictionary)
                {
                    jv.Add(fieldName, toJsonDictionary(va as IDictionary));

                }
                else
                {
                    jv.Add(fieldName, toJsonValue(va));
                }
            }

            return jv;
        }

        public static JsonObject toJsonDictionary(IDictionary dict)
        {
            JsonObject jo = new JsonObject();

            foreach (string key in dict.Keys)
            {
                jo.Add(key, toJsonValue(dict[key]));
            }

            return jo;
        }

        public static JsonArray toJsonArray(IList list)
        {
            JsonArray ja = new JsonArray();
            foreach (object obj in list)
            {
                ja.Add(toJsonValue(obj));
            }

            return ja;
        }

        public static T toGraph<T>(JsonValue jv)
        {
            return (T)toObject(typeof(T), jv);
        }

        private static object toObject(Type maptype, JsonValue jv)
        {
            if (jv == null) return null;

            if (maptype.IsPrimitive || maptype == typeof(Decimal) || maptype == typeof(String))
            {
                switch (jv.JsonType)
                {
                    case JsonType.Number:
                        return (int)jv;
                    case JsonType.String:
                        return (string)jv;
                    case JsonType.Boolean:
                        return (bool)jv;
                }
            }

            if (jv.JsonType == JsonType.Array)
            {
                IList list = (IList)Activator.CreateInstance(maptype);

                foreach (JsonValue j in jv)
                {
                    list.Add(toObject(maptype.GetGenericArguments()[0], j));
                }

                return list;

            }

            if (maptype.IsGenericType && maptype.GetGenericTypeDefinition() == typeof(Dictionary<,>))
            {
                IDictionary dic = Activator.CreateInstance(maptype) as IDictionary;

                foreach (String key in ((JsonObject)jv).Keys)
                {
                    dic.Add(key, toObject(maptype.GetGenericArguments()[1], jv[key]));
                }

                return dic;

            }

            object instance = Activator.CreateInstance(maptype);

            System.Reflection.FieldInfo[] fInfo = instance.GetType().GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            foreach (System.Reflection.FieldInfo prop in fInfo)
            {
                Type t = prop.FieldType;

                if (jv.ContainsKey(prop.Name))
                {
                    JsonValue jProv = jv[prop.Name];
                    switch (jProv.JsonType)
                    {
                        case JsonType.Number:
                            prop.SetValue(instance, (int)jv[prop.Name]);
                            break;
                        case JsonType.String:
                            prop.SetValue(instance, (string)jv[prop.Name]);
                            break;
                        case JsonType.Boolean:
                            prop.SetValue(instance, (bool)jv[prop.Name]);
                            break;
                        case JsonType.Object:
                            prop.SetValue(instance, toObject(t, jv[prop.Name]));
                            break;
                        case JsonType.Array:
                            prop.SetValue(instance, toObject(t, jv[prop.Name]));
                            break;
                    }
                }
            }
            return instance;
        }
    }
}

