using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using WebFormParser.model;
using WebFormParser.Utility.Asp;
using WebFormParser.Utility.Asp.Enum;

namespace WebFormParser.Utility.Legacy
{
    public static class Parser
    {
        private const string Pattern = @"(?'open'<[a-zA-Z]+\s[0-9A-Za-z=""' ]+|<[A-Za-z]+>|<[A-Za-z]+\s)|((?'code'<%\n([^%>]|\n)+%>|<%([^%>]|\n)+%>)|(?'attr'[a-zA-Z0-9]+|[0-9A-Za-z=""']+))|(?'close'>|>([^<]|\n)+</[A-Za-z]+>|/>|[0-9A-Za-z=""'\n/ ]+>|</[A-Za-z]+>)|(?'comment'<!--.+-->|<!--[\s\S.]+-->)";
        private const RegexOptions Options = RegexOptions.Multiline | RegexOptions.Compiled;

        public static List<Entry> GetRegexGroupMatches(string input)
        {
            var ret = new List<Entry>();
            var nodes = new List<Entry>();
            var regex = new Regex(Pattern, Options);
            var groupList = GetRegexGroupNames(regex);
            var mc = regex.Matches(input); 
            foreach (Match m in mc)
            {
                var groupName = GetGroupNameForMatch(groupList, m);
                var matchValue = FormatValue(m.Value, groupName);
                var entry = new Entry
                {
                    GroupName = groupName,
                    Value = matchValue
                };
                nodes.Add(entry);
            }

            nodes = CategorizeEntries(nodes);

            ret = MergeNodes(nodes);
            
            return ret;
        }

        private static List<Entry> MergeNodes(List<Entry> nodes)
        {
            for(var i = 0; i < nodes.Count; i++)
            {
                var node = nodes[i];
                var prevNode = i > 0 ? nodes[i - 1] : null;

                if (prevNode == null )
                    continue;

                if (node.TagType is not (TagTypeEnum.Attr or TagTypeEnum.CodeValue))
                    continue;

                if (prevNode.TagType is not (TagTypeEnum.Attr or TagTypeEnum.CodeValue or TagTypeEnum.CodeOpen or TagTypeEnum.CodeContent))
                    continue;

                if (prevNode.TagType is (TagTypeEnum.CodeOpen or TagTypeEnum.CodeContent))
                { ;
                    node.TagType = TagTypeEnum.CodeContent;
                    node.GroupName = Entry.GetGroupName(node.TagType);
                }

                if (prevNode.TagType == TagTypeEnum.CodeOpen)
                    continue;

                prevNode.Value += (node.TagType == TagTypeEnum.CodeContent) ? " " + node.Value : node.Value;

                nodes.RemoveAt(i);
                i--;
            }

            return nodes;
        }

        private static List<Entry> CategorizeEntries(List<Entry> entries)
        {
            var ret = new List<Entry>();
            var state = new State();

            foreach (var entry in entries)
            {

                state.PrevCode = state.IsCode;
                
                switch (entry.GroupName)
                {
                    case "open":
                        state.IsTag = true;
                        state.IsOpen = !entry.Value.Contains(">");
                        break;
                    case "close":
                        state.IsTag = false;
                        state.IsOpen = false;
                        break;
                    case "code":
                        state.IsCode = true;
                        break;
                }

                var tag = HandleEntry(entry, ref state);

                ret.Add(tag);
            }

            return ret;
        }

        private static Entry HandleEntry(Entry entry, ref State state)
        {
            entry.FileType = state.IsCode ? AspFileEnum.CodeBehind : AspFileEnum.Html;

            SetTagType(ref entry, state);

            switch (entry.GroupName)
            {
                case "attr":
                    if (!state.IsOpen) entry.TagType = TagTypeEnum.Content;
                    if (state.IsCode)
                    {
                        entry.TagType = state.IsOpen ? TagTypeEnum.CodeAttr : TagTypeEnum.CodeContent;
                        entry.TagType = InCodeBlock(entry); 
                        entry.CodeFunction = "page_logic_" + state.FuncCount.ToString("D2");
                    }
                    break;
                case "code":
                    if (state.IsOpen)
                    {
                        entry.TagType = TagTypeEnum.CodeAttr;
                        if (!state.PrevCode)
                            state.FuncCount++;
                        entry.CodeFunction = "page_logic_" + state.FuncCount.ToString("D2");
                    }
                    if (entry.Value.Contains("<%@"))
                    {
                        entry.TagType = TagTypeEnum.Page;
                        entry.FileType = AspFileEnum.Html;
                        state.IsCode = false;
                    }

                    if (entry.FileType == AspFileEnum.CodeBehind)
                    {
                        if (entry.TagType == TagTypeEnum.Content)
                        {
                            entry.TagType = TagTypeEnum.CodeContent;
                            if (!state.PrevCode)
                                state.FuncCount++;
                        }

                        entry.CodeFunction = "page_logic_" + state.FuncCount.ToString("D2");
                    }
                    break;
                case "open":
                    if (state.IsCode)
                    {
                        entry.TagType = TagTypeEnum.CodeOpen;
                        if (!state.PrevCode)
                            state.FuncCount++;
                        entry.CodeFunction = "page_logic_" + state.FuncCount.ToString("D2");
                    }
                    break;
                case "close":
                    if (state.IsCode)
                    {
                        entry.TagType = TagTypeEnum.CodeClose;
                        entry.CodeFunction = "page_logic_" + state.FuncCount.ToString("D2");
                    }
                    break;
            }

            SetGroupName(ref entry);

            if (!state.IsCode)
                return entry;

            state.OpenCode += entry.Value.Count(f => f == '{');
            state.CloseCode += entry.Value.Count(f => f == '}');
            state.HandleCodeState(entry.Value);

            return entry;
        }

        private static TagTypeEnum InCodeBlock(Entry entry)
        {
            var isCode = entry.Value.Contains("<%") || entry.Value.Contains("%>");
            return isCode ? TagTypeEnum.CodeAttr : TagTypeEnum.CodeValue;
        }

        private static void SetGroupName(ref Entry entry)
        {
            entry.GroupName = Entry.GetGroupName(entry.TagType);
        }

        private static void SetTagType(ref Entry entry, State state)
        {
            entry.TagType = entry.GroupName switch
            {
                "attr" => TagTypeEnum.Attr,
                "open" => TagTypeEnum.Open,
                "close" => TagTypeEnum.Close,
                "comment" => TagTypeEnum.Comment,
                _ => TagTypeEnum.Content
            };

            entry.IsOpen = state.IsOpen;
        }

        private static List<string> GetRegexGroupNames(Regex regex)
        {
            var groupList = regex.GetGroupNames().ToList();
            return groupList.Where( t => !int.TryParse( t , out var i)).ToList();
        }

        private static string GetGroupNameForMatch(List<string> groupNames, Match m)
        {
            var ret = string.Empty;
            var groupList = m.Groups.Keys.Where(t => m.Groups[t].Success).Where(t => !int.TryParse(t, out var i)).ToList();
            if (groupList.Count > 0)
                ret = groupList[0];

            return ret;
        }

        private static string FormatValue(string value, string groupName)
        {
            switch (groupName)
            {
                case "code":
                    return FormatCode(value);
                case "comment":
                    return FormatCode(value);
                default:
                    return value;
            }
        }

        private static string FormatCode(string value)
        {
            var lineList = new List<string>();
            var lines = value.Split(Environment.NewLine);
            foreach(var line in lines)
                lineList.Add(line.Trim());
            return string.Join(" ", lineList);
        }

    }
}
