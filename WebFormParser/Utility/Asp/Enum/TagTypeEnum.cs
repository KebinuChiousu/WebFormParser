using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommandLine;

namespace WebFormParser.Utility.Asp.Enum
{
    public enum TagTypeEnum
    {
        Comment = 0,
        Content = 1,
        Open = 2,
        Close = 3,
        Attr = 4,
        Value = 5,
        DocType = 8,
        Page = 9,
        CodeComment = 10,
        CodeContent = 11,
        CodeOpen = 12,
        CodeClose = 13,
        CodeAttr = 14,
        CodeValue = 15
    }
}
