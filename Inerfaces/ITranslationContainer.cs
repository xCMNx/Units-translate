using System.Collections.Generic;
using System.Text;

namespace Core
{
    /// <summary>
    /// Атрибут для контейнеров, указывающий какие расширения контейнер умеет
    /// </summary>
    public class ContainerFilter : BaseParserRepresentationAttribute
    {
        public ContainerFilter(string name, params string[] extensions) : base(name, extensions)
        {
        }
    }

    public interface ITranslationContainer
    {
        ICollection<ITranslationItem> Load(string filePath, Encoding encoding);
        bool Save(string filePath, Encoding encoding, ICollection<ITranslationItem> items);
    }
}
