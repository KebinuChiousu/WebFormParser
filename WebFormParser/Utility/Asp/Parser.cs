using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using WebFormParser.model;
using WebFormParser.Utility.Asp.Enum;
using AngleSharp;
using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using HtmlAgilityPack;
using System.Reflection;

namespace WebFormParser.Utility.Asp
{
    public static class Parser
    {
        private const RegexOptions Options = RegexOptions.Multiline | RegexOptions.Compiled;

        public static List<Entry> ParseDocument(string input)
        {
            var nodes = ParseLines(input);

            if (nodes.Count == 0)
                return nodes;

            nodes = ParseHtml(nodes);
            nodes = LabelCodeFunctions(nodes);
            return nodes;
        }

        #region "Phase I - Identify Lines"

        private static List<Entry> ParseLines(string input)
        {
            var nodes = new List<Entry>();
            var lines = input.Split(Environment.NewLine);

            if (lines.Length == 1)
                return nodes;

            var state = new State();

            foreach (var line in lines)
            {
                var entry = new Entry();
                if (line.StartsWith("<%") || state.IsCode)
                {
                    entry.FileType = AspFileEnum.CodeBehind;
                    state.IsCode = true;
                    HandleCodeState(ref state, line);
                }
                entry.Value = line;
                nodes.Add(entry);
            }

            nodes = CategorizeNodes(nodes);

            return nodes;
        }

        public static List<Entry> CategorizeNodes(List<Entry> entries)
        {
            var nodes = new List<Entry>();

            var state = new State();

            foreach (var entry in entries)
            {
                var value = entry.Value;

                if (value.Length <= 2)
                    continue;

                switch (entry.FileType)
                {
                    case AspFileEnum.CodeBehind:
                        nodes.Add(HandleCodeNode(ref state, entry));
                        break;
                    default:
                        nodes.Add(HandleHtmlNode(ref state, entry));
                        break;
                }
            }

            return nodes;
        }

        public static Entry HandleCodeNode(ref State state, Entry entry)
        {
            var value = entry.Value;

            if (value.StartsWith("<%--")) state.IsComment = true;

            entry.TagType = (state.IsComment) ? TagTypeEnum.CodeComment : TagTypeEnum.CodeContent;

            if (value.Contains("--%>")) state.IsComment = false;

            if (!value.StartsWith("<%@ Page"))
                return entry;

            state.IsComment = false;
            entry.FileType = AspFileEnum.Html;
            entry.TagType = TagTypeEnum.Page;

            return entry;
        }

        public static Entry HandleHtmlNode(ref State state, Entry entry)
        {
            var value = entry.Value;

            if (value.StartsWith("<!--")) state.IsComment = true;

            entry.TagType = (state.IsComment) ? TagTypeEnum.Comment : TagTypeEnum.Content;

            if (value.Contains("-->")) state.IsComment = false;

            return entry;

        }

        #endregion

        #region "Phase II - Identify Html"

        public static List<Entry> ParseHtml(List<Entry> entries)
        {
            var nodes = new List<Entry>();

            foreach (var entry in entries)
            {
                if (entry.FileType == AspFileEnum.CodeBehind || entry.TagType == TagTypeEnum.Page)
                {
                    nodes.Add(entry);
                    continue;
                }

                ParseHtmlFragment(ref nodes, entry.Value);
            }

            return nodes;
        }

        public static void ParseHtmlFragment(ref List<Entry> entries, string source)
        {
            var parser = new HtmlParser();
            var htmlDoc = parser.ParseDocument(source);

            if (htmlDoc.Body == null)
                return;

            if (!htmlDoc.HasChildNodes)
                return;

            var htmlNode = htmlDoc.ChildNodes[0];

            var nodes = GetNodes(htmlNode.ChildNodes);

            if (nodes == null) return;

            var state = new State();

            foreach (var node in nodes)
            {
                HandleNode(ref entries, ref state, node);
            }

        }

        /// <summary>
        /// Return Elements of Either Head or Body Tag.
        /// </summary>
        /// <param name="nodes"></param>
        /// <returns></returns>
        private static INodeList? GetNodes(INodeList nodes)
        {
            if (nodes.Length == 0)
                return null;

            var headCount = nodes[0].ChildNodes.Length;
            return headCount > 0 ? nodes[0].ChildNodes : nodes[1].ChildNodes;
        }

        private static void GetAttributes(ref Entry entry, INode node)
        {
            if (node.NodeType != NodeType.Element) return;

            var ele = (IElement)node;
            var allProps = ele.GetType().GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).OrderBy(pi => pi.Name).ToList();
            PropertyInfo propInfo = ele.GetType().GetProperties(BindingFlags.NonPublic | BindingFlags.Instance).Single(pi => pi.Name == "Attributes");
            IEnumerable<object> attributes = (IEnumerable<object>)propInfo.GetValue(ele, null);

            var dict = new Dictionary<string, string>();

            foreach (AngleSharp.Dom.Attr attr in attributes)
            {
                if (!dict.ContainsKey(attr.Name))
                    dict.Add(attr.Name, attr.Value);
            }

            entry.Attributes = dict;
        }

        private static void HandleNode(ref List<Entry> entries, ref State state, INode? node)
        {

            var hasAttr = false;
            if (node == null)
                return;

            Entry closingTag;
            var block = GetValue(node);

            if (string.IsNullOrEmpty(block))
                return;

            var entry = new Entry();

            if (IsCode(block)) state.IsCode = true;

            entry.TagType = GetTagType(node, state);
            entry.Value = FormatValue(entry, block);
            entry.GroupName = Entry.GetGroupName(entry.TagType);
            entry.FileType = state.IsCode ? AspFileEnum.CodeBehind : AspFileEnum.Html;

            if (state.IsCode)
                ProcessCodeBlock(ref entry, ref state, node);

            GetAttributes(ref entry, node);

            entries.Add(entry);

            if (node.NodeType == NodeType.Comment)
                return;

            foreach (var child in node.ChildNodes)
            {
                HandleNode(ref entries, ref state, child);
            }
        }

        private static string GetValue(INode? node)
        {
            if (node == null)
                return string.Empty;

            var pattern = @"[\p{C}-[\r\n\t]]+";

            var value = node.NodeType switch
            {
                NodeType.Element => node.NodeName.ToLower(),
                _ => node.NodeValue
            };

            value = Regex.Replace(value, pattern, string.Empty);
            if (value == Environment.NewLine) value = string.Empty;

            return value;
        }

        #endregion

        #region "Phase III - Add Code Function Name"

        private static List<Entry> LabelCodeFunctions(List<Entry> entries)
        {
            var nodes = new List<Entry>();

            var funcCount = 1;

            for (var idx = 1; idx < entries.Count; idx++)
            {
                var prevEntry = entries[idx - 1];
                var entry = entries[idx];

                if (entry.FileType == AspFileEnum.CodeBehind)
                    entry.CodeFunction = $"render_logic_{funcCount:D2}";

                nodes.Add(entry);

                if (entry.FileType == AspFileEnum.Html)
                    if (prevEntry.FileType == AspFileEnum.CodeBehind)
                        funcCount++;
            }

            return nodes;
        }

        #endregion

        #region "Legacy Parser"

        private static void ProcessAttr(ref List<Entry> entries, ref State state, HtmlAttribute attr)
        {

            Entry? entry = null;
            var result = string.Empty;

            bool check1 = IsCode(attr.Name);
            bool check2 = IsCode(attr.Value);

            bool isCode = check1 || check2;

            var blocks = new List<Attr>();

            if (check1)
                blocks.AddRange(GetBlocks(attr.Name, true));
            else
            {
                var value = attr.Name + "=";
                entry = AddAttr(value, isCode, true, ref state);
                if (entry != null && isCode) entries.Add(entry);
            }

            if (check2)
                blocks.AddRange(GetBlocks(attr.Value, false));
            else
            {
                var value = attr.Value;
                if (isCode)
                    entry = AddAttr(value, isCode, false, ref state);
                else
                    if (entry != null)
                    entry.Value += $"\"{value}\"";

                if (entry != null) entries.Add(entry);
            }

            foreach (var block in blocks)
            {
                var isCodeBlock = IsCode(block.Value);
                var isCodeCheck = isCodeBlock || state.IsCode;
                entry = AddAttr(block.Value, isCodeCheck, !block.IsValue, ref state);
                if (entry != null) entries.Add(entry);
            }
        }

        private static Entry? AddAttr(string value, bool isCode, bool isAttr, ref State state)
        {
            if (string.IsNullOrEmpty(value))
                return null;

            var entry = new Entry
            {
                Value = value,
                FileType = isCode ? AspFileEnum.CodeBehind : AspFileEnum.Html,
                TagType = isAttr ? TagTypeEnum.Attr : TagTypeEnum.Value,
                CodeFunction = isCode ? "page_logic_" + state.FuncCount.ToString("D2") : null,
                IsOpen = true
            };

            entry.TagType = Entry.GetTagType(entry.TagType, isCode);
            entry.GroupName = Entry.GetGroupName(entry.TagType);

            state.IsCode = isCode;
            HandleCodeState(ref state, entry.Value);

            return entry;
        }

        [System.Diagnostics.DebuggerStepThrough()]
        private static IEnumerable<Attr> GetBlocks(string block, bool isAttr)
        {
            const string pattern = @"<%[^%>]+%>";
            var regex = new Regex(pattern, Options);
            var mc = regex.Matches(block);
            foreach (Match m in mc)
            {
                block = block.Replace(m.Value, "<sep>" + m.Value + "<sep>");
            }

            var blocks = block.Split("<sep>").ToList();

            var result = new List<Attr>();

            foreach (var t in blocks)
            {
                if (string.IsNullOrEmpty(t))
                    continue;

                result.Add(new Attr(t, !isAttr));
            }

            return result;
        }

        private static void ProcessCodeBlock(ref Entry entry, ref State state, INode? node)
        {
            var block = entry.Value;
            var begIdx = block.IndexOf("<%") - 1;
            if (begIdx > 0) block = block.Substring(begIdx);
            var endIdx = block.IndexOf("%>") + 1;
            if (endIdx > 0) block = block.Substring(1, endIdx);
            entry.Value = block;

            entry.CodeFunction = "page_logic_" + state.FuncCount.ToString("D2");

            if (node.NodeType == NodeType.Text)
            {
                entry.TagType = TagTypeEnum.CodeContent;
                entry.GroupName = Entry.GetGroupName(entry.TagType);
            }

            HandleCodeState(ref state, entry.Value);
        }

        #endregion



        private static string FormatValue(Entry entry, string value)
        {
            var ret = value;

            var test = value.Replace(Environment.NewLine, "");

            if (string.IsNullOrEmpty(test))
                return string.Empty;

            switch (entry.TagType)
            {
                case TagTypeEnum.Page:
                    ret = value.Replace(Environment.NewLine, "");
                    return ret;
                default:
                    return ret;
            }
        }



        private static TagTypeEnum GetTagType(INode? node, State state)
        {
            if (node == null)
                return TagTypeEnum.Content;

            switch (node.NodeType)
            {
                case NodeType.Comment:
                    return state.IsCode ? TagTypeEnum.CodeComment : TagTypeEnum.Comment;
                case NodeType.Element:
                    return state.IsCode ? TagTypeEnum.CodeOpen : TagTypeEnum.Open;
                default:
                    return state.IsCode ? TagTypeEnum.CodeContent : TagTypeEnum.Content;
            }

            ;
        }

        private static bool IsCode(string value)
        {
            if (string.IsNullOrEmpty(value))
                return false;

            var isCode = value.Contains("<%") || value.Contains("%>");
            var isPage = value.Contains("<%@");

            return isCode && !isPage;
        }

        private static void HandleCodeState(ref State state, string block)
        {
            state.OpenCode += block.Count(f => f == '{');
            state.CloseCode += block.Count(f => f == '}');
            state.HandleCodeState(block);
        }
    }
}
