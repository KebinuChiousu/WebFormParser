using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebFormParser.Utility.Asp
{
    public class Attr
    {
        public readonly string Value;
        public readonly bool IsValue;

        public Attr(string value, bool isValue)
        {
            this.Value = value;
            this.IsValue = isValue;
        }
    }
}
