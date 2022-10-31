using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AngleSharp.Dom;
using WebFormParser.model;
using WebFormParser.Utility;

namespace WebFormParser.csp
{
    internal class CleanStyle
    {
        public static IDocument StyleParser(IDocument document, string filename)
        {
            string css = "";
            var styleTags = document.GetElementsByTagName("style");
            foreach (var styleTag in styleTags)
            {
                css += styleTag.InnerHtml + "\n";
                styleTag.Remove();
                if (Source.source.ToLower().IndexOf("<style") == -1)
                {
                    Source.error += "error finding <style> tag for " + filename + "\n";
                }
                else
                {
                    string strToRemove = Source.source.Substring(Source.source.IndexOf("<style"), Source.source.IndexOf("</style>") - Source.source.IndexOf("<style") + 8);
                    Source.source = Source.source.Replace(strToRemove, "");
                }
            }
            var linkTags = document.GetElementsByTagName("link");
            foreach (var linkTag in linkTags)
            {
                if (linkTag.Parent == null)
                    continue;
                    
                var nodeName = linkTag.Parent.NodeName;
                var removedSuccessful = CssRemoveFromSource(linkTag);

                if (!removedSuccessful)
                    continue;

                linkTag.Parent?.RemoveChild(linkTag);
                var addedSuccessful = Util.addToHead(linkTag.OuterHtml, filename);
                
                if (!addedSuccessful)
                    continue;

                document.Head?.Append(linkTag);
            }

            var elements = document.All;
            foreach (var element in elements)
            {
                var style = element.GetAttribute("style");
                if (style != null)
                {
                    //Check if there is a style that we can do.
                    if (style != "" && !style.Contains("<%"))
                    {
                        var id = element.GetAttribute("id");

                        // if element has an ID
                        if (id == null)
                        {
                            Guid myuuid = Guid.NewGuid();
                            id = "randomId-" + myuuid.ToString();
                            id = id.Replace("-", "");
                            Source.source = Util.ReplaceFirst(Source.source, "style=\"" + style + "\"", "id=\"" + id + "\"");
                        }
                        else
                        {
                            Source.source = Util.ReplaceFirst(Source.source, "style=\"" + style + "\"", "");
                        }
                        element.SetAttribute("id", id);
                        element.RemoveAttribute("style");
                        css += "#" + id + " {" + style + "}\n";
                        Source.changed = true;
                    }
                    else
                    {
                        Source.error += "Manual Editing Required for: " + filename + "\n";
                    }
                }
            }

            if (css != "")
            {
                var cssFilename = filename + ".css";
                //Get old css and add it to the css variable, so that it can be written back
                //Otherwise the old css will get overwritten by File.Create(); -MH 2022
                if (File.Exists(cssFilename))
                {
                    string oldCSS = File.ReadAllText(cssFilename);
                    css = oldCSS + "\n" + css;
                }

                //Create file if it doesn't exist, else overwrite file.
                using (FileStream fs = File.Create(cssFilename))
                {
                    byte[] info = new UTF8Encoding(true).GetBytes(css);
                    fs.Write(info, 0, info.Length);
                }
                var cssLinkTag = document.CreateElement<AngleSharp.Html.Dom.IHtmlLinkElement>();
                cssLinkTag.SetAttribute("href", Path.GetFileName(cssFilename));
                cssLinkTag.SetAttribute("rel", "stylesheet");
                cssLinkTag.SetAttribute("type", "text/css");
                if (!Source.source.Contains(cssFilename))
                {
                    Util.addToHead(cssLinkTag.OuterHtml, cssFilename);
                }
                var head = document.Head;
                if (head != null)
                {
                    head.Append(cssLinkTag);
                }
            }
            return document;
        }

        private static bool CssRemoveFromSource(IElement linkTag)
        {
            return Util.RemoveFromSource(linkTag, @"<link[\s\S]*?/>", "href");
        }
    }
}
