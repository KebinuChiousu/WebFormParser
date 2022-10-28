using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebFormParser.model
{
    public static class Source
    {
        public static string source = "";
        public static bool changed = false;
        public static bool addJsParserToHeader = false;
        public static bool addJsParserDotJs = false;
        public static string embeddedJsScript = string.Empty;
        internal static string error = "\n\nERROR LOG: \n";
        public static string filename = string.Empty;
        public static List<string> anonymouseFunctions = new List<string>();
    }

}
