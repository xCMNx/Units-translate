using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Serialization;
using Core;

namespace Linguist
{
    public class EntryesComparer : Comparer<Entry>
    {
        public override int Compare(Entry e1, Entry e2) => string.Compare(e1.Eng, e2.Eng, true);

        public static readonly EntryesComparer Comparer = new EntryesComparer();
    }

    [Serializable]
    public class Field
    {
        [XmlAttribute]
        public string attrname;
        [XmlAttribute]
        public string fieldtype;
        [XmlAttribute]
        public string SUBTYPE;
        [XmlAttribute]
        public string required;
        [XmlAttribute]
        public string WIDTH;
        [XmlAnyAttribute]
        public XmlAttribute[] XAttributes;
    }

    [Serializable]
    public class Params
    {
        [XmlAttribute]
        public int AUTOINCVALUE = 1;
        [XmlAnyAttribute]
        public XmlAttribute[] XAttributes;
    }

    [Serializable]
    public class Metadata
    {
        [XmlArray("FIELDS")]
        [XmlArrayItem("FIELD", Type = typeof(Field))]
        public Field[] FIELDS = new Field[] {
            new Field() { attrname = "id", fieldtype = "i4", SUBTYPE = "Autoinc" }
            ,new Field() { attrname = "Phrase1", fieldtype = "string", required = "true", WIDTH = "512" }
            ,new Field() { attrname = "Phrase2", fieldtype = "string", WIDTH = "512" }
        };
        [XmlElement("PARAMS")]
        public Params PARAMS = new Params();
    }

    [Serializable]
    public class DATAPACKET
    {
        [XmlAttribute("Version")]
        public string Version = "2.0";
        [XmlElement("METADATA")]
        public Metadata Metadata = new Metadata();
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
    public class Entry : ITranslationItem
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

        [XmlIgnore]
        public string Value => _Eng;
        [XmlIgnore]
        public string Translation => _Trans;

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
        public Linguist(IEnumerable<Entry> entries)
        {
            Entryes.AddRange(entries.OrderBy(e => e.Value, StringComparer.InvariantCulture));
        }
        public Linguist(IEnumerable<ITranslationItem> items)
            : this(items.Select(i => new Entry(i.Value, i.Translation)))
        {
        }
    }
}
