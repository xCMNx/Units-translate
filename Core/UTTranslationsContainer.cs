using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml.Serialization;

namespace Core
{
    [ContainerFilter("Units translate", "utt")]
    public class UTTranslationsContainer : ITranslationContainer
    {
        static XmlSerializer serializer = new XmlSerializer(typeof(BaseTranslationItem[]), new XmlRootAttribute("Items"));

        public ICollection<ITranslationItem> Load(string filePath, Encoding encoding)
        {
            using (var txtreader = new FileStream(filePath, FileMode.Open))
                return serializer.Deserialize(txtreader) as BaseTranslationItem[];
        }

        public bool Save(string filePath, Encoding encoding, ICollection<ITranslationItem> items)
        {
            using (StreamWriter sw = new StreamWriter(filePath, false, encoding))
                serializer.Serialize(sw, items);
            return true;
        }
    }
}
