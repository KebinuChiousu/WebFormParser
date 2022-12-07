using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AngleSharp.Dom;
using Microsoft.VisualBasic;
using WebFormParser.Utility.Asp;
using WebFormParser.Utility.Asp.Enum;

namespace WebFormParser.Utility
{
    public static class Demo
    {
        public static void PrintClassicNodes()
        {
            
            string input = Util.GetEmbeddedString("ClassicAspMigration.asp");

            var entries = Asp.Parser.GetRegexGroupMatches(input);

            var htmlList = Asp.CodeGen.Generate(ref entries, AspFileEnum.Html);
            var codeList = Asp.CodeGen.Generate(ref entries);

            // PrintAspNodeTree(entries);
            // PrintHtmlCode(htmlList);
        }

        private static void PrintAspNodeTree(List<Entry> entries)
        {
            Console.WriteLine("ASP Node Tree:");

            foreach (var entry in entries)
            {
                Console.WriteLine($"FileType: {entry.GetFileType(entry.FileType)}");
                Console.WriteLine($"Group: {entry.GroupName}");
                Console.WriteLine($"Value: {entry.Value}");
            }
        }

        private static void PrintHtmlCode(List<string> htmlList)
        {
            Console.WriteLine("Html: ");

            foreach (var entry in htmlList)
            {
                Console.WriteLine(entry);
            }
        }
    }
}
