using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WebFormParser.model;
using WebFormParser.Utility.Asp;
using WebFormParser.Utility.Asp.Enum;
using WebFormParser.Utility.Csp;

namespace WebFormParser.Utility
{
    public class BusinessLogic
    {
        public static void CleanCode(string src, string dest)
        {
            Console.WriteLine("Phase 1: Determine code to be extracted from aspx page.");

            var pages = Util.GetPageList(src);
            Util.MakeFolders(pages, src, dest);

            foreach (var page in pages)
            {
                Console.WriteLine(page);

                var input = File.ReadAllText(page);

                var entries = Asp.Parser.ParseDocument(input);
                var htmlList = Asp.CodeGen.Generate(ref entries, mode: AspFileEnum.Html);
                var codeList = Asp.CodeGen.Generate(ref entries, page);

                Util.WriteFile(htmlList, src, dest, page);
                Util.WriteFile(codeList, src, dest, page + ".cs");
            }

            Program.Stage++;
        }

        public static void FixCode(string src, string dest)
        {
            return;
        }
    }
}
