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
           
            Console.WriteLine("End ...");
            Console.ReadLine();
        }
       
    }
}
namespace Microshaoft.Share
{
    using System;
    using Newtonsoft.Json;
    public interface IMessage
    {
        MessageHeader Header { get; set; }
        Func<string, IMessage> GetInstanceCreatorByJson();
        Func<IMessage> InstanceGetter { get; }
    }
    public class MessageHeader
    {
        // 消息主题
        [JsonProperty(PropertyName = "T")]
        public string Topic;
        // 原始请求主题
        [JsonProperty(PropertyName = "RT")]
        public string RequestTopic;

        [JsonProperty(PropertyName = "SEQ")]
        public long? Sequence;

        [JsonProperty(PropertyName = "RSEQ")]
        public long? RequestSequence;

        // 消息号
        [JsonProperty(PropertyName = "I")]
        public long? ID;


        // 关联消息号
        [JsonProperty(PropertyName = "L")]
        public long? LinkID;
        // Require Response
        [JsonProperty(PropertyName = "RR")]
        public int? RequireResponse;
        // 发送方
        [JsonProperty(PropertyName = "S")]
        public Party Sender;
        // 接收方
        [JsonProperty(PropertyName = "R")]
        public Party[] Receivers;
        // 发送时间戳
        [JsonProperty(PropertyName = "ST")]
        public DateTime? SendTimeStamp;
        // 过期时间戳
        [JsonProperty(PropertyName = "ET")]
        public DateTime? ExpireTime;
        // 次数
        [JsonProperty(PropertyName = "SC")]
        public int? SendCount;
        // Result
        [JsonProperty(PropertyName = "RV")]
        public int? ResultValue;

    }
    public class Party
    {
        //AppID
        [JsonProperty(PropertyName = "A")]
        public string AppID = "*";
        //部门机构ID
        [JsonProperty(PropertyName = "G")]
        public string GroupID = "*";
        //UserID
        [JsonProperty(PropertyName = "U")]
        public string UserID = "*";
        [JsonIgnore]
        public string ID
        {
            get
            {
                return
                    string.Format
                            (
                                "{1}{0}{2}{0}{3}"
                                , "-"
                                , AppID.ToLower().Trim()
                                , GroupID.ToLower().Trim()
                                , UserID.ToLower().Trim()
                            )
                            .ToLower()
                                .Trim();
            }
        }
    }
}