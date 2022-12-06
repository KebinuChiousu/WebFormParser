using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.VisualBasic;
using WebFormParser.model;
using WebFormParser.Utility.Asp.Enum;

namespace WebFormParser.Utility.Asp
{
    public static class Parser
    {
        private const string Pattern = @"(?'open'<[a-zA-Z]+\s[0-9A-Za-z=""' ]+|<[A-Za-z]+>|<[A-Za-z]+\s)|((?'code'<%\n([^%>]|\n)+%>|<%([^%>]|\n)+%>)|(?'attr'[a-zA-Z0-9]+|[0-9A-Za-z=""']+))|(?'close'>|>([^<]|\n)+</[A-Za-z]+>|/>|[0-9A-Za-z=""'\n/ ]+>|</[A-Za-z]+>)|(?'comment'<!--.+-->|<!--[\s\S.]+-->)";
        private const RegexOptions Options = RegexOptions.Multiline;

        public static List<Entry> GetRegexGroupMatches(string input)
        {
            var ret = new List<Entry>();

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
                ret.Add(entry);
            }

            ret = CategorizeEntries(ret);

            return ret;
        }

        private static List<Entry> CategorizeEntries(List<Entry> entries)
        {
            var ret = new List<Entry>();
            var state = new State();

            foreach (var entry in entries)
            {
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

            SetTagType(ref entry);

            switch (entry.GroupName)
            {
                case "attr":
                    if (!state.IsOpen) entry.TagType = TagTypeEnum.Content;
                    if (state.IsCode)
                    {
                        entry.TagType = state.IsOpen ? TagTypeEnum.CodeValue : TagTypeEnum.CodeContent;
                        entry.CodeFunction = "page_logic_" + state.FuncCount;
                    }
                    break;
                case "code":
                    if (state.IsOpen) entry.TagType = TagTypeEnum.CodeAttr;
                    if (entry.Value.Contains("<%@"))
                    {
                        entry.TagType = TagTypeEnum.Page;
                        entry.FileType = AspFileEnum.Html;
                    }
                    if (entry.FileType == AspFileEnum.CodeBehind)
                        entry.CodeFunction = "page_logic_" + state.FuncCount;
                    break;
                case "open":
                    if (state.IsCode)
                    {
                        entry.TagType = TagTypeEnum.CodeOpen;
                        state.FuncCount++;
                        entry.CodeFunction = "page_logic_" + state.FuncCount;
                    }
                    
                    break;
                case "close":
                    if (state.IsCode)
                    {
                        entry.TagType = TagTypeEnum.CodeClose;
                        entry.CodeFunction = "page_logic_" + state.FuncCount;
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

        private static void SetGroupName(ref Entry entry)
        {
            entry.GroupName = entry.GetTagType(entry.TagType);
        }

        private static void SetTagType(ref Entry entry)
        {
            entry.TagType = entry.GroupName switch
            {
                "attr" => TagTypeEnum.Attr,
                "open" => TagTypeEnum.Open,
                "close" => TagTypeEnum.Close,
                "comment" => TagTypeEnum.Comment,
                _ => TagTypeEnum.Content
            };
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
                lineList.Add(Strings.Trim(line));
            return string.Join(" ", lineList);
        }

    }
}
