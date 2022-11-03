using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AngleSharp.Html.Parser;
using AngleSharp;
using AngleSharp.Dom;
using WebFormParser.csp;
using WebFormParser.model;

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

        static void ParseFile(string filename)
        {
            Source.filename = filename;
            string source = File.ReadAllText(filename);
            string[] lines = source.Split(
                new string[] { Environment.NewLine },
                StringSplitOptions.None
            );
            var line1 = "";
            if (lines[0].Contains("<%@"))
            {
                line1 = lines[0];
                lines = Util.RemoveAt(lines, 0);
            }
            source = string.Join(System.Environment.NewLine, lines);
            Source.source = source;
            Source.changed = false;
            Source.addJsParserToHeader = false;
            Source.embeddedJsScript = String.Empty;
            //Use the default configuration for AngleSharp
            var config = Configuration.Default;

            //Create a new context for evaluating webpages with the given config
            var context = BrowsingContext.New(config);

            //Just get the DOM representation
            //IDocument document = await context.OpenAsync(req => req.Content(source));
            var parser = new HtmlParser();
            IDocument document = parser.ParseDocument(Source.source);
            document = CleanStyle.StyleParser(document, filename);
            document = CleanJs.JsParser(document, filename);

            //source = document.ToHtml();
            if (Source.changed)
            {
                Source.changed = false;
                if (line1 != "")
                {
                    Source.source = line1 + "\n" + Source.source;
                }

                using FileStream fs = File.Create(filename);
                byte[] info = new UTF8Encoding(true).GetBytes(Source.source);
                fs.Write(info, 0, info.Length);
            }
        }
    }
}
