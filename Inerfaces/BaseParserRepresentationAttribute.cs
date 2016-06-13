using System.Linq;

namespace Core
{
    public interface IRepresentationAttribute
    {
        string Name { get; }
        string[] Extensions { get; }
        string Filter { get; }
    }

    [System.AttributeUsage(System.AttributeTargets.Class)]
    public class BaseParserRepresentationAttribute : System.Attribute, IRepresentationAttribute
    {
        public string Name { get; protected set; }
        public string[] Extensions { get; protected set; }
        public string Filter => $"{Name}|{string.Join(";", Extensions.Select(e => $"*.{e}").ToArray() )}";

        public BaseParserRepresentationAttribute(string name, params string[] extensions)
        {
            Name = name;
            Extensions = extensions;
        }
    }
}
