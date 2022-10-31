using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AngleSharp.Dom;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
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

        public static void MakeFolders(List<string> pages, string src, string dest)
        {
            foreach (var page in pages)
            {
                var fi = new FileInfo(page);
                var srcDir = fi.Directory.ToString();
                var destDir = srcDir.Replace(src, dest, false, null);

                if (!Directory.Exists(destDir))
                    Directory.CreateDirectory(destDir);
            }
        }

        public static void WriteFile(List<string>? lines, string root, string src, string dest, string fileName)
        {
            if (lines == null)
                return;
            
            var srcRoot = Path.Combine(root, src);
            var destRoot = Path.Combine(root, dest);

            var destFileName = fileName.Replace(srcRoot, destRoot);
            using var outputFile = new StreamWriter(destFileName);

            foreach (var line in lines)
            {
                outputFile.WriteLine(line);
            }
        }

        public static MethodDeclarationSyntax GetMethodDeclarationSyntax(string returnTypeName,
            string methodName, string[]? parameterTypes, 
            string[]? parameterNames, List<string>? body, string modifier = "private")
        {
            ParameterListSyntax parameterList;
            BlockSyntax block;

            if (parameterTypes != null && parameterNames != null)
            {
                parameterList =
                    SyntaxFactory.ParameterList(
                        SyntaxFactory.SeparatedList(GetParametersList(parameterTypes, parameterNames)));
            }
            else
            {
                parameterList = SyntaxFactory.ParameterList();
            }

            if (body != null)
            {
                List<StatementSyntax> statements = new List<StatementSyntax>();

                foreach (var line in body)
                {
                    var statement = SyntaxFactory.ParseStatement(line);
                    statements.Add(statement);
                }

                block = SyntaxFactory.Block().AddStatements(statements.ToArray());
            }
            else
            {
                block = SyntaxFactory.Block();
            }

            SyntaxToken modify;

            switch (modifier)
            {
                case "public":
                    modify = SyntaxFactory.Token(SyntaxKind.PublicKeyword);
                    break;
                case "protected":
                    modify = SyntaxFactory.Token(SyntaxKind.ProtectedKeyword);
                    break;
                default:
                    modify = SyntaxFactory.Token(SyntaxKind.PrivateKeyword);
                    break;
            }

            return SyntaxFactory.MethodDeclaration(SyntaxFactory.ParseTypeName(returnTypeName), methodName)
                .AddModifiers(modify)
                .WithBody(block)
                // Annotate that this node should be formatted
                .WithAdditionalAnnotations(Formatter.Annotation);
        }

        private static IEnumerable<ParameterSyntax> GetParametersList(string[] parameterTypes, string[] paramterNames)
        {
            for (int i = 0; i < parameterTypes.Length; i++)
            {
                yield return SyntaxFactory.Parameter(attributeLists: SyntaxFactory.List<AttributeListSyntax>(),
                    modifiers: SyntaxFactory.TokenList(),
                    type: SyntaxFactory.ParseTypeName(parameterTypes[i]),
                    identifier: SyntaxFactory.Identifier(paramterNames[i]),
                    @default: null);
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
