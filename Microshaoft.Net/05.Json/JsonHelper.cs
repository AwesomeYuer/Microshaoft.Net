namespace Microshaoft
{
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using System;
    using System.IO;
    using System.Xml;
    public static class JsonHelper
    {
        public static string XmlToJson(string xml, bool keyQuoteName = false)
        {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml(xml);
            string json = string.Empty;
            using (var stringWriter = new StringWriter())
            {
                using (var jsonTextWriter = new JsonTextWriter(stringWriter))
                {
                    jsonTextWriter.QuoteName = keyQuoteName;
                    JsonSerializer jsonSerializer = new JsonSerializer();
                    jsonSerializer.Serialize(jsonTextWriter, xmlDocument);
                    json = stringWriter.ToString();
                }
            }
            return json;
        }
        public static string JsonToXml(string json, string deserializeRootElementName = "root", bool includeRoot = true)
        {
            var xmlDocument = JsonConvert.DeserializeXmlNode(json, deserializeRootElementName);
            var xml = string.Empty;
            XmlNode xmlNode = null;
            if (!includeRoot)
            {
                xmlNode = xmlDocument.SelectSingleNode(deserializeRootElementName);
            }
            else
            {
                xmlNode = xmlDocument;
            }
            xml = xmlNode.InnerXml;
            return xml;
        }
        public static T DeserializeByJTokenPath<T>
            (
                string json
                , string jTokenPath = null //string.Empty
            )
        {
            JObject jObject = JObject.Parse(json);
            JsonSerializer jsonSerializer = new JsonSerializer();
            if (string.IsNullOrEmpty(jTokenPath))
            {
                jTokenPath = string.Empty;
            }
            JToken jToken = jObject.SelectToken(jTokenPath);
            using (var jsonReader = jToken.CreateReader())
            {
                return jsonSerializer.Deserialize<T>(jsonReader);
            }
        }
        public static string Serialize(object target, bool keyQuoteName = false)
        {
            string json = string.Empty;
            using (StringWriter stringWriter = new StringWriter())
            {
                using (JsonTextWriter jsonTextWriter = new JsonTextWriter(stringWriter))
                {
                    jsonTextWriter.QuoteName = keyQuoteName;
                    JsonSerializer jsonSerializer = new JsonSerializer();
                    jsonSerializer.Serialize(jsonTextWriter, target);
                    json = stringWriter.ToString();
                }
            }
            return json;
        }
        public static void ReadJsonPathsValuesAsStrings
                            (
                                string json
                                , string[] jsonPaths
                                , Func<string, string, bool> onReadedOncePathStringValueProcesssFunc = null
                            )
        {
            using (var stringReader = new StringReader(json))
            {
                using (var jsonReader = new JsonTextReader(stringReader))
                {
                    bool breakAndReturn = false;
                    while
                        (
                            jsonReader.Read()
                            && !breakAndReturn
                        )
                    {
                        foreach (var x in jsonPaths)
                        {
                            if (x == jsonReader.Path)
                            {
                                if (onReadedOncePathStringValueProcesssFunc != null)
                                {
                                    var s = jsonReader.ReadAsString();
                                    breakAndReturn = onReadedOncePathStringValueProcesssFunc
                                            (
                                                x
                                                , s
                                            );
                                    if (breakAndReturn)
                                    {
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
