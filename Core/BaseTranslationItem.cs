using System.Xml.Serialization;

namespace Core
{
    public class BaseTranslationItem : ITranslationItem
    {
        [XmlAttribute]
        public string Value { get; set; }
        [XmlAttribute]
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
