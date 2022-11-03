using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.VisualBasic;

namespace WebFormParser.Utility.Asp
{
    public static class Parser
    {
        private const string Pattern = @"(?'open'<[a-z]+\s[0-9a-z=""' ]+|<[a-z]+>|<[a-z]+\s)|((?'code'<%\n([^%>]|\n)+%>|<%([^%>]|\n)+%>)|(?'attr'[a-z]+|[0-9a-z=""']+))|(?'close'>|>([^<]|\n)+</[a-z]+>|/>|[0-9a-z=""'\n/ ]+>|</[a-z]+>)|(?'comment'<!--.+-->|<!--[\s\S.]+-->)";

        public static List<Entry> GetRegexGroupMatches(string input)
        {
            var ret = new List<Entry>();

            RegexOptions options = RegexOptions.Multiline;
            var regex = new Regex(Pattern, options);
            var groupList = GetRegexGroupNames(regex);
            MatchCollection mc = regex.Matches(input); 
            foreach (Match m in mc)
            {
                var groupName = GetGroupNameForMatch(groupList, m);
                var matchValue = FormatValue(m.Value, groupName);
                var entry = new Entry();
                entry.GroupName = groupName;
                entry.Value = matchValue;
                ret.Add(entry);
            }

            return ret;
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

        public class Entry
        {
            public string GroupName { get; set; }
            public string Value { get; set; }

            public Entry()
            {
                GroupName = "";
                Value = "";
            }
        }
    }
}
