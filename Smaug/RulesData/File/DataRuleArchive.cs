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
    class DataRuleArchive : DataRule
    {
        public override bool? TestRule(string path, byte[] contents, ref List<string> snippets)
        {
            var temp = Path.GetExtension(path);

            if (temp != null)
            {
                switch (temp.ToLower())
                {
                    case ".7z":
                    case ".bzip2":
                    case ".gzip":
                    case ".rar":
                    case ".tar":
                    case ".wim":
                    case ".xr":
                        return false;
                    case ".zip":
                        //ZipToPlaintext(contents);
                        return false;
                    default:
                        break;
                }
            }

            return null;
        }

        public override string ToString()
        {
            return "archive";
        }

        private string ZipToPlaintext(byte[] contents)
        {
            var sb = new StringBuilder();

            using (var content_stream = new MemoryStream(contents))
            {
                using (var package = Package.Open(content_stream, FileMode.Open, FileAccess.Read))
                {
                    
                }
            }

            return sb.ToString();
        }
    }
}
