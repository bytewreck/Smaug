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
                    /* Microsoft Word */
                    case ".docx": // Word document
                    case ".docm": // Word macro-enabled document
                    case ".dotx": // Word template
                    case ".dotm": // Word macro-enabled template
                        return base.TestRuleString(path, WordXmlToPlaintext(contents), ref snippets);

                    case ".docb": // Word binary document
                        break; // Not yet analyzed

                    case ".doc": // Legacy Word document; Microsoft Office refers to them as "Microsoft Word 97 – 2003 Document"
                    case ".dot": // Legacy Word templates; officially designated "Microsoft Word 97 – 2003 Template"
                        return base.TestRule(path, contents, ref snippets); // Strings appear uniformly distributed - less inclusive parsing may not be necessary

                    case ".wbk": // Legacy Word document backup; referred as "Microsoft Word Backup Document"
                    case ".wll": // Word add-in
                    case ".wwl": // Word add-in
                        return false;

                    /* Microsoft Excel */
                    case ".xlsx": // Excel workbook
                    case ".xlsm": // Excel macro-enabled workbook
                    case ".xltx": // Excel template
                    case ".xltm": // Excel macro-enabled template
                    case ".xlam": // Excel macro-enabled add-in
                        return base.TestRuleString(path, ExcelXmlToPlaintext(contents), ref snippets);

                    case ".xlsb": // Excel binary worksheet (BIFF12)
                        return base.TestRuleString(path, ExcelBiff12ToPlaintext(contents), ref snippets);

                    case ".xls": // Legacy Excel worksheets; officially designated "Microsoft Excel 97-2003 Worksheet"
                    case ".xlt": // Legacy Excel templates; officially designated "Microsoft Excel 97-2003 Template"
                    case ".xlm": // Legacy Excel macro
                    case ".xla": // Excel add-in that can contain macros
                        return base.TestRuleString(path, ExcelBiff8ToPlaintext(contents), ref snippets);

                    case ".xll": // Excel add-in
                    case ".xlw": // Excel work space; previously known as "workbook"
                        return false;

                    /* Microsoft PowerPoint */
                    case ".pptx": // PowerPoint presentation
                    case ".pptm": // PowerPoint macro-enabled presentation
                    case ".ppsx": // PowerPoint slideshow
                    case ".ppsm": // PowerPoint macro-enabled slideshow
                    case ".potx": // PowerPoint template
                    case ".potm": // PowerPoint macro-enabled template
                    case ".sldx": // PowerPoint slide
                    case ".sldm": // PowerPoint macro-enabled slide
                        return base.TestRuleString(path, PowerPointXmlToPlaintext(contents), ref snippets);

                    case ".ppt": // Legacy PowerPoint presentation
                    case ".pps": // Legacy PowerPoint slideshow
                    case ".pot": // Legacy PowerPoint template
                        return base.TestRule(path, contents, ref snippets); // Strings appear uniformly distributed - less inclusive parsing may not be necessary

                    case ".pa": // PowerPoint add-in
                    case ".ppa": // PowerPoint add-in
                    case ".ppam": // PowerPoint add-in
                        return false;

                    /* Microsoft OneNote */
                    case ".one": // OneNote export file
                    case ".onetoc2": // OneNote export file
                        return base.TestRule(path, contents, ref snippets);

                    /* Microsoft Outlook */
                    case ".eml":
                        return base.TestRule(path, contents, ref snippets);

                    case ".oft": // The format supports both ASCII and UTF-16 versions
                    case ".msg": // The format supports both ASCII and UTF-16 versions
                        return base.TestRule(path, contents, ref snippets) |
                            base.TestRuleString(path, Encoding.Unicode.GetString(contents), ref snippets) |
                            base.TestRuleString(path, Encoding.Unicode.GetString(contents.Skip(1).ToArray()), ref snippets);

                    case ".ost":
                    case ".pst":
                        return false; // Not yet analyzed

                    /* Microsoft Publisher */
                    // The format is undocumented, so we perform ASCII and UTF-16 string searches.
                    // The UTF-16 search is also performed with an offset of 1 to account for both evenly and oddly aligned unicode characters
                    case ".pub": // Publisher publication
                        return base.TestRule(path, contents, ref snippets) | 
                            base.TestRuleString(path, Encoding.Unicode.GetString(contents), ref snippets) |
                            base.TestRuleString(path, Encoding.Unicode.GetString(contents.Skip(1).ToArray()), ref snippets);

                    /* XPS */
                    case ".xps": // XML-based document format used for printing and preserving documents.
                        return base.TestRuleString(path, XpsXmlToPlaintext(contents), ref snippets);

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

        private string WordXmlToPlaintext(byte[] contents)
        {
            var sb = new StringBuilder();

            using (var content_stream = new MemoryStream(contents))
            {
                using (var package = Package.Open(content_stream, FileMode.Open, FileAccess.Read))
                {
                    var document_ct = "application/vnd.openxmlformats-officedocument.wordprocessingml.document.main+xml";

                    foreach (var part in package.GetParts().Where(p => p.ContentType.Equals(document_ct)))
                    {
                        var data = XDocument.Load(XmlReader.Create(part.GetStream()));

                        if (data != null)
                        {
                            XNamespace n = "http://schemas.openxmlformats.org/wordprocessingml/2006/main";

                            var paragraphs = data.Root
                                .Elements(n + "body")
                                .Descendants(n + "p");

                            foreach (var paragraph in paragraphs)
                            {
                                sb.AppendLine(paragraph
                                    .Elements(n + "r")
                                    .Elements(n + "t")
                                    .Aggregate(new StringBuilder(), (s, e) => s.Append(e.Value), s => s.ToString()));
                            }
                        }
                    }
                }
            }

            return sb.ToString();
        }

        private string ExcelXmlToPlaintext(byte[] contents)
        {
            var sb = new StringBuilder();

            using (var content_stream = new MemoryStream(contents))
            {
                using (var package = Package.Open(content_stream, FileMode.Open, FileAccess.Read))
                {
                    var strings_ct = "application/vnd.openxmlformats-officedocument.spreadsheetml.sharedStrings+xml";

                    foreach (var part in package.GetParts().Where(p => p.ContentType.Equals(strings_ct)))
                    {
                        var data = XDocument.Load(XmlReader.Create(part.GetStream()));

                        if (data != null)
                        {
                            XNamespace n = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";

                            foreach (var str in data.Root.Elements(n + "si"))
                            {
                                sb.AppendLine(str
                                    .Elements(n + "t")
                                    .Aggregate(new StringBuilder(), (s, e) => s.Append(e.Value), s => s.ToString()));
                            }
                        }
                    }
                }
            }

            return sb.ToString();
        }

        private string PowerPointXmlToPlaintext(byte[] contents)
        {
            var sb = new StringBuilder();

            using (var content_stream = new MemoryStream(contents))
            {
                using (var package = Package.Open(content_stream, FileMode.Open, FileAccess.Read))
                {
                    var slide_ct = "application/vnd.openxmlformats-officedocument.presentationml.slide+xml";

                    foreach (var part in package.GetParts().Where(s => s.ContentType.Equals(slide_ct)))
                    {
                        var data = XDocument.Load(XmlReader.Create(part.GetStream()));

                        if (data != null)
                        {
                            XNamespace p = "http://schemas.openxmlformats.org/presentationml/2006/main";
                            XNamespace a = "http://schemas.openxmlformats.org/drawingml/2006/main";

                            var paragraphs = data.Root
                                .Elements(p + "cSld")
                                .Descendants(a + "p");

                            foreach (var paragraph in paragraphs)
                            {
                                sb.AppendLine(paragraph
                                    .Elements(a + "r")
                                    .Elements(a + "t")
                                    .Aggregate(new StringBuilder(), (s, e) => s.Append(e.Value), s => s.ToString()));
                            }
                        }
                    }

                }
            }

            return sb.ToString();
        }

        private string XpsXmlToPlaintext(byte[] contents)
        {
            var sb = new StringBuilder();

            using (var content_stream = new MemoryStream(contents))
            {
                using (var package = Package.Open(content_stream, FileMode.Open, FileAccess.Read))
                {
                    var page_ct = "application/vnd.ms-package.xps-fixedpage+xml";

                    foreach (var part in package.GetParts().Where(p => p.ContentType.Equals(page_ct)))
                    {
                        var data = XDocument.Load(XmlReader.Create(part.GetStream()));

                        if (data != null)
                        {
                            XNamespace n = "http://schemas.microsoft.com/xps/2005/06";

                            foreach (var group in data.Root
                                .Descendants(n + "Glyphs")
                                .GroupBy(g => g.Attribute("OriginY").Value))
                            {
                                sb.AppendLine(group
                                    .Attributes("UnicodeString")
                                    .Aggregate(new StringBuilder(), (s, e) => s.Append(e.Value), s => s.ToString()));
                            }
                        }
                    }
                }
            }

            return sb.ToString();
        }

        private string ExcelBiff12ToPlaintext(byte[] contents)
        {
            var sb = new StringBuilder();

            using (var content_stream = new MemoryStream(contents))
            {
                using (var package = Package.Open(content_stream, FileMode.Open, FileAccess.Read))
                {
                    var string_part = package.GetPart(new Uri("/xl/sharedStrings.bin", UriKind.Relative));

                    using (var br = new BinaryReader(string_part.GetStream()))
                    {
                        if (br.ReadUInt16() == 0x019f /* SST */)
                        {
                            var record_length = br.ReadByte();
                            var count_strings = br.ReadUInt32();
                            var count_uniques = br.ReadUInt32();

                            for (var i = 0; i < count_strings; i++)
                            {
                                if (br.ReadByte() == 0x13 /* SI */)
                                {
                                    var length = br.ReadByte(); // length of the SI record
                                    var flags = br.ReadByte();

                                    var str_cchCharacters = br.ReadUInt32();
                                    var str_rgchData = br.ReadBytes((int)(str_cchCharacters * 2));
                                    sb.AppendLine(Encoding.Unicode.GetString(str_rgchData));

                                    if ((flags & 1) != 0)
                                    {
                                        for (uint j = 0, dwSizeStrRun = br.ReadUInt32(); j < dwSizeStrRun; j++)
                                        {
                                            var ich = br.ReadUInt16();
                                            var ifnt = br.ReadUInt16();
                                        }
                                    }

                                    if ((flags & 2) != 0)
                                    {
                                        var phoneticStr_cchCharacters = br.ReadUInt32();
                                        var phoneticStr_rgchData = br.ReadBytes((int)(phoneticStr_cchCharacters * 2));

                                        for (uint j = 0, dwPhoneticRun = br.ReadUInt32(); j < dwPhoneticRun; j++)
                                        {
                                            var ichFirst = br.ReadUInt16();
                                            var ichMom = br.ReadUInt16();
                                            var cchMom = br.ReadUInt16();
                                            var ifnt = br.ReadUInt16();
                                            br.ReadUInt16(); // flags (character set type / alignment)
                                        }
                                    }
                                }
                            }

                            if (br.ReadUInt16() != 0x01a0 /* SST_END */)
                            {
                                if (ProgramOptions.Verbose)
                                    Printer.Warning("Excel BIFF12 parser did not reach end of file.");
                            }
                        }
                    }
                }
            }

            return sb.ToString();
        }

        private string ExcelBiff8ToPlaintext(byte[] contents)
        {
            var sb = new StringBuilder();

            using (var content_stream = new MemoryStream(contents))
            {
                using (var br = new BinaryReader(content_stream))
                {
                    var cfb_header = br.ReadBytes(512);

                    while (br.BaseStream.Position < br.BaseStream.Length)
                    {
                        var biff_record_type = br.ReadUInt16();

                        if (biff_record_type == 0)
                            break;

                        var biff_record_size = br.ReadUInt16();
                        var biff_record_data = br.ReadBytes(biff_record_size);

                        if (biff_record_type == 0x00fc /* SST */)
                        {
                            using (var record_stream = new MemoryStream(biff_record_data))
                            {
                                using (var record_reader = new BinaryReader(record_stream))
                                {
                                    var count_strings = record_reader.ReadInt32();
                                    var count_uniques = record_reader.ReadInt32();

                                    for (var i = 0; i < count_strings; i++)
                                    {
                                        // Instances of 'XLUnicodeRichExtendedString'
                                        var cch = record_reader.ReadUInt16();
                                        var flags = record_reader.ReadByte();
                                        var cRun = (flags & 8) != 0 ? record_reader.ReadUInt16() : 0;
                                        var cbExtRst = (flags & 4) != 0 ? record_reader.ReadInt32() : 0;

                                        var rgb = string.Empty;

                                        if ((flags & 1) != 0)
                                            rgb = Encoding.Unicode.GetString(record_reader.ReadBytes(cch * 2));
                                        else
                                            rgb = Encoding.UTF8.GetString(record_reader.ReadBytes(cch));

                                        if (!string.IsNullOrEmpty(rgb))
                                            sb.AppendLine(rgb);

                                        for (var j = 0; j < cRun; j++)
                                        {
                                            var ich = record_reader.ReadUInt16();
                                            var ifnt = record_reader.ReadUInt16();
                                        }

                                        if (cbExtRst != 0)
                                            record_reader.ReadBytes(cbExtRst);
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return sb.ToString();
        }
    }
}
