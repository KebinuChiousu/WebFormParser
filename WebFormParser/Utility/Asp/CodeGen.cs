using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AngleSharp.Css.Values;
using WebFormParser.Utility.Asp.Enum;

namespace WebFormParser.Utility.Asp
{
    public static class CodeGen
    {
        public static List<string> Generate(ref List<Entry> codeDom, AspFileEnum mode = AspFileEnum.CodeBehind)
        {
            var ret = string.Empty;

            return mode switch
            {
                AspFileEnum.CodeBehind => GenCodeFile(codeDom),
                _ => GenHtmlFile(ref codeDom)
            };
        }

        private static List<string> GenCodeFile(List<Entry> codeDom)
        {
            var ret = new List<string>();

            return ret;
        }

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
    }
}
