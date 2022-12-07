using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AngleSharp.Css.Values;
using WebFormParser.Utility.Asp.Enum;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

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

        private static List<string> GenCodeFile(List<Entry> codeDom, string className)
        {
            var ret = new List<string>();

            var ns = GetNamespace("WebForms");
            ns = AddUsing(ns, "System");
            ns = AddUsing(ns, "System.Web");

            var classDeclaration = GetClass(className);

            codeDom = codeDom.Where(e => e.FileType == AspFileEnum.CodeBehind).ToList();

            var codeFunc = string.Empty;
            var stmtList = new List<string>();
            var stmt = string.Empty;

            foreach (var entry in codeDom)
            {
                if (codeFunc != entry.CodeFunction)
                {
                    if (!string.IsNullOrEmpty(codeFunc))
                        classDeclaration = AddFunction(classDeclaration, codeFunc, stmtList);

                    stmtList.Clear();
                    codeFunc = entry.CodeFunction;
                }

                stmtList.Add(ExtractCode(entry));
            }

            if (stmtList.Count > 0)
                classDeclaration = AddFunction(classDeclaration, codeFunc, stmtList);

            // Add the class to the namespace.
            ns = ns.AddMembers(classDeclaration);

            // Normalize and get code as string.
            var code = ns
                .NormalizeWhitespace()
                .ToFullString();

            ret = code.Split(Environment.NewLine).ToList();

            return ret;
        }

        #region "Namespace Generation"

        private static NamespaceDeclarationSyntax GetNamespace(string ns)
        {
            // Create a namespace: (namespace CodeGenerationSample)
            return SyntaxFactory.NamespaceDeclaration(SyntaxFactory.ParseName(ns)).NormalizeWhitespace();
        }

        private static NamespaceDeclarationSyntax AddUsing(NamespaceDeclarationSyntax ns, string strUsing)
        {
            return ns.AddUsings(SyntaxFactory.UsingDirective(SyntaxFactory.ParseName(strUsing)));
        }

        #endregion

        #region "Class Generation"

        private static ClassDeclarationSyntax GetClass(string className)
        {
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

        private static ClassDeclarationSyntax AddFunction(ClassDeclarationSyntax classDeclaration, string? funcName, List<string> codeList)
        {
            if (funcName == null)
                return classDeclaration;

            var stmtList = new List<StatementSyntax>();

            foreach (var entry in codeList)
            {
                var stmt = GetStatement(entry);
                stmtList.Add(stmt);
            }

            var method = AddMethod("void", funcName, stmtList);

            return classDeclaration.AddMembers(method);
        }

        private static MethodDeclarationSyntax AddMethod(string methodType, string methodName, 
            List<StatementSyntax> methodBody, ModifierEnum modifier = ModifierEnum.Public)
        {
            // Create a method
            var methodDeclaration = SyntaxFactory.MethodDeclaration(SyntaxFactory.ParseTypeName(methodType), methodName)
                .AddModifiers(GetModifier(modifier))
                .WithBody(SyntaxFactory.Block(methodBody));

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

        private static StatementSyntax GetStatement(string statement)
        {
            // Create a statement with the body of a method.
            return SyntaxFactory.ParseStatement(statement);
        }

        private static string ExtractCode(Entry entry)
        {
            var code = entry.Value;
            code = code.Replace("<%", "");
            code= code.Replace("%>", "");
            code = code.TrimStart();
            code = code.TrimEnd();

            code = ProcessCode(entry, code);

            return code;
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
                
                if (prevEntry is { TagType: TagTypeEnum.Value }) isValue = true;

                if (isValue)
                    block.Append('"');
                
                if (entry.FileType == AspFileEnum.Html)
                {
                    if (entry.TagType is TagTypeEnum.Close or TagTypeEnum.Open)
                        if (entry.Value.TrimStart().Length > 2)
                            RenderBlock(ref ret, ref block);
                    block.Append(entry.Value);
                }
                else
                {
                    var codeBlock = HandleCodeBlock(entry, ref codeFunc);
                    if (!string.IsNullOrEmpty(codeBlock))
                        block.Append("<% " + codeBlock + "; %>");
                }

                if (isValue)
                {
                    block.Append('"');
                    isValue = false;
                }

                if (entry.Value == "value=")
                    entry.TagType = TagTypeEnum.Value;

                if (entry.TagType is (TagTypeEnum.Content or TagTypeEnum.CodeContent))
                    entry.IsOpen = true;

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
