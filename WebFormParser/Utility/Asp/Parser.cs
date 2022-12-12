using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using WebFormParser.model;
using WebFormParser.Utility.Asp.Enum;

namespace WebFormParser.Utility.Asp
{
    public static class Parser
    {
        private const RegexOptions Options = RegexOptions.Multiline | RegexOptions.Compiled;

        public static List<Entry> ParseDocument(string input)
        {
            var htmlDoc = new HtmlAgilityPack.HtmlDocument();
            htmlDoc.LoadHtml(input);
            var htmlNodes = htmlDoc.DocumentNode.ChildNodes;
            var nodes = new List<Entry>();

            var state = new State();

            foreach (var node in htmlNodes)
            {
                HandleNode(ref nodes, ref state, node);
            }

            return nodes;
        }

        private static void HandleNode(ref List<Entry> entries, ref State state, HtmlNode node)
        {
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

            entry.IsOpen = node.HasAttributes;
            entry.HasAttr = node.HasAttributes;

            entries.Add(entry);

            if (node.HasAttributes) ProcessAttributes(ref entries, ref state, node);

            if (!node.HasChildNodes)
            {
                if (!node.HasAttributes)
                    return;

                CloseOpenTag(ref entries, entry, state);
                if (entry.SelfClosing())
                    return;
                
                closingTag = GetClosingTag(node.Name, state);
                entries.Add(closingTag);
            }

            if (node.HasAttributes)
                CloseOpenTag(ref entries, entry, state);
            
            foreach (var child in node.ChildNodes)
            {
                HandleNode(ref entries, ref state, child);
            }
            
            closingTag = GetClosingTag(node.Name, state);
            entries.Add(closingTag);
        }

        private static void CloseOpenTag( ref List<Entry> entries, Entry node, State state)
        {
            bool closed = node.SelfClosing();
            
            var entry = new Entry
            {
                Value = closed ? "/>" : ">",
                TagType = Entry.GetTagType(TagTypeEnum.Close, state.IsCode),
                IsOpen = !closed
            };
            entry.GroupName = Entry.GetGroupName(entry.TagType);
            entries.Add(entry);
        }

        private static void ProcessAttributes(ref List<Entry> entries, ref State state, HtmlNode node)
        {
            foreach (var attr in node.Attributes)
            {
                ProcessAttr(ref entries, ref state, attr);
            }
        }

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

        private static void ProcessCodeBlock(ref Entry entry, ref State state, HtmlNode node)
        {
            var block = entry.Value;
            var begIdx = block.IndexOf("<%") - 1;
            if (begIdx > 0) block = block.Substring(begIdx);
            var endIdx = block.IndexOf("%>") + 1;
            if (endIdx > 0) block = block.Substring(1, endIdx);
            entry.Value = block;

            entry.CodeFunction = "page_logic_" + state.FuncCount.ToString("D2");

            if (node.NodeType == HtmlNodeType.Text)
            {
                entry.TagType = TagTypeEnum.CodeContent;
                entry.GroupName = Entry.GetGroupName(entry.TagType);
            }

            HandleCodeState(ref state, entry.Value);
        }

        private static Entry GetClosingTag(string name, State state)
        {
            var tagType = state.IsCode ? TagTypeEnum.CodeClose : TagTypeEnum.Close;

            var entry = new Entry()
            {
                Value = name,
                TagType = tagType,
                GroupName = Entry.GetGroupName(tagType),
                FileType = state.IsCode ? AspFileEnum.CodeBehind : AspFileEnum.Html
            };

            if (state.IsCode)
                entry.CodeFunction = "page_logic_" + state.FuncCount.ToString("D2");

            return entry;
        }
        private static void HandleCodeState(ref State state, string block)
        {
            state.OpenCode += block.Count(f => f == '{');
            state.CloseCode += block.Count(f => f == '}');
            state.HandleCodeState(block);
        }

        private static string FormatValue(Entry entry, string value)
        {
            var ret = value;
            switch (entry.TagType)
            {
                case TagTypeEnum.Page:
                    ret = value.Replace(Environment.NewLine, "");
                    return ret;
                default:
                    return ret;
            }
        }

        private static string GetValue(HtmlNode node)
        {
            var pattern = @"[\p{C}-[\r\n\t]]+";
            var value = string.Empty;
            
            switch (node.NodeType)
            {
                case HtmlNodeType.Comment:
                    value = node.InnerHtml;
                    break;
                case HtmlNodeType.Element:
                    value = node.Name;
                    break;
                default:
                    value = node.InnerText;
                    break;
            }

            value = Regex.Replace(value, pattern, string.Empty);
            if (value == Environment.NewLine) value = string.Empty;

            return value;
        }

        private static TagTypeEnum GetTagType(HtmlNode node, State state)
        {
            switch (node.NodeType)
            {
                case HtmlNodeType.Comment:
                    return state.IsCode ? TagTypeEnum.CodeComment : TagTypeEnum.Comment;
                case HtmlNodeType.Text:
                    return CategorizeText(node, state);
                case HtmlNodeType.Element:
                    return state.IsCode ? TagTypeEnum.CodeOpen : TagTypeEnum.Open;
                default:
                    return state.IsCode ? TagTypeEnum.CodeContent : TagTypeEnum.Content;
            }
        }

        private static TagTypeEnum CategorizeText(HtmlNode node, State state)
        {
            var block = node.InnerText;
            if (block.Contains("<%@")) return TagTypeEnum.Page;
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
    }
}
