using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Smaug.RulesMeta
{
    interface IMetaRule
    {
        bool? TestRule(string path);
    }
}
