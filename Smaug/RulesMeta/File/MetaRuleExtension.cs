using System.Collections.Generic;
using System.IO;

namespace Smaug.RulesMeta.File
{
    class MetaRuleExtension : IMetaRule
    {
        private HashSet<string> AcceptExtensions { get; } = new HashSet<string>()
        {
            /* Credential files */
            ".agilekeychain",
            ".cred",
            ".kdb",
            ".kdbx",
            ".key",
            ".keychain",
            ".keyfile",
            ".kwallet",
            ".pass",
            ".psafe3",

            /* Certificate files */
            ".der",
            ".pem",
            ".pfx",
            ".p12",
            ".pk12",
            ".pkcs12",
            
            /* Database files */
            //".bak",
            ".mdf",
            ".sdf",
            ".sql",
            ".sqlite",
            ".sqlite3",

            /* Infrastructure as Code (IaC) files */
            ".cscfg",
            ".tfvars",
            
            /* Memory dumps */
            //".dmp",
            
            /* Network dumps */
            ".cap",
            ".pcap",
            ".pcapng",
            
            /* Remote access files */
            ".ppk",
            ".rdp",
            ".rdg",
            ".rtsz",
            ".rtsx",
            ".tvopt",
            ".sdtid",
            
            /* Virtual Machine (VM) files */
            ".wim",
            ".ova",
            ".ovf",
            ".vmx",

            /* VPN files */
            ".ovpn",
            ".hat",
        };

        private HashSet<string> RejectExtensions { get; } = new HashSet<string>()
        {
            ".aac",
            ".adml",
            ".admx",
            ".ai",
            ".aiff",
            ".arw",
            ".avchd",
            ".avi",
            ".bmp",
            ".cr2",
            ".css",
            ".dib",
            ".eps",
            ".etl",
            ".flac",
            ".flv",
            ".gif",
            ".heic",
            ".heif",
            ".ind",
            ".indd",
            ".indt",
            ".j2k",
            ".jfi",
            ".jfif",
            ".jif",
            ".jp2",
            ".jpe",
            ".jpeg",
            ".jpf",
            ".jpg",
            ".jpm",
            ".jpx",
            ".k25",
            ".less",
            ".lock",
            ".m4a",
            ".m4p",
            ".m4v",
            ".mj2",
            ".mov",
            ".mp2",
            ".mp3",
            ".mp4",
            ".mpe",
            ".mpeg",
            ".mpg",
            ".mpv",
            ".nrw",
            ".nse",
            ".oga",
            ".ogg",
            ".otf",
            ".pcm",
            ".pdf",
            ".png",
            ".psd",
            ".pyc",
            ".pyi",
            ".qt",
            ".svg",
            ".svgz",
            ".swf",
            ".tif",
            ".tiff",
            ".ttf",
            ".wav",
            ".webm",
            ".webp",
            ".wmv",
            ".xcf",
            ".xsd",
            ".xsl",
        };

        public bool? TestRule(string path)
        {
            var extension = Path.GetExtension(path);

            if (!string.IsNullOrEmpty(extension))
            {
                var temp = extension.ToLower();

                if (RejectExtensions.Contains(temp))
                    return false;
                else if (AcceptExtensions.Contains(temp))
                    return true;
            }

            return null;
        }

        public override string ToString()
        {
            return "extension";
        }
    }
}
