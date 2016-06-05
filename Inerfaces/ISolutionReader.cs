using System.Collections.Generic;

namespace Core
{
    /// <summary>
    /// Атрибут для парсеров, указывающий какие расширения решений парсер умеет
    /// </summary>
    [System.AttributeUsage(System.AttributeTargets.Class)]
    public class MapperSolutionFilter : System.Attribute
    {
        public string[] Extensions;
        public MapperSolutionFilter(string[] extensions)
        {
            Extensions = extensions;
        }
    }

    public interface ISolutionReader
    {
        ICollection<string> GetFiles(string solutionFileName);
    }
}
