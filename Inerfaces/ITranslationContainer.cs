using System.Collections.Generic;
using System.Text;

namespace Core
{
    /// <summary>
    /// Атрибут для контейнеров, указывающий какие расширения контейнер умеет
    /// </summary>
    [System.AttributeUsage(System.AttributeTargets.Class)]
    public class ContainerFilter : System.Attribute
    {
        public string[] Extensions;
        public string Name;
        public ContainerFilter(string name, params string[] extensions)
        {
            Extensions = extensions;
            Name = name;
        }
    }

    public interface ITranslationContainer
    {
        ICollection<ITranslationItem> Load(string filePath, Encoding encoding);
        bool Save(string filePath, Encoding encoding, ICollection<ITranslationItem> items);
    }
}
