﻿using WebFormParser.Utility.Asp.Enum;

namespace WebFormParser.Utility.Asp;

public class Entry
{
    public string GroupName { get; set; }
    public string Value { get; set; }
    public AspFileEnum FileType { get; set; }
    public TagTypeEnum TagType { get; set; }
    public string? CodeFunction { get; set; }

    public Entry()
    {
        GroupName = "";
        Value = "";
        FileType = AspFileEnum.Html;
    }

    public string GetFileType(AspFileEnum fileType)
    {
        return fileType == AspFileEnum.CodeBehind ? "Code" : "Html";
    }

    public string GetTagType(TagTypeEnum tagType)
    {
        return tagType switch
        {
            TagTypeEnum.Content => "content",
            TagTypeEnum.Comment => "comment",
            TagTypeEnum.Open => "tagOpen",
            TagTypeEnum.Close => "tagClose",
            TagTypeEnum.Attr => "tagAttr",
            TagTypeEnum.Page => "page",
            TagTypeEnum.CodeOpen => "codeOpen",
            TagTypeEnum.CodeClose => "codeClose",
            TagTypeEnum.CodeAttr => "codeAttr",
            TagTypeEnum.CodeValue => "codeValue",
            TagTypeEnum.CodeContent => "codeContent",
            _ => "content"
        };
    }
}