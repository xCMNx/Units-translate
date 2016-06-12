using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using Core;

namespace Linguist
{
    [ContainerFilter("Linguist", "*.xml")]
    public class LinguistContaier : ITranslationContainer
    {
        static XmlSerializerNamespaces namespaces = new XmlSerializerNamespaces();
        static XmlWriterSettings WriterSettings = new XmlWriterSettings { Indent = true, IndentChars = "\t", NewLineHandling = NewLineHandling.None };
        static XmlSerializer serializer = new XmlSerializer(typeof(Linguist));

        //static Encoding DefaultEncoding = Encoding.GetEncoding("windows-1251");
        //public Encoding _Encoding = DefaultEncoding;
        //public Encoding Encoding => _Encoding;

        public ICollection<ITranslationItem> Load(string filePath, Encoding encoding)
        {
            using (var txtreader = new FileStream(filePath, FileMode.Open))
            {
                using (var xmlreader = new XmlTextReader(txtreader))
                {
                    xmlreader.MoveToContent();
                    //_Encoding = xmlreader.Encoding;
                    return (serializer.Deserialize(xmlreader) as Linguist).Entryes.OfType<ITranslationItem>().ToArray();
                }
            }
        }

        public bool Save(string filePath, Encoding encoding, ICollection<ITranslationItem> items)
        {
            using (StreamWriter sw = new StreamWriter(filePath, false, encoding))
            using (var xw = XmlWriter.Create(sw, WriterSettings))
            {
                xw.WriteStartDocument(true);
                //xw.WriteRaw(string.Format("\r\n<?xml-stylesheet type=\"text/xsl\" href=\"{0}.xsl\"?>\r\n", Path.GetFileNameWithoutExtension(path)));
                xw.WriteRaw("\r\n<?xml-stylesheet type=\"text/xsl\" href=\"eng_rus.xsl\"?>\r\n");
                serializer.Serialize(xw, new Linguist(items), namespaces);
            }
            return true;
        }

        static LinguistContaier()
        {
            namespaces.Add(string.Empty, string.Empty);
        }
    }
}
