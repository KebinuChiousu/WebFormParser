using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WebFormParser.model;
using WebFormParser.Utility.Csp;

namespace WebFormParser.Utility.Legacy
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
                Hashtable blocks = ExtractCode.ParseCode(page);
                var fi = new FileInfo(page);
                var fileName = fi.Name;
                var aspx = (List<string>?)blocks[fileName];
                Util.WriteFile(aspx, src, dest, page);
                blocks.Remove(fileName);
                var code = ExtractCode.ProcessCode(blocks, page);
                Util.WriteFile(code, src, dest, page + ".cs");
            }

            Program.Stage++;
        }

        public static void FixCode(string src, string dest)
        {
            Console.WriteLine("Phase 2: Analyzing C# code and applying custom fixes.");

            var pages = Util.GetPageList(src);

            Dictionary<string, List<string>> source = new();

            foreach (var page in pages)
            {
                var srcName = page + ".cs";
                var destPage = page.Replace(src, dest);
                var code = File.ReadAllLines(srcName).ToList();
                source.Add(srcName, code);
            }

            Console.WriteLine("Generic Fix 1: Inserting missing using statements");
            source = ExtractCode.AddUsing(source);

            Util.WriteFiles(source, src, dest);

            Program.Stage++;
        }
    }
}
