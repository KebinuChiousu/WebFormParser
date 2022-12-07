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
            var input = Util.GetEmbeddedString("ClassicAspMigration.asp");
            var fileName = "ClassicAspMigration";
            var entries = Parser.GetRegexGroupMatches(input);
            var htmlList = Asp.CodeGen.Generate(ref entries, mode: AspFileEnum.Html);
            var codeList = Asp.CodeGen.Generate(ref entries, fileName);

            // PrintAspNodeTree(entries);
            
            PrintCode(AspFileEnum.Html, htmlList);
            Console.WriteLine("");
            PrintCode(AspFileEnum.CodeBehind, codeList);
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

        private static void PrintCode(AspFileEnum fileType, List<string> entries)
        {
            var title = fileType == AspFileEnum.Html ? "Html: " : "Code: ";
            Console.WriteLine(title);

            foreach (var entry in entries)
            {
                Console.WriteLine(entry);
            }
        }
    }
}
