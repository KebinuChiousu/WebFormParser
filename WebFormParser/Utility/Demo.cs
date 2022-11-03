using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.VisualBasic;

namespace WebFormParser.Utility
{
    public static class Demo
    {
        public static void PrintClassicNodes()
        {
            
            string input = Util.GetEmbeddedString("ClassicAspMigration.asp");

            var entries = Asp.Parser.GetRegexGroupMatches(input);

            foreach (var entry in entries)
            {
                Console.WriteLine($"Group {entry.GroupName}");
                Console.WriteLine($"Value: {entry.Value}");
            }
        }
    }
}
