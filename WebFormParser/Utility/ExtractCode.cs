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

        public static Hashtable ParseCode(string fileName)
        {
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
                bool skip = ParseLine(line, ref openTag, ref aspx);
                if (skip)
                    continue;

                if (ProcessLine(line, ref block, ref isCode, ref aspx))
                {
                    blockCnt += 1;
                    var blockStr = "Render_Logic_" + blockCnt.ToString("D2");
                    blocks.Add(blockStr, block);
                    aspx.Insert(aspx.Count - 1," " + blockStr + "();");
                    block = new List<string>();

                }
            }
            return blocks;
        }

        private static bool ParseLine(string line, ref bool openTag, ref List<string> output)
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

            bool check = (ret || openTag);

            if (check)
                output.Add(line);

            return check;
        }

        private static bool ProcessLine(string line, ref List<string> block, ref bool isCode, ref List<string> output)
        {
            int startIdx = 0;
            int len = -1;

            bool ret = false;

            string content = string.Empty;

            if (line.Contains("<%"))
            {
                output.Add(line);
                isCode = true;
            }

            if (!isCode)
                return false;

            content = line.Replace("<%", "");
            content = content.Replace("%>", "");
            content = content.Trim();

            if (isCode && !string.IsNullOrEmpty(content))
                block.Add(content);

            if (line.Contains("%>"))
            {
                output.Add(line);
                isCode = false;
                ret = true;
            }

            return ret;
        }
    }
}
