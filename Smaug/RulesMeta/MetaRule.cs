using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Smaug.RulesMeta
{
    class MetaRule : IMetaRule
    {
        public virtual bool? TestRule(string path)
        {
            return null;
        }
        
        protected bool TestDefaultRegex(string name)
        {
            return ProgramOptions.SearchMetaPatterns.Any(k => Regex.IsMatch(name, k));
        }
    }
}
