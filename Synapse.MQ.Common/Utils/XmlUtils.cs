using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.IO;
using System.Xml.Serialization;

namespace Synapse.MQ
{
    class XmlUtils
    {
        public static string Serialize<T>(object data, bool indented = true, string filePath = null, bool omitXmlDeclaration = true, bool omitXmlNamespace = true)
        {
            XmlWriterSettings settings = new XmlWriterSettings();
            settings.OmitXmlDeclaration = omitXmlDeclaration;
            settings.ConformanceLevel = ConformanceLevel.Auto;
            settings.CloseOutput = true;
            settings.Encoding = Encoding.Unicode;
            settings.Indent = indented;

            MemoryStream ms = new MemoryStream();
            XmlSerializer s = new XmlSerializer(typeof(T));
            XmlWriter w = XmlWriter.Create(ms, settings);
            if (omitXmlNamespace)
            {
                XmlSerializerNamespaces ns = new XmlSerializerNamespaces();
                ns.Add("", "");
                s.Serialize(w, data, ns);
            }
            else
            {
                s.Serialize(w, data);
            }
            string result = Encoding.Unicode.GetString(ms.GetBuffer(), 0, (int)ms.Length);
            w.Close();

            if (!string.IsNullOrWhiteSpace(filePath))
            {
                using (StreamWriter file = new StreamWriter(filePath, false))
                {
                    file.Write(result);
                }
            }

            return result;
        }

        public static T Deserialize<T>(string str)
        {
            XmlSerializer s = new XmlSerializer(typeof(T));
            if (str[0] == '<')
                return (T)s.Deserialize(new StringReader(str));
            else
                return (T)s.Deserialize(new StringReader(str.Substring(1)));

        }


    }
}
