using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using AngleSharp;
using WebFormParser.model;

namespace WebFormParser.Utility.Csp
{
    public static class Parser
    {
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
            source = string.Join(Environment.NewLine, lines);
            Source.source = source;
            Source.changed = false;
            Source.addJsParserToHeader = false;
            Source.embeddedJsScript = string.Empty;
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
