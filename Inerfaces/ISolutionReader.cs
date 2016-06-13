using System.Collections.Generic;

namespace Core
{
    /// <summary>
    /// Атрибут для парсеров решений, указывающий какие расширения решений парсер умеет
    /// </summary>
    public class MapperSolutionFilter : BaseParserRepresentationAttribute
    {
        public MapperSolutionFilter(string name, params string[] extensions) : base(name, extensions)
        {
        }
    }

    public interface ISolutionReader
    {
        ICollection<string> GetFiles(string solutionFileName);
    }
}
