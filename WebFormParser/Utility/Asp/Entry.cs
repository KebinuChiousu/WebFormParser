using System.Runtime.InteropServices;
using WebFormParser.Utility.Asp.Enum;

namespace WebFormParser.Utility.Asp;

public class Entry
{
    public string GroupName { get; set; }
    public string Value { get; set; }
    public AspFileEnum FileType { get; set; }
    public TagTypeEnum TagType { get; set; }
    public string? CodeFunction { get; set; }
    public bool IsOpen { get; set; }

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

    public static TagTypeEnum GetTagType(TagTypeEnum tagType, bool isCode)
    {
        return tagType switch
        {
            TagTypeEnum.Attr => isCode ? TagTypeEnum.CodeAttr : TagTypeEnum.Attr,
            TagTypeEnum.Value => isCode ? TagTypeEnum.CodeValue : TagTypeEnum.Value,
            TagTypeEnum.Close => isCode ? TagTypeEnum.CodeClose : TagTypeEnum.Close,
            TagTypeEnum.Open => isCode ? TagTypeEnum.CodeOpen : TagTypeEnum.Open,
            TagTypeEnum.Comment => isCode ? TagTypeEnum.CodeComment : TagTypeEnum.Comment,
            TagTypeEnum.Content => isCode ? TagTypeEnum.CodeContent : TagTypeEnum.Content,
            _ => tagType
        };
    }

    public static string GetGroupName(TagTypeEnum tagType)
    {
        return tagType switch
        {
            TagTypeEnum.Content => "content",
            TagTypeEnum.Comment => "comment",
            TagTypeEnum.Open => "tagOpen",
            TagTypeEnum.Close => "tagClose",
            TagTypeEnum.Attr => "tagAttr",
            TagTypeEnum.Value => "tagValue",
            TagTypeEnum.Page => "page",
            TagTypeEnum.CodeComment => "codeComment",
            TagTypeEnum.CodeOpen => "codeOpen",
            TagTypeEnum.CodeClose => "codeClose",
            TagTypeEnum.CodeAttr => "codeAttr",
            TagTypeEnum.CodeValue => "codeValue",
            TagTypeEnum.CodeContent => "codeContent",
            _ => "content"
        };
    }

    public override string ToString()
    {
        string fileType = GetFileType(this.FileType);

        return fileType + " - " + GetGroupName(this.TagType);
    }
}