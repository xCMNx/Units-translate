using System;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Serialization;

namespace Core
{
    [Serializable]
    public class Field
    {
        [XmlAnyAttribute]
        public XmlAttribute[] XAttributes;
    }

    [Serializable]
    public class Params
    {
        [XmlAnyAttribute]
        public XmlAttribute[] XAttributes;
    }

    [Serializable]
    public class Param
    {
        [XmlAttribute("AUTOINCVALUE")]
        public string AUTOINCVALUE;
    }
   
    [Serializable]
    public class Metadata
    {
        [XmlArray("FIELDS")]
        [XmlArrayItem("FIELD", Type = typeof(Field))]
        public Field[] FIELDS;
        [XmlElement("PARAMS", Type = typeof(Param))]
        public Param PARAMS;
    }

    [Serializable]
    public class DATAPACKET
    {
        [XmlAttribute("Version")]
        public string Version = "2.0";
        [XmlElement("METADATA")]
        public Metadata Metadata;
        [XmlElement("ROWDATA")]
        public string ROWDATA = string.Empty;
    }

    [Serializable]
    public class Language
    {
        [XmlAttribute("Id")]
        public int Id = 1049;
        [XmlAttribute("charset")]
        public string Charset = "RUSSIAN_CHARSET";
    }

    [Serializable]
    public class Entry
    {
        string _Eng;
        string _Trans;
        [XmlAttribute("eng")]
        public string Eng
        {
            get { return _Eng; }
            set { _Eng = value; }
        }
        [XmlAttribute("trans")]
        public string Trans
        {
            get { return _Trans; }
            set { _Trans = value; }
        }
        public Entry(string eng, string trans)
        {
            _Eng = eng;
            _Trans = trans;
        }
        public Entry() { }
    }

    [Serializable]
    public class Linguist
    {
        [XmlElement("DATAPACKET")]
        public DATAPACKET Datapacket = new DATAPACKET();
        [XmlElement("language")]
        public Language Language = new Language();
        [XmlArray("data")]
        [XmlArrayItem("entry", Type = typeof(Entry))]
        public List<Entry> Entryes = new List<Entry>();

        public Linguist()
        {
        }
    }
}
