using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Xml.Linq;

namespace Core
{
    public class XmlToDynamic
    {
        public static void Parse(dynamic parent, XElement node)
        {
            foreach (var a in node.Attributes())
                AddProperty(parent, a.Name.ToString(), a.Value);
            if (node.HasElements)
            {
                IEnumerable<XElement> sorted = from XElement elt in node.Elements() orderby node.Elements(elt.Name.LocalName).Count() descending select elt;
                string elementName = string.Empty;
                List<dynamic> list = null;
                foreach (var element in sorted)
                {
                    var item = new ExpandoObject();
                    Parse(item, element);
                    if (element.Name.LocalName != elementName)
                    {
                        list = null;
                        AddProperty(parent, elementName = element.Name.LocalName, item);
                    }
                    else
                        if (list == null)
                        AddProperty(parent, element.Name.LocalName, list = new List<dynamic>() { (parent as IDictionary<string, object>)[element.Name.LocalName], item });
                    else
                        list.Add(item);
                }
            }
            else if (!string.IsNullOrWhiteSpace(node.Value))
                AddProperty(parent, "TextValue", node.Value.Trim());
        }

        private static void AddProperty(dynamic parent, string name, object value)
        {
            if (parent is List<dynamic>)
                (parent as List<dynamic>).Add(value);
            else
                (parent as IDictionary<string, object>)[name] = value;
        }

        public static dynamic Parse(string xmlText)
        {
            var root = new ExpandoObject();
            var xDoc = XDocument.Parse(xmlText);
            XmlToDynamic.Parse(root, xDoc.Elements().First());
            return root;
        }
    }
}
