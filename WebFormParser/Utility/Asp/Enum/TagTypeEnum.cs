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
        Content = 0,
        Comment = 1,
        Open = 2,
        Close = 3,
        Attr = 4,
        Page = 5,
        CodeContent = 10,
        CodeValue = 11,
        CodeOpen = 12,
        CodeClose = 13,
        CodeAttr = 14

    }
}
