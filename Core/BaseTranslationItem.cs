using System.Xml.Serialization;

namespace Core
{
    [XmlType("I")]
    public class BaseTranslationItem : ITranslationItem
    {
        [XmlAttribute("V")]
        public string Value { get; set; }
        [XmlAttribute("T")]
        public string Translation { get; set; }

        public BaseTranslationItem()
        {
        }
        public BaseTranslationItem(string value, string translation)
        {
            Translation = translation;
            Value = value;
        }
    }
}
