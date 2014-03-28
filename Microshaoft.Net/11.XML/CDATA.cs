namespace Microshaoft
{
    using System;
    using System.Xml;
    using System.Xml.Schema;
    using System.Xml.Serialization;
    public class CDATA : IXmlSerializable
    {
        private string _text;
        public CDATA()
        {
        }
        public CDATA(string text)
        {
            this._text = text;
        }
        public string Text
        {
            get
            {
                return _text;
            }
        }
        XmlSchema IXmlSerializable.GetSchema()
        {
            return null;
        }
        void IXmlSerializable.ReadXml(XmlReader reader)
        {
            string s = reader.ReadInnerXml();
            string startTag = "<![CDATA[";
            string endTag = "]]>";
            s = s.Trim(new char[] { '\r', '\n', '\t', ' ' });
            if (s.StartsWith(startTag) && s.EndsWith(endTag))
            {
                s = s.Substring(startTag.Length, s.LastIndexOf(endTag) - startTag.Length);
            }
            this._text = s;
        }
        void IXmlSerializable.WriteXml(XmlWriter writer)
        {
            writer.WriteCData(this._text);
        }
    }
}