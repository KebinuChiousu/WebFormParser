using System.Collections;
using System.Diagnostics;
using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;

namespace WebFormParser.Utility.Legacy
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
                bool skip = ParseLine(line, ref openTag);
                if (skip)
                {
                    aspx.Add(line);
                    continue;
                }

                if (ProcessLine(line, ref block, ref isCode, ref aspx))
                {
                    if (block.Count == 0)
                        continue;

                    if (!ValidateCode(ref block, ref aspx))
                        continue;

                    blockCnt += 1;
                    var blockStr = "Render_Logic_" + blockCnt.ToString("D2");
                    blocks.Add(blockStr, block);
                    aspx.Add("<% " + blockStr + "(); %>");
                    block = new List<string>();
                }
            }
            blocks.Add(fi.Name, aspx);
            return blocks;
        }

        private static bool ValidateCode(ref List<string> block, ref List<string> aspx)
        {

            bool check;
            //var newBlock = new List<string>();

            /*
            List<char> symbols = new()
            {
                '{',
                '}'
            };

            if (CleanBlock(block, symbols, ref newBlock)) 
                return true;
            */

            int open = Util.CountChar(block, '{');
            int close = Util.CountChar(block, '}');

            check = open == close;


            if (check)
            {
                check = Util.CheckIf(block);

                if (block[0].Contains("{"))
                    check = false;
                if (block[0].Contains("}"))
                    check = false;
                if (block[0].Contains("readonly"))
                    check = false;
                if (block[^1].Contains("{"))
                    return false;
                if (block[^1].Contains("}"))
                    return false;

                if (check)
                    return true;
            }

            aspx.Add("<%");
            foreach (var line in block)
            {
                aspx.Add(line);
            }
            aspx.Add("%>");
            block = new List<string>();

            /*
            if (newBlock.Count == 0)
            {
                return false;
            }
            else 
            {
                block = newBlock;
                return true;
            }
            */

            return check;
        }

        private static bool ParseLine(string line, ref bool openTag)
        {
            bool ret = false;

            if (string.IsNullOrWhiteSpace(line))
                return false;

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

            bool check = ret || openTag;
            return check;
        }

        private static bool ProcessLine(string line, ref List<string> block, ref bool isCode, ref List<string> output)
        {
            var ret = false;

            if (string.IsNullOrWhiteSpace(line))
                return false;

            if (line.Contains("<%") && line.Contains("%>"))
            {
                output.Add(line);
                return false;
            }

            if (line.Contains("<%"))
            {
                isCode = true;
            }

            if (!isCode)
            {
                output.Add(line);
                return false;
            }

            var content = line.Replace("<%", "");
            content = content.Replace("%>", "");
            content = content.Trim();

            if (isCode && !string.IsNullOrEmpty(content))
                block.Add(content);

            if (!line.Contains("%>"))
                return ret;

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



        #region "Add Using Statement"

        public static Dictionary<string, List<string>> AddUsing(Dictionary<string, List<string>> source)
        {
            Dictionary<string, List<string>> ret = new();

            foreach (var page in source.Keys)
            {
                var src = source[page];
                List<string> usingList = new();

                if (Util.PageContains(src, "HttpContext"))
                    if (!Util.PageContains(src, "using System.Web"))
                        usingList.Add("System.Web");

                if (usingList.Count == 0)
                    continue;

                src = InsertUsing(page, usingList);

                if (src.Count > 0)
                    ret.Add(page, src);
            }

            return ret;
        }



        private static List<string> InsertUsing(string fileName, List<string> usingList)
        {
            List<string> ret = new();
            var code = File.ReadAllText(fileName);
            var tree = CSharpSyntaxTree.ParseText(code);

            List<UsingDirectiveSyntax> udsList = new();

            foreach (var entry in usingList)
            {
                var insertTree = CSharpSyntaxTree.ParseText("using " + entry + ";" + Environment.NewLine);
                if (insertTree.GetRoot().ChildNodes().First() is UsingDirectiveSyntax uds)
                    udsList.Add(uds);
            }

            if (udsList.Count == 0)
                return ret;

            var codeRoot = UpdateUsingDirectives(tree, udsList);
            var annotation = Formatter.Annotation;

            if (codeRoot == null)
                return ret;

            code = codeRoot.NormalizeWhitespace().ToFullString();
            ret = code.Split(Environment.NewLine).ToList();

            return ret;
        }

        private static CompilationUnitSyntax? UpdateUsingDirectives(SyntaxTree originalTree, List<UsingDirectiveSyntax> usingList)
        {
            var rootNode = originalTree.GetRoot() as CompilationUnitSyntax;
            rootNode = rootNode?.AddUsings(usingList.ToArray());
            return rootNode;
        }

        #endregion
    }
}
