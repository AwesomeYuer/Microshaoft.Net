namespace TestConsoleApplication
{
    using System;
    using System.Diagnostics;
    using System.Threading;
    using Newtonsoft.Json;
    using System.IO;
    using System.Xml;
    //using System.Threading.Tasks;
    using Microshaoft;
    using Microshaoft.Share;
    class Program111
    {
        static void Main(string[] args)
        {
            var xml = @"<a>asdsad</a>";
            var json = JsonHelper.XmlToJson(xml);
            Console.WriteLine(json);
            ProcessOnce();
            Console.WriteLine("End ...");
            Console.ReadLine();
        }
        static void ProcessOnce()
        {
            var party = new Party()
            {
                PartyID = "clientID"
                 ,
                PartyInstanceID = "userID"
            };
            var loginRequestMessage = new LoginRequest()
            {
                Header = new MessageHeader()
                {
                    From = party
                    ,
                    To = new Party[] 
					{
						party
						, new Party()
						{
							 PartyID = "1111111111"
							 ,
							 PartyInstanceID = "1111111-111111111"
						}
					}
                    ,
                    RequireResponse = 1
                    ,
                    SendTimeStamp = DateTime.Now
                    ,
                    Topic = "LoginRequest"
                    ,
                    Count = 1
                    ,
                    ID = Guid.NewGuid().ToString("N")
                    ,
                    LinkID = null
                    ,
                    Result = null
                }
                ,
                Body = new LoginRequestBody()
                {
                    Password = "password"
                }
            };
            Console.WriteLine("Json 序列化");
            var json = JsonHelper.Serialize(loginRequestMessage);
            Console.WriteLine(json);
            Console.WriteLine("Json 反序列化");
            var path = "H";
            var messageHeader = JsonHelper.DeserializeByJTokenPath<MessageHeader>(json, path);
            Console.WriteLine("JTokenPath: {0}, MessageHeader.Topic::Value: {1}", path, messageHeader.Topic);
            path = "B";
            var loginRequestBody = JsonHelper.DeserializeByJTokenPath<LoginRequestBody>(json, path);
            Console.WriteLine("JTokenPath: {0}, LoginRequestBody.Password::Value: {1}", path, loginRequestBody.Password);
            path = "H.To[1]";
            var party1 = JsonHelper.DeserializeByJTokenPath<Party>(json, path);
            Console.WriteLine("JTokenPath: {0}, Party.PartyID::Value: {1}", path, party1.PartyID);
            path = "";
            var loginRequest = JsonHelper.DeserializeByJTokenPath<LoginRequest>(json, path);
            Console.WriteLine("JTokenPath: {0}, LoginRequest.Header.ID::Value: {1}", path, loginRequest.Header.ID);
            Console.WriteLine("JTokenPath: {0}, LoginRequest.Body.Password::Value: {1}", path, loginRequest.Body.Password);
            var xml = JsonHelper.JsonToXml(json);
            Console.WriteLine(xml);
            json = JsonHelper.XmlToJson(xml);
            Console.WriteLine(json);
            //Console.ReadLine();
        }
    }
}
namespace Microshaoft.Share
{
    using Newtonsoft.Json;
    public class LoginRequest : IMessage
    {
        [JsonProperty("H")]
        public MessageHeader Header { get; set; }
        [JsonProperty("B")]
        public LoginRequestBody Body;
    }
    public class LoginRequestBody
    {
        [JsonProperty("P")]
        public string Password;
    }
    // CommonResponse
}
namespace Microshaoft.Share
{
    using Newtonsoft.Json;
    using System;
    public interface IMessage
    {
        MessageHeader Header { get; }
    }
    public class MessageHeader
    {
        // 消息主题
        [JsonProperty(PropertyName = "T")]
        public string Topic;
        // 消息号
        [JsonProperty(PropertyName = "I")]
        public string ID;
        // 关联消息号
        [JsonProperty(PropertyName = "L")]
        public string LinkID;
        // Require Response
        [JsonProperty(PropertyName = "R")]
        public int RequireResponse;
        // 发送方
        [JsonProperty(PropertyName = "F")]
        public Party From;
        // 接收方
        public Party[] To;
        // 发送时间戳
        [JsonProperty(PropertyName = "S")]
        public DateTime? SendTimeStamp;
        // 次数
        [JsonProperty(PropertyName = "C")]
        public int Count;
        // Result
        [JsonProperty(PropertyName = "V")]
        public int? Result;
    }
    public class Party
    {
        //PartyID
        [JsonProperty(PropertyName = "P")]
        public string PartyID;
        //PartyInstanceID
        [JsonProperty(PropertyName = "I")]
        public string PartyInstanceID;
    }
}
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
            // var s = string.Format("{{0}}", json);
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
            JToken jToken = jObject.SelectToken(jTokenPath);
            using (var jsonReader = jToken.CreateReader())
            {
                Console.WriteLine(Environment.StackTrace);
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
