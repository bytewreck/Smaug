using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;

namespace Smaug.RulesMeta.Directory
{
    class MetaRulePath : IMetaRule
    {
        private List<string> AcceptDirectories { get; } = new List<string>()
        {
            "\\.azure",
            "\\.aws",
            "\\.ssh",
        };

        private List<string> RejectDirectories { get; } = new List<string>()
        {
            /* Windows (WINDIR) */
            "\\windows\\appcompat",
            "\\windows\\apppatch",
            "\\windows\\bcastdvr",
            "\\windows\\bitlockerdiscoveryvolumecontents",
            "\\windows\\branding",
            "\\windows\\browsercore",
            "\\windows\\assembly",
            "\\windows\\assembly",
            "\\windows\\boot",
            "\\windows\\cursors",
            "\\windows\\debug",
            "\\windows\\diagtrack",
            "\\windows\\downloaded program files",
            "\\windows\\diagnostics",
            "\\windows\\elambkup",
            "\\windows\\fonts",
            "\\windows\\globalization",
            "\\windows\\help",
            "\\windows\\identitycrl",
            "\\windows\\ime",
            "\\windows\\immersivecontrolpanel",
            "\\windows\\inboxapps",
            "\\windows\\inf",
            "\\windows\\inputmethod",
            "\\windows\\installer",
            "\\windows\\l2schemas",
            "\\windows\\livekernelreports",
            "\\windows\\logs",
            "\\windows\\media",
            "\\windows\\microsoft.net",
            "\\windows\\ocr",
            "\\windows\\offline web pages",
            "\\windows\\performance",
            "\\windows\\personalization",
            "\\windows\\pla",
            "\\windows\\policydefinitions",
            "\\windows\\prefetch",
            "\\windows\\printdialog",
            "\\windows\\provisioning",
            "\\windows\\registration",
            "\\windows\\rescache",
            "\\windows\\resources",
            "\\windows\\schemas",
            "\\windows\\security",
            "\\windows\\servicing", "\\servicing\\LCU",
            "\\windows\\servicestate",
            "\\windows\\setup",
            "\\windows\\shellcomponents",
            "\\windows\\shellexperiences",
            "\\windows\\skb",
            "\\windows\\softwaredistribution",
            "\\windows\\speech_onecore",
            "\\windows\\speech",
            "\\windows\\system",
            "\\windows\\system32", "\\system32",
            "\\windows\\systemapps",
            "\\windows\\systemresources",
            "\\windows\\syswow64", "\\syswow64",
            "\\windows\\tempinst",
            "\\windows\\twain_32",
            "\\windows\\uus",
            "\\windows\\waas",
            "\\windows\\web",
            "\\windows\\winsxs", "\\winsxs",

            /* AppData */
            "\\appdata\\local\\microsoft",
            "\\appdata\\locallow\\microsoft",
            "\\appdata\\roaming\\microsoft\\teams",
            "\\appdata\\roaming\\microsoft\\windows",

            "\\appdata\\local\\jedi",
            "\\appdata\\local\\nuget",
            "\\appdata\\local\\packages",
            "\\appdata\\local\\pip",
            "\\appdata\\local\\postman",
            "\\appdata\\local\\programs",
            "\\appdata\\local\\squirreltemp",

            /* Program Files */
            ":\\program files",
            ":\\program files (x86)",
            "$\\program files",
            "$\\program files (x86)",

            /* Misc */
            ":\\programdata",
            "$\\programdata",

            "\\.gradle",
            "\\.nuget",
            "\\.vscode",

            "\\adobe",
            "\\lenovo",
            "\\microsoft\\clicktorun",
            "\\microsoft\\visualstudio",
            "\\usoshared\\logs",
            "android\\sdk",
            "google\\chrome",
            "mozilla\\firefox",

            "\\appdata\\local\\npm-cache",
            "\\lib\\site-packages",
            "\\lib\\ruby",
            "\\node_modules"
        };

        public bool? TestRule(string path)
        {
            if (!string.IsNullOrEmpty(path))
            {
                var temp = path.ToLower();

                if (RejectDirectories.Any(s => temp.Contains(s)))
                    return false;
                else if (AcceptDirectories.Any(s => temp.Contains(s)))
                    return true;
            }

            return null;
        }

        public override string ToString()
        {
            return "meta:path";
        }
    }
}
