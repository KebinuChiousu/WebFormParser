﻿using System.Collections;
using System.Runtime.CompilerServices;
using System.Text;
using AngleSharp;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using CommandLine;
using WebFormParser.csp;
using WebFormParser.model;
using WebFormParser.Utility;

namespace WebFormParser
{
    internal class Program
    {
        static void Main(string[] args)
        {
            CommandLine.Parser.Default.ParseArguments<Options>(args)
                .WithParsed(RunOptions);
        }

        static void RunOptions(Options opts)
        {
            CleanCode(opts.Source, opts.Destination);
        }

        private static void CleanCode(string src, string dest)
        {
            List<string> pages = GetPageList(src);
            Util.MakeFolders(pages, src, dest);

            foreach (var page in pages)
            {
                Console.WriteLine(page);
                Hashtable blocks = ExtractCode.ParseCode(page);
                var fi = new FileInfo(page);
                var fileName = fi.Name;
                var aspx = (List<string>?) blocks[fileName];
                Util.WriteFile(aspx,  src, dest, page);
                blocks.Remove(fileName);
                var code = ExtractCode.ProcessCode(blocks, page);
                Util.WriteFile(code,  src, dest, page + ".cs");
            }
            Console.WriteLine(pages.Count);
        }

        private static List<string> GetPageList(string targetFolder)
        {
            List<string> result = new();

            foreach (var fileName in Util.GetFiles(targetFolder))
            {
                var extension = Path.GetExtension(fileName);
                var validExtensions = new string[] { ".aspx" };
                if (validExtensions.Contains(extension))
                {
                    result.Add(fileName);
                }
            }

            return result;
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