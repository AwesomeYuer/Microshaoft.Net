namespace Microshaoft
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Xml.Xsl;
    using System.Xml;
    using System.IO;
    public static class XslCompiledTransformHelper
    {
        public static string Transform(string xsl, string xml)
        {
            XslCompiledTransform xslt = new XslCompiledTransform();
            StringReader stringReader = new StringReader(xsl);
            XmlReader xmlReader = XmlReader.Create(stringReader);
            xslt.Load(xmlReader);
            stringReader = new StringReader(xml);
            xmlReader = XmlReader.Create(stringReader);
            XmlWriterSettings xmlWriterSettings = new XmlWriterSettings();
            //xmlWriterSettings.Encoding = Encoding.UTF8;
            xmlWriterSettings.Indent = true;
            xmlWriterSettings.OmitXmlDeclaration = true;
            Stream stream = new MemoryStream();
            XmlWriter xmlWriter = XmlWriter.Create(stream, xmlWriterSettings);
            xslt.Transform
                        (
                            xmlReader
                            , xmlWriter
                        );
            byte[] buffer = StreamDataHelper.ReadDataToBytes(stream);
            Encoding e = EncodingHelper.IdentifyEncoding(stream, Encoding.Default);
            int offset = e.GetPreamble().Length;
            string s = e.GetString(buffer, offset, buffer.Length - offset);

            return s;
        }


    }




}


