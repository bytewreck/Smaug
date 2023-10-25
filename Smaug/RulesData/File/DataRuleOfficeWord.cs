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
    class DataRuleOfficeWord : DataRule
    {
        public override bool? TestRule(string path, byte[] contents, ref List<string> snippets)
        {
            var temp = Path.GetExtension(path);

            if (temp != null && temp.ToLower() == ".docx")
                return base.TestRuleString(path, DocxToPlaintext(contents), ref snippets);

            return null;
        }

        public override string ToString()
        {
            return "word";
        }

        private string DocxToPlaintext(byte[] contents)
        {
            var sb = new StringBuilder();

            using (var content_stream = new MemoryStream(contents))
            {
                using (var package = Package.Open(content_stream, FileMode.Open, FileAccess.Read))
                {
                    var document_relationship = package.GetRelationshipsByType("http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument").FirstOrDefault();

                    if (document_relationship != null)
                    {
                        var document_part = package.GetPart(PackUriHelper.ResolvePartUri(new Uri("/", UriKind.Relative), document_relationship.TargetUri));
                        var document_data = XDocument.Load(XmlReader.Create(document_part.GetStream()));

                        if (document_data != null)
                        {
                            XNamespace w = "http://schemas.openxmlformats.org/wordprocessingml/2006/main";

                            var paragraphs = document_data.Root
                                .Element(w + "body")
                                .Descendants(w + "p");

                            foreach (var paragraph in paragraphs)
                            {
                                sb.AppendLine(paragraph
                                    .Elements(w + "r")
                                    .Elements(w + "t")
                                    .Aggregate(new StringBuilder(), (s, e) => s.Append(e.Value), s => s.ToString()));
                            }
                        }
                    }
                }
            }

            return sb.ToString();
        }
    }
}
