using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Packaging;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace Smaug.RulesData.File
{
    class DataRuleOfficeExcel : DataRule
    {
        public override bool? TestRule(string path, byte[] contents, ref List<string> snippets)
        {
            var temp = Path.GetExtension(path);

            if (temp != null && temp.ToLower() == ".xlsx")
                return base.TestRuleString(path, XlsxToPlaintext(contents), ref snippets);

            return null;
        }

        public override string ToString()
        {
            return "excel";
        }

        private string XlsxToPlaintext(byte[] contents)
        {
            var sb = new StringBuilder();

            using (var content_stream = new MemoryStream(contents))
            {
                using (var package = Package.Open(content_stream, FileMode.Open, FileAccess.Read))
                {
                    var string_part = package.GetPart(new Uri("/xl/sharedStrings.xml", UriKind.Relative));
                    var string_data = XDocument.Load(XmlReader.Create(string_part.GetStream()));

                    if (string_data != null)
                    {
                        XNamespace w = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";

                        foreach (var str in string_data.Root.Elements(w + "si"))
                        {
                            sb.AppendLine(str
                                .Elements(w + "t")
                                .Aggregate(new StringBuilder(), (s, e) => s.Append(e.Value), s => s.ToString()));
                        }
                    }
                }
            }

            return sb.ToString();
        }
    }
}
