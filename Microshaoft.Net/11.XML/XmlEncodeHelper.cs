namespace Test
{
    using System;
    using System.IO;
    using System.Text;
    using System.Xml;
    using System.Xml.Serialization;
    using Microshaoft;
    public class Class111
    {
        static void Main(string[] args)
        {
            string text = "`~!@#$%&*()_+-=\t{}|[]\\://\";'<>?,./\r\n ";
            text += "～！＃￥％…＆×（）——＋·－＝｛｝｜【】＼：“”；‘’＇＜＞？，．／　";
            Console.WriteLine("orginal text: {0}", text);
            string s = XmlEncodeHelper.XmlAttributeValueEncode(text);
            Console.WriteLine("XmlAttributeValueEncode: {0}", s);
            string ss = XmlEncodeHelper.XmlAttributeValueDecode(s);
            Console.WriteLine("XmlAttributeValueDecode: {0}", ss);
            Console.WriteLine("{0}", (ss == text ? true : false));
            s = XmlEncodeHelper.XmlElementValueEncode(text);
            Console.WriteLine("XmlElementValueEncode: {0}", s);
            ss = XmlEncodeHelper.XmlElementValueDecode(s);
            Console.WriteLine("XmlElementValueDecode: {0}", ss);
            Console.WriteLine("{0}", (ss == text ? true : false));
        }
    }
}
namespace Microshaoft
{
    using System;
    using System.IO;
    using System.Text;
    using System.Xml;
    using System.Xml.Serialization;
    public static class XmlEncodeHelper
    {
        public static string XmlAttributeValueEncode
                        (
                            string text
                        )
        {
            XmlDocument document = GenerateXmlDocument(text);
            XmlNode node = document.DocumentElement.SelectSingleNode("Element");
            text = node.Attributes["EncodedAttributeValue"].InnerXml;
            return text;
        }
        public static string XmlElementValueEncode
                        (
                            string text
                        )
        {
            XmlDocument document = GenerateXmlDocument(text);
            XmlNode node = document.DocumentElement.SelectSingleNode("Element/EncodedElementValue");
            text = node.InnerXml;
            return text;
        }
        public static string XmlAttributeValueDecode(string xml)
        {
            XmlDocument document = GenerateXmlDocument("");
            XmlNode node = document.DocumentElement.SelectSingleNode("Element");
            node.Attributes["EncodedAttributeValue"].InnerXml = xml;
            string s = node.Attributes["EncodedAttributeValue"].InnerText;
            return s;
        }
        public static string XmlElementValueDecode(string xml)
        {
            XmlDocument document = GenerateXmlDocument("");
            XmlNode node = document.DocumentElement.SelectSingleNode("Element/EncodedElementValue");
            node.InnerXml = xml;
            string s = node.InnerText;
            return s;
        }

        private static XmlDocument GenerateXmlDocument(string text)
        {
            EncodedElement y = new EncodedElement();
            y.EncodedAttributeValue = text;
            y.EncodedElementValue = text;
            XmlEncoder x = new XmlEncoder();
            x.Element = y;
            Encoding e = Encoding.UTF8;
            MemoryStream stream = new MemoryStream();
            XmlTextWriter writer = new XmlTextWriter(stream, e);
            XmlSerializer serializer = new XmlSerializer(x.GetType());
            string xml = SerializerHelper.XmlSerializerObjectToXml<XmlEncoder>
                                                    (
                                                        x
                                                        , writer
                                                        , serializer
                                                    );
            XmlDocument document = new XmlDocument();
            document.LoadXml(xml);
            return document;
        }
    }
    [XmlRoot("XmlEncoder")]
    [Serializable]
    public class XmlEncoder
    {
        [XmlElement("Element")]
        public EncodedElement Element;
    }
    [Serializable]
    public class EncodedElement
    {
        [XmlAttribute("EncodedAttributeValue")]
        public string EncodedAttributeValue;
        [XmlElement("EncodedElementValue")]
        public string EncodedElementValue;
    }
}






