using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading.Tasks;
using WebFormParser.Utility.Asp.Enum;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;

namespace WebFormParser.Utility.Asp
{
    public static class CodeGen
    {
        public static List<string> Generate(ref List<Entry> codeDom, string fileName = "", AspFileEnum mode = AspFileEnum.CodeBehind)
        {
            var ret = string.Empty;

            return mode switch
            {
                AspFileEnum.CodeBehind => GenCodeFile(codeDom, fileName),
                _ => GenHtmlFile(ref codeDom)
            };
        }

        #region "CodeBehind"

        private static List<string> GenCodeFile(List<Entry> codeDom, string fileName)
        {
            var ret = new List<string>();

            var ns = GetNamespace(fileName);
            ns = AddUsing(ns, "System");
            ns = AddUsing(ns, "System.Web");

            ClassDeclarationSyntax? classDeclaration = null;

            if (ns.Members.Count > 0)
                classDeclaration = (ClassDeclarationSyntax) ns.Members[0];
            
            classDeclaration ??= GetClass(fileName);
            var newClass = classDeclaration;

            codeDom = codeDom.Where(e => e.FileType == AspFileEnum.CodeBehind).ToList();

            var codeFunc = string.Empty;
            var stmtList = new List<Entry>();
            var stmt = string.Empty;

            foreach (var entry in codeDom)
            {
                if (codeFunc != entry.CodeFunction)
                {
                    if (!string.IsNullOrEmpty(codeFunc))
                        newClass = AddFunction(newClass, codeFunc, stmtList);

                    stmtList.Clear();
                    codeFunc = entry.CodeFunction;
                }

                stmtList.Add(ExtractCode(entry));
            }

            if (stmtList.Count > 0)
                newClass = AddFunction(newClass, codeFunc, stmtList);

            if (ns.Members.Count > 0)
            {
                ns.Members.Replace(classDeclaration, newClass);
            }
            else
            {
                // Add the class to the namespace.
                ns = ns.AddMembers(newClass);
            }

            // Normalize and get code as string.
            var code = ns
                .NormalizeWhitespace()
                .ToFullString();

            ret = code.Split(Environment.NewLine).ToList();

            return ret;
        }

        #region "Namespace Generation"

        private static NamespaceDeclarationSyntax? GetNamespaceFromFile(string fileName)
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

            if (codeRoot.Members[0].Kind() == SyntaxKind.NamespaceDeclaration)
                return (NamespaceDeclarationSyntax)codeRoot.Members[0];

            return null;
        }

        private static NamespaceDeclarationSyntax GetNamespace(string fileName, string ns = "WebForms")
        {
            NamespaceDeclarationSyntax? nsDecl = null;

            if (!string.IsNullOrEmpty(fileName))
                nsDecl = GetNamespaceFromFile(fileName);

            if (nsDecl != null)
                return nsDecl;

            // Create a namespace: (namespace CodeGenerationSample)
            return SyntaxFactory.NamespaceDeclaration(SyntaxFactory.ParseName(ns)).NormalizeWhitespace();
        }

        private static NamespaceDeclarationSyntax AddUsing(NamespaceDeclarationSyntax ns, string strUsing)
        {
            return ns.AddUsings(SyntaxFactory.UsingDirective(SyntaxFactory.ParseName(strUsing)));
        }

        #endregion

        #region "Class Generation"

        private static ClassDeclarationSyntax? GetClassByFilename(string fileName)
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

            if (codeRoot.Members[0].Kind() == SyntaxKind.ClassDeclaration)
                return (ClassDeclarationSyntax)codeRoot.Members[0];

            return null;
        }

        private static ClassDeclarationSyntax GetClass(string className)
        {
            var codeClass = GetClassByFilename(className);

            if (codeClass != null)
                return codeClass;

            return GetClass(className, ModifierEnum.Public, null);
        }

        private static ClassDeclarationSyntax GetClass(string className, ModifierEnum modifier, List<string>? baseClassList = null)
        {
            //  Create a class
            var classDeclaration = SyntaxFactory.ClassDeclaration(className);

            // Add modifier
            var modifierToken = GetModifier(modifier);
            classDeclaration = classDeclaration.AddModifiers(modifierToken);

            // Add Inherited base types if provided.
            AddBaseClass(classDeclaration, baseClassList);

            return classDeclaration;
        }

        private static ClassDeclarationSyntax AddBaseClass(ClassDeclarationSyntax classDeclaration, List<string>? baseClassList = null)
        {
            if (baseClassList == null)
                return classDeclaration;
            
            var baseTypeList = new List<BaseTypeSyntax>();

            foreach (var baseClass in baseClassList)
            {
                baseTypeList.Add(SyntaxFactory.SimpleBaseType(SyntaxFactory.ParseTypeName(baseClass)));
            }

            classDeclaration = classDeclaration.AddBaseListTypes(baseTypeList.ToArray());

            return classDeclaration;
        }

        #endregion

        #region "Var / Field"

        private static VariableDeclarationSyntax AddVariable(string varType, string varName)
        {
            // Create a string variable: (bool canceled;)
            var variableDeclaration = SyntaxFactory.VariableDeclaration(SyntaxFactory.ParseTypeName(varType))
                .AddVariables(SyntaxFactory.VariableDeclarator(varName));

            return variableDeclaration;
        }

        private static FieldDeclarationSyntax AddField(VariableDeclarationSyntax varDecl, ModifierEnum modifier = ModifierEnum.Private)
        {
            // Create a field declaration: (private bool canceled;)
            var fieldDeclaration = SyntaxFactory.FieldDeclaration(varDecl)
                .AddModifiers(GetModifier(modifier));

            return fieldDeclaration;
        }

        #endregion

        #region "Property"

        private static PropertyDeclarationSyntax AddProperty(string propType, string propName)
        {
            return AddProperty(propType, propName, ModifierEnum.Public);
        }

        private static PropertyDeclarationSyntax AddProperty(string propType, string propName, ModifierEnum modifier)
        {
            // Create a Property: (public int Quantity { get; set; })
            var propertyDeclaration = SyntaxFactory.PropertyDeclaration(SyntaxFactory.ParseTypeName(propType), propName)
                .AddModifiers(GetModifier(modifier))
                .AddAccessorListAccessors(
                    SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration).WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)),
                    SyntaxFactory.AccessorDeclaration(SyntaxKind.SetAccessorDeclaration).WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)));
           
            return propertyDeclaration;
        }

        #endregion

        #region "Method"

        private static ClassDeclarationSyntax AddFunction(ClassDeclarationSyntax classDeclaration, string? funcName, List<Entry> codeList)
        {
            if (funcName == null)
                return classDeclaration;

            var stmtList = new List<StatementSyntax>();
            var docList = new List<SyntaxTrivia>();

            foreach (var entry in codeList)
            {
                var code = entry.Value;
                switch (entry.TagType)
                {
                    case TagTypeEnum.CodeComment:
                        docList.Add(GetComment(code));
                        break;
                    default:
                        stmtList.Add(GetStatement(code));
                        break;
                }
            }

            var method = AddMethod("void", funcName, stmtList, docList);

            return classDeclaration.AddMembers(method);
        }

        private static MethodDeclarationSyntax AddMethod(string methodType, string methodName, 
            List<StatementSyntax> methodBody, List<SyntaxTrivia> comments,
            ModifierEnum modifier = ModifierEnum.Public)
        {
            // Create a method
            var methodDeclaration = SyntaxFactory.MethodDeclaration(SyntaxFactory.ParseTypeName(methodType), methodName)
                .AddModifiers(GetModifier(modifier))
                .WithBody(SyntaxFactory.Block(methodBody));

            if (comments.Count > 0)
                methodDeclaration = methodDeclaration.WithLeadingTrivia(comments);

            return methodDeclaration;
        }

        #endregion

        #region "Helper routines"

        private static SyntaxToken GetModifier(ModifierEnum modifier)
        {
            SyntaxToken modifierToken;
            
            switch (modifier)
            {
                case ModifierEnum.Protected:
                    modifierToken = SyntaxFactory.Token(SyntaxKind.ProtectedKeyword);
                    break;
                case ModifierEnum.Private:
                    modifierToken = SyntaxFactory.Token(SyntaxKind.PrivateKeyword);
                    break;
                default:
                    modifierToken = SyntaxFactory.Token(SyntaxKind.PublicKeyword);
                    break; 
            }

            return modifierToken;
        }

        private static StatementSyntax GetStatement(string value)
        {
            // Create a statement with the body of a method.
            return SyntaxFactory.ParseStatement(value);
        }

        private static SyntaxTrivia GetComment(string value)
        {
            return SyntaxFactory.Comment(value);
        }

        private static Entry ExtractCode(Entry entry)
        {
            var code = entry.Value;

            bool isComment = code.Contains("<%--");

            code = code.Replace("<%", "");
            code= code.Replace("%>", "");
            code = code.TrimStart();
            code = code.TrimEnd();

            if (!isComment)
            {
                if (!string.IsNullOrEmpty(code))
                    code = ProcessCode(entry, code);
            }
            else
            {
                entry.TagType = TagTypeEnum.CodeComment;
                entry.GroupName = Entry.GetGroupName(entry.TagType);
                code = "// " + code;
            }

            entry.Value = code;

            return entry;
        }

        private static string ProcessCode(Entry entry, string value)
        {
            switch (entry.TagType)
            {
                case TagTypeEnum.CodeOpen:
                case TagTypeEnum.CodeClose:
                case TagTypeEnum.CodeContent:
                case TagTypeEnum.CodeAttr:
                case TagTypeEnum.CodeValue:
                    return ProcessCodeBlock(entry, value);
                default:
                    return value;
            }
        }

        private static string ProcessCodeBlock(Entry entry, string value)
        {
            if (IsStatement(value)) return value;
            value = RenderHtml(value);
            return value;
        }

        private static string RenderHtml(string value)
        {
            return $"HttpContext.Current.Response.Write(\"{value}\");";
        }

        private static bool IsStatement(string stmt)
        {
            string code = "{};";
            char check = stmt[^1];

            return code.Contains(check);
        }

        #endregion

        #endregion

        #region "Html Generation"

        private static List<string> GenHtmlFile(ref List<Entry> htmlDom)
        {
            var ret = new List<string>();
            var codeFunc = string.Empty;
            var block = new StringBuilder();
            Entry? prevEntry = null;
            var isValue = false;

            foreach (var entry in htmlDom)
            {
                
                if (entry.FileType == AspFileEnum.Html)
                {
                    if (entry.TagType is TagTypeEnum.Close or TagTypeEnum.Open)
                        if (entry.Value.TrimStart().Length > 2)
                            RenderBlock(ref ret, ref block);
                    block.Append(FormatHtml(entry));
                }
                else
                {
                    var codeBlock = HandleCodeBlock(entry, ref codeFunc);
                    if (!string.IsNullOrEmpty(codeBlock))
                        block.Append("<% " + codeBlock + "; %>");
                }



                if (block.Length > 0 && entry.TagType != TagTypeEnum.Value)
                    if (block[^1] != ' ')
                        block.Append(" ");

                prevEntry = entry;

                if (entry.IsOpen)
                    continue;

                RenderBlock(ref ret, ref block);
            }
            return ret;
        }

        private static string FormatHtml(Entry entry)
        {
            switch (entry.TagType)
            {
                case TagTypeEnum.Open:
                case TagTypeEnum.CodeOpen:
                    return (entry.HasAttr) ? $"<{entry.Value} ": $"<{entry.Value}>";
                case TagTypeEnum.Close:
                case TagTypeEnum.CodeClose:
                    return SelfClosing(entry.Value) ? entry.Value : $"</{entry.Value}>";
                default:
                    return entry.Value;
            }
        }

        private static bool SelfClosing(string value)
        {
            var tags = new List<string> 
                { ">", "/>" };

            return tags.Contains(value);
        }

        [DebuggerStepThrough]
        private static void RenderBlock(ref List<string> blocks, ref StringBuilder block)
        {
            if (string.IsNullOrEmpty(block.ToString()))
                return;
            
            blocks.Add(block.ToString());
            block.Clear();
        }

        private static string HandleCodeBlock(Entry entry, ref string codeFunc)
        {
            if (string.IsNullOrEmpty(entry.CodeFunction))
                return string.Empty;

            if (string.IsNullOrEmpty(codeFunc))
            {
                codeFunc = entry.CodeFunction;
            }
            else
            {
                if (codeFunc == entry.CodeFunction)
                    return string.Empty;

                codeFunc = entry.CodeFunction;
            }
            return codeFunc;
        }

        #endregion
    }
}
