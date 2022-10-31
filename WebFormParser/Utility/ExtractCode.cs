using System.Collections;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;

namespace WebFormParser.Utility
{
    public static class ExtractCode
    {
        private static readonly string _newLine = Environment.NewLine;

        public static Hashtable ParseCode(string fileName)
        {
            var fi = new FileInfo(fileName);

            var source = File.ReadAllText(fileName);
            var lines = source.Split(_newLine, StringSplitOptions.None);
            var blocks = new Hashtable();
            var block = new List<string>();
            var openTag = false;
            var isCode = false;
            int blockCnt = 0;

            var aspx = new List<string>();

            foreach (var line in lines)
            {
                bool skip = ParseLine(line, ref openTag, ref aspx);
                if (skip)
                    continue;

                if (ProcessLine(line, ref block, ref isCode, ref aspx))
                {
                    if (block.Count <= 1)
                    {
                        block = new List<string>();
                        continue;
                    }

                    blockCnt += 1;
                    var blockStr = "Render_Logic_" + blockCnt.ToString("D2");
                    blocks.Add(blockStr, block);
                    aspx.Insert(aspx.Count - 1, " " + blockStr + "();");
                    block = new List<string>();

                }
            }
            blocks.Add(fi.Name, aspx);
            return blocks;
        }

        private static bool ParseLine(string line, ref bool openTag, ref List<string> output)
        {
            bool ret = false;

            if (line.Contains("<%--"))
                openTag = true;

            if (line.Contains("<%@"))
                ret = true;
            if (string.IsNullOrEmpty(line))
                ret = true;

            if (line.Contains("--%>"))
            {
                openTag = false;
                ret = true;
            }

            bool check = (ret || openTag);

            if (check)
                output.Add(line);

            return check;
        }

        private static bool ProcessLine(string line, ref List<string> block, ref bool isCode, ref List<string> output)
        {
            var ret = false;

            if (line.Contains("<%"))
            {
                output.Add(line);
                isCode = true;
            }

            if (!isCode)
                return false;

            var content = line.Replace("<%", "");
            content = content.Replace("%>", "");
            content = content.Trim();

            if (isCode && !string.IsNullOrEmpty(content))
                block.Add(content);

            if (!line.Contains("%>"))
                return ret;

            output.Add(line);
            isCode = false;
            ret = true;

            return ret;
        }

        public static List<string>? ProcessCode(Hashtable blocks, string fileName)
        {
            var fi = new FileInfo(fileName + ".cs");
            var srcFileName = fi.Name;
            var srcDirectory = fi.Directory;

            if (srcDirectory == null)
                return null;

            var sourceFile = Path.Combine(srcDirectory.ToString(), srcFileName);

            if (!File.Exists(sourceFile))
                return null;

            var code = File.ReadAllText(sourceFile);
            var tree = CSharpSyntaxTree.ParseText(code);
            var codeRoot = tree.GetCompilationUnitRoot();
            ClassDeclarationSyntax? codeClass;

            if (codeRoot.Members[0].Kind() == SyntaxKind.NamespaceDeclaration)
            {
                codeClass = (ClassDeclarationSyntax?)((NamespaceDeclarationSyntax)codeRoot.Members[0]).Members[0];
            }
            else
            {
                codeClass = (ClassDeclarationSyntax?)codeRoot.Members[0];
            }

            if (codeClass == null) return null;
            var newCodeClass = codeClass;

            foreach (var key in blocks.Keys)
            {
                if (key == null)
                    continue;

                var blockName = key.ToString();

                if (blockName == null) continue;

                var block = (List<string>?)blocks[key];
                var methodToInsert = Util.GetMethodDeclarationSyntax("void", blockName, block, "protected");
                newCodeClass = newCodeClass.AddMembers(methodToInsert);
            }

            SyntaxAnnotation annotation = Formatter.Annotation;

            var newRoot = codeRoot.ReplaceNode(codeClass, newCodeClass);
            code = newRoot.NormalizeWhitespace().ToFullString();
            List<string> ret = code.Split(Environment.NewLine).ToList();

            return ret;
        }
    }
}
