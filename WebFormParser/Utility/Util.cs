using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AngleSharp.Dom;
using WebFormParser.model;

namespace WebFormParser.Utility
{
    public static class Util
    {

        public static IEnumerable<string> GetFiles(string path)
        {
            Queue<string> queue = new Queue<string>();
            queue.Enqueue(path);
            while (queue.Count > 0)
            {
                path = queue.Dequeue();
                try
                {
                    foreach (string subDir in Directory.GetDirectories(path))
                    {
                        queue.Enqueue(subDir);
                    }
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine(ex);
                }
                string[] files = null;
                try
                {
                    files = Directory.GetFiles(path);
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine(ex);
                }
                if (files != null)
                {
                    for (int i = 0; i < files.Length; i++)
                    {
                        yield return files[i];
                    }
                }
            }
        }

        public static string[] RemoveAt(string[] source, int index)
        {
            string[] dest = new string[source.Length - 1];
            if (index > 0)
                Array.Copy(source, 0, dest, 0, index);

            if (index < source.Length - 1)
                Array.Copy(source, index + 1, dest, index, source.Length - index - 1);

            return dest;
        }

        public static string ReplaceFirst(string text, string search, string replace)
        {
            int pos = text.ToLower().IndexOf(search.ToLower());
            if (pos < 0)
            {
                Source.error += "error finding: " + search + " in " + Source.filename + "\n";
                return text;
            }
            return text.Substring(0, pos) + replace + text.Substring(pos + search.Length);
        }

        public static bool RemoveFromSource(IElement? tag, string regex, string attribute)
        {
            if (tag != null && tag.Parent != null)
            {
                if (Source.source.Contains(tag.OuterHtml.ToString()) && !Source.source.Contains("<%--" + tag.OuterHtml.ToString() + "--%>"))
                {
                    Source.source = ReplaceFirst(Source.source, tag.OuterHtml.ToString(), "");
                    Source.changed = true;
                    return true;
                }
                MatchCollection matches = Regex.Matches(Source.source, regex);
                foreach (Match match in matches)
                {
#pragma warning disable CS8604 // Possible null reference argument.
                    if (match.ToString().Contains(tag.GetAttribute(attribute)))
                    {
                        if (Source.source.Contains(match.ToString()) && !Source.source.Contains("<%--" + match.ToString() + "--%>"))
                        {
                            Source.source = ReplaceFirst(Source.source, match.ToString(), "");
                            Source.changed = true;
                            return true;
                        }
                    }
#pragma warning restore CS8604 // Possible null reference argument.
                }
                Source.error += "Unable to remove tag " + attribute + " " + tag.GetAttribute(attribute) + " for " + Source.filename + "\n";
                return false;
            }
            return false;
        }

        public static string RandomString(int length)
        {
            var chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            var stringChars = new char[length];
            var random = new Random();

            for (int i = 0; i < stringChars.Length; i++)
            {
                stringChars[i] = chars[random.Next(chars.Length)];
            }

            var finalString = new string(stringChars);
            return finalString;
        }

        public static bool AddHiddenElementToBody(string outerHtml, string filename)
        {
            return addToElement("\t\t\t" + outerHtml, filename, "/form");
        }
        public static bool addToHead(string outerHtml, string filename)
        {
            return addToElement("\t\t" + outerHtml, filename, "/head");
        }
        public static bool addToElement(string outerHtml, string filename, string element)
        {
            string[] arrayOfLines = Source.source.Split("\n");
            string[] newArray = new string[arrayOfLines.Length + 1];
            bool notAdded = true;
            foreach (string line in arrayOfLines)
            {
                int index = Array.IndexOf(newArray, null);
                if (line.ToLower().Contains(element) && notAdded)
                {
                    notAdded = false;
                    newArray[index] = outerHtml;
                    newArray[index + 1] = line;
                }
                else
                {
                    newArray[index] = line;
                }
            }
            if (notAdded)
            {
                Source.error += "Could not find <" + element + "> for " + filename;
                return false;
            }
            Source.source = string.Join("\n", newArray);
            Source.changed = true;
            return true;
        }
    }
}
