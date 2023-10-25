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
    class DataRuleOffice : DataRule
    {
        public override bool? TestRule(string path, byte[] contents, ref List<string> snippets)
        {
            var extension = Path.GetExtension(path);

            if (!string.IsNullOrEmpty(extension))
            {
                switch (extension.ToLower())
                {
                    case ".docx": // Word document
                    case ".docm": // Word macro-enabled document; same as docx, but may contain macros and scripts
                    case ".dotx": // Word template
                    case ".dotm": // Word macro-enabled template; same as dotx, but may contain macros and scripts
                        return base.TestRuleString(path, WordToPlaintext(contents), ref snippets);

                    case ".docb": // Word binary document introduced in Microsoft Office 2007
                    case ".wll": // Word add-in
                    case ".wwl": // Word add-in
                        break; // Haven't analyzed these formats yet (should be identical to above / ooxml)
                         
                    case ".doc": // Legacy Word document; Microsoft Office refers to them as "Microsoft Word 97 – 2003 Document"
                    case ".dot": // Legacy Word templates; officially designated "Microsoft Word 97 – 2003 Template"
                    case ".wbk": // Legacy Word document backup; referred as "Microsoft Word Backup Document"
                        break; // Haven't analyzed these formats yet (should be legacy formats)

                    case ".xlsx": // Excel workbook
                    case ".xlsm": // Excel macro-enabled workbook; same as xlsx but may contain macros and scripts
                    case ".xltx": // Excel template
                    case ".xltm": // Excel macro-enabled template; same as xltx but may contain macros and scripts
                        return base.TestRuleString(path, ExcelToPlaintext(contents), ref snippets);

                    case ".xls": // Legacy Excel worksheets; officially designated "Microsoft Excel 97-2003 Worksheet"
                    case ".xlt": // Legacy Excel templates; officially designated "Microsoft Excel 97-2003 Template"
                    case ".xlm": // Legacy Excel macro
                        break; // Haven't analyzed these formats yet (should be legacy formats)

                    case ".xlsb": // Excel binary worksheet (BIFF12)
                    case ".xla": // Excel add-in that can contain macros
                    case ".xlam": // Excel macro-enabled add-in
                    case ".xll": // Excel XLL add-in; a form of DLL-based add-in[1]
                    case ".xlw": // Excel work space; previously known as "workbook"
                        break; // Haven't analyzed these formats yet (other formats)

                    case ".pptx": // PowerPoint presentation
                    case ".pptm": // PowerPoint macro-enabled presentation
                    case ".potx": // PowerPoint template
                    case ".potm": // PowerPoint macro-enabled template
                    case ".ppam": // PowerPoint add-in
                    case ".ppsx": // PowerPoint slideshow
                    case ".ppsm": // PowerPoint macro-enabled slideshow
                    case ".sldx": // PowerPoint slide
                    case ".sldm": // PowerPoint macro-enabled slide
                    case ".pa": // PowerPoint add-in
                        return false; // Not coded yet (should be ooxml)

                    case ".ppt": // Legacy PowerPoint presentation
                    case ".pot": // Legacy PowerPoint template
                    case ".pps": // Legacy PowerPoint slideshow
                    case ".ppa": // PowerPoint (2007?) add-in
                        break; // Haven't analyzed these formats yet (should be legacy formats)

                    case ".one": // a OneNote export file
                        return false; // Not coded yet (?)

                    case ".pub": // a Microsoft Publisher publication
                        return false; // Not coded yet (?)

                    case ".xps": // a XML-based document format used for printing (on Windows Vista and later) and preserving documents.
                        return false; // Not coded yet (?)

                        /* Email files (Outlook) */
                    case ".msg":
                    case ".pst":
                    case ".edb":
                    case ".ost":
                    case ".eml":
                        return false; // Not coded yet (?)

                    default:
                        break;
                }
            }

            return null;
        }

        public override string ToString()
        {
            return "data:office";
        }

        private string WordToPlaintext(byte[] contents)
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

        private string ExcelToPlaintext(byte[] contents)
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
