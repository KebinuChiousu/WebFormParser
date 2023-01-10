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

            var pageCnt = 1;
            double percent = 0;
            var dtSessionStart = DateTime.Now;
            var dtEnd = DateTime.Now;
            TimeSpan diffDate; 
            double seconds = 0;
            foreach (var page in pages)
            {
                var dtStart = DateTime.Now;
                percent = ((double)pageCnt / (double)pages.Count);

                Console.WriteLine($"Page: {pageCnt} of {pages.Count} ({percent:P2}) - {page}");
                var input = File.ReadAllText(page);

                var entries = Asp.Parser.ParseDocument(input, page);
                var htmlList = Asp.CodeGen.Generate(ref entries, mode: AspFileEnum.Html);

                if (htmlList.Count == 0)
                    continue;

                var codeList = Asp.CodeGen.Generate(ref entries, page);

                // Util.PrintAspNodeTree(entries);
                
                // _ = Console.ReadKey();

                // Util.PrintCode(AspFileEnum.Html, htmlList);
                // Console.WriteLine("");
                // Util.PrintCode(AspFileEnum.CodeBehind, codeList);
                // _ = Console.ReadKey();

                Util.WriteFile(htmlList, src, dest, page);
                Util.WriteFile(codeList, src, dest, page + ".cs");

                dtEnd = DateTime.Now;
                diffDate = dtEnd - dtStart;
                Console.WriteLine($"Page Processing Time: {diffDate.TotalSeconds:F} seconds.");


                pageCnt++;
            }

            diffDate = dtEnd - dtSessionStart;
            Console.WriteLine($"Processing Complete: {diffDate.TotalSeconds:F} seconds.");

            Program.Stage++;
        }

        public static void FixCode(string src, string dest)
        {
            return;
        }
    }
}
