using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
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

            var entries = Parser.ParseDocument(input);
            var htmlList = Asp.CodeGen.Generate(ref entries, mode: AspFileEnum.Html);
            var codeList = Asp.CodeGen.Generate(ref entries, fileName);

            // Util.PrintAspNodeTree(entries);
            
            Util.PrintCode(AspFileEnum.Html, htmlList);
            Console.WriteLine("");
            Util.PrintCode(AspFileEnum.CodeBehind, codeList);
        }

    }
}
