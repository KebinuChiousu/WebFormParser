using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace WebFormParser.Utility
{
    public static class ExtractCode
    {
        private static string _newLine = Environment.NewLine;

        public static List<List<string>> ParseCode(string fileName)
        {
            var source = File.ReadAllText(fileName);
            var lines = source.Split(_newLine, StringSplitOptions.None);
            var blocks = new List<List<string>>();
            var block = new List<string>();
            var openTag = false;
            var isCode = false;

            foreach (var line in lines)
            {
                bool skip = ParseLine(line, ref openTag);
                if (skip)
                    continue;

                if (ProcessLine(line, ref block, ref isCode))
                {
                    blocks.Add(block);
                    block = new List<string>();
                }
            }
            return blocks;
        }

        private static bool ParseLine(string line, ref bool openTag)
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
            
            return (ret || openTag);
        }

        private static bool ProcessLine(string line, ref List<string> block, ref bool isCode)
        {
            int startIdx = 0;
            int len = -1;

            bool ret = false;

            string content = string.Empty;

            if (line.Contains("<%"))
                isCode = true;

            if (!isCode)
                return false;

            content = line.Replace("<%", "");
            content = content.Replace("%>", "");
            content = content.Trim();

            if (isCode && !string.IsNullOrEmpty(content))
                block.Add(content);

            if (line.Contains("%>"))
            {
                isCode = false;
                ret = true;
            }

            return ret;
        }
    }
}
