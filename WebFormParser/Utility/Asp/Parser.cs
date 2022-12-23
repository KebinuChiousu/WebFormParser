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
using System.Reflection.Metadata;

namespace WebFormParser.Utility.Asp
{
    public static class Parser
    {
        private const RegexOptions Options = RegexOptions.Multiline | RegexOptions.Compiled;

        public static List<Entry> ParseDocument(string input)
        {
            // Phase I - Identify Lines
            var nodes = ParseLines(input);

            if (nodes.Count == 0)
                return nodes;

            // Phase II - Identify Html
            nodes = ParseHtml(nodes);
            BypassComments(ref nodes);

            // Phase III - Identify Html that should be treated as code
            nodes = UpdateNodeClassification(nodes);

            // Phase IV - Add Code Function Names
            nodes = LabelCodeFunctions(nodes);

            // Phase V - Consolidate Node List
            nodes = ConsolidateNodes(nodes);

            return nodes;
        }

        private static void BypassComments(ref List<Entry> entries)
        {
            var entryCount = entries.Count;
            
            for (var idx = 0; idx < entryCount; idx++)
            {
                var entry = entries[idx];
                if (entry.TagType != TagTypeEnum.CodeComment)
                    continue;

                var comment = entry.Value;
                comment = comment.Replace("<%--", "<!--");
                comment = comment.Replace("--%>", "-->");

                entry.Value = comment;
                entry.TagType = TagTypeEnum.Comment;
                entry.FileType = AspFileEnum.Html;

                if (idx == 0) continue;

                var prevEntry = entries[idx - 1];

                if (prevEntry.TagType != TagTypeEnum.Comment)
                    continue;

                prevEntry.Children.Add(entry);
                entries.RemoveAt(idx);
                idx--;
                entryCount--;
            }
        }

        #region "Phase I - Identify Lines"

        private static List<Entry> ParseLines(string input)
        {
            var nodes = new List<Entry>();
            var lines = input.Split(Environment.NewLine);

            if (lines.Length == 1)
                return nodes;
            
            var aspx = input.Replace(Environment.NewLine, "");
            aspx = aspx.Replace("<%--", "<!--");
            aspx = aspx.Replace("--%>", "-->");
            aspx = aspx.Replace("\t", "");

            var preTag = aspx.Split("<").ToList();
            preTag.RemoveAt(0);
            
            for (var idx = 0; idx < preTag.Count; idx++)
                preTag[idx] = "<" + preTag[idx];

            var tags = new List<string>();

            foreach (var tag in preTag)
            {
                lines = tag.Split(">");
                for (var idx2 = 0; idx2 < lines.Length; idx2++)
                {
                    var value = lines[idx2];
                    if ((value.StartsWith("<") || value == "--") && (!value.StartsWith("<!") || value.EndsWith("--")))
                        lines[idx2] = value + ">";
                }

                foreach (var line in lines)
                {
                    var value = line.TrimStart().TrimEnd();
                    if (value.Length == 0)
                        continue;
                    tags.Add(value);
                }
            }
            
            var state = new State();

            foreach (var line in tags)
            {
                var entry = new Entry();
                if (line.StartsWith("<%") || state.IsCode)
                {
                    entry.FileType = AspFileEnum.CodeBehind;
                    state.IsCode = true;
                }
                entry.Value = line;
                entry.InnerText = line;
                HandleCodeState(ref state, entry);
                ClassifyNode(ref entry);
                nodes.Add(entry);
            }

            nodes = CategorizeNodes(nodes);

            return nodes;
        }

        public static void ClassifyNode(ref Entry entry)
        {
            const string pattern = @"(?'open'<[a-zA-Z]+\s[0-9A-Za-z=""' ]+|<[A-Za-z]+>|<[A-Za-z]+\s)|(?'close'</[A-Za-z]+>|</[A-Za-z]+>)|(?'comment'<!--.+-->|<!--[\s\S.]+-->)";
            var result = Util.GetRegexGroupMatches(entry.Value, pattern);

            if (result.Count == 0)
                return;

            result[0].InnerText = entry.InnerText;

            entry = result[0];

            entry.Value = entry.InnerText;

            entry.TagType = entry.GroupName switch
            {
                "open" => TagTypeEnum.Open,
                "close" => TagTypeEnum.Close,
                "comment" => TagTypeEnum.Comment,
                _ => TagTypeEnum.Content
            };
        }

        public static List<Entry> CategorizeNodes(List<Entry> entries)
        {
            var nodes = new List<Entry>();

            var state = new State();

            foreach (var entry in entries)
            {
                var node = entry;
                node = HandleNode(ref state, entry);
                nodes.Add(node);
            }

            return nodes;
        }

        public static Entry HandleNode(ref State state, Entry entry)
        {
            var value = entry.Value;
            state.IsCode = entry.FileType == AspFileEnum.CodeBehind;

            if (value.StartsWith("<!--")) state.IsComment = true;

            if (state.IsCode)
                entry.TagType = (state.IsComment) ? TagTypeEnum.CodeComment : TagTypeEnum.CodeContent;
            else
                entry.TagType = (state.IsComment) ? TagTypeEnum.Comment: TagTypeEnum.Content;
                                                                                             
            if (value.Contains("-->")) state.IsComment = false;

            if (!value.StartsWith("<%@ Page"))
                return entry;

            state.IsComment = false;
            entry.FileType = AspFileEnum.Html;
            entry.TagType = TagTypeEnum.Page;

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
            var allProps = ele.GetType().
                GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).
                OrderBy(pi => pi.Name).ToList();
            var propInfo = ele.GetType().
                GetProperties(BindingFlags.NonPublic | BindingFlags.Instance).
                Single(pi => pi.Name == "Attributes");
            var attributes = (IEnumerable<object>)propInfo.GetValue(ele, null);

            var dict = new Dictionary<string, string>();

            foreach (AngleSharp.Dom.Attr attr in attributes)
            {
                if (!dict.ContainsKey(attr.Name))
                    dict.Add(attr.Name, attr.Value);
            }

            entry.Attributes = dict;
        }

        private static void HandleNode(ref List<Entry> entries, ref State state, INode? node)
        { ;
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
            HandlePartialTag(ref entry, node);

            entries.Add(entry);

            if (node.NodeType == NodeType.Comment)
                return;

            var children = entry.Children;

            foreach (var child in node.ChildNodes)
            {
                HandleNode(ref children, ref state, child);
            }
        }

        private static void HandlePartialTag(ref Entry entry, INode node)
        {
            if (node.HasChildNodes)
                return;

            if (node.NodeName != "TABLE")
                return;

            entry.FileType = AspFileEnum.CodeBehind;

            switch (node.NodeType)
            {
                case NodeType.Element:
                    entry.TagType = TagTypeEnum.CodeOpen;
                    break;
                default:
                    entry.TagType = TagTypeEnum.CodeContent;
                    break;
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

        #region "Phase III - Complete Code Blocks"

        private static List<Entry> UpdateNodeClassification(List<Entry> entries)
        {
            var nodes = new List<Entry>();

            var state = new State();

            foreach (var entry in entries)
            {
                if (entry.Value.StartsWith("<script"))
                {
                    state.IsCode = false;
                    state.IsScript = true;
                }
                
                if (entry.FileType == AspFileEnum.CodeBehind)
                {
                    state.IsCode = true;
                    HandleCodeState(ref state, entry);
                    nodes.Add(entry);
                    continue;
                }

                if (!state.IsCode || state.IsScript)
                {
                    nodes.Add(entry);
                    if (entry.Value.StartsWith("</script>"))
                        state.IsScript = false;
                    continue;
                }

                entry.FileType = AspFileEnum.CodeBehind;
                entry.TagType = GetTagType(entry, state);
                nodes.Add(entry);

            }

            return nodes;
        }

        #endregion

        #region "Phase IV - Add Code Function Name"

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

        #region "Phase IV - Consolidate Node List"

        private static List<Entry> ConsolidateNodes(List<Entry> entries)
        {
            var nodes = new List<Entry>();
            var entryCount = entries.Count;

            for (var idx = 0; idx < entryCount; idx++)
            {
                var entry = entries[idx];

                if (entry is not { NeedsChildren: true, HasChildren: false })
                {
                    nodes.Add(entry);
                    continue;
                }

                entry.Children.Add(entries[idx +1]);
                entries.RemoveAt(idx + 1);
                entryCount--;
                nodes.Add(entry);
            }

            if (nodes.Count == 0)
                return nodes;

            if (nodes[^1].TagType != TagTypeEnum.Content)
                return nodes;

            // Last entry should always be an HTML Tag not content.
            nodes[^2].Children.Add(nodes[^1]);
            nodes.RemoveAt(nodes.Count - 1);

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
            HandleCodeState(ref state, entry);

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

            HandleCodeState(ref state, entry);
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
        }

        private static TagTypeEnum GetTagType(Entry entry, State state)
        {
            switch (entry.TagType)
            {
                case TagTypeEnum.Comment:
                    return TagTypeEnum.Comment;
                case TagTypeEnum.Open:
                    return state.IsCode ? TagTypeEnum.CodeOpen : TagTypeEnum.Open;
                case TagTypeEnum.Close:
                    return state.IsCode ? TagTypeEnum.CodeClose : TagTypeEnum.Close;
                case TagTypeEnum.Content:
                    return state.IsCode ? TagTypeEnum.CodeContent : TagTypeEnum.Content;
                case TagTypeEnum.Page:
                    return TagTypeEnum.Page;
            }

            return TagTypeEnum.Content;
        }

        private static bool IsCode(string value)
        {
            if (string.IsNullOrEmpty(value))
                return false;

            var isCode = value.Contains("<%") || value.Contains("%>");
            var isPage = value.Contains("<%@");

            return isCode && !isPage;
        }

        [DebuggerStepThrough]
        private static void HandleCodeState(ref State state, Entry entry)
        {
            var block = entry.InnerText;
            state.OpenCode += block.Count(f => f == '{');
            state.CloseCode += block.Count(f => f == '}');
            state.HandleCodeState(block);
        }
    }
}
