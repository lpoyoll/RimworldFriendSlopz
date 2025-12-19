using System.IO;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Shared.Misc
{
    public static class XmlHelper
    {
        public static string PrettyXml(string xml) { return XDocument.Parse(xml).ToString(); }

        public static void WriteXmlToFile(string xml, string path, bool shouldPretty)
        {
            if (shouldPretty) xml = PrettyXml(xml);
            File.WriteAllText(path, xml);
        }

        public static string ReadXmlFromFile(string path)
        {
            string xml = File.ReadAllText(path);
            xml = new Regex(@">\s*<").Replace(xml, "><");
            return xml;
        }
    }
}