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


            JsonHelper.ReadJsonPathsValuesAsStrings
                            (
                                json
                                , new string[] { "H" }
                                , (x, y) =>
                                    {
                                        Console.WriteLine(x);
                                        return false;

                                    }
                            );



            var xml = JsonHelper.JsonToXml(json);
            Console.WriteLine(xml);
            json = JsonHelper.XmlToJson(xml);
            Console.WriteLine(json);
            Console.ReadLine();
        }
    }
}
