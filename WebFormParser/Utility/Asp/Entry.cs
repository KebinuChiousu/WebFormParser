using System.Diagnostics;
using System.Net.Http.Headers;
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

    public string Name 
    {
        get
        {
            if (this.FileType == AspFileEnum.CodeBehind)
                return string.Empty;
            return this.TagType == TagTypeEnum.Open ? GetEntryName(this.Value) : string.Empty;
        }
    }

    private static string GetEntryName(string value)
    {
        var source = value;
        // strip <
        source= source.Replace("<", "");
        // strip >
        source= source.Replace(">", "");
        // Remove whitespace from front
        source = source.TrimStart();
        // Split on whitespace
        var tag = source.Split(" ").ToList();
        // Get Tag name (text after <)
        var name = tag[0].ToLower();
        return name;
    }

    public bool NeedsChildren
    {
        get { return RequiresChildren();  }
    }

    public string InnerText { get; set; }

    public List<Entry> Children { get; }

    public bool HasAttributes
    {
        get { return Attributes.Count > 0; }
    }

    public bool HasChildren
    {
        get { return Children.Count > 0; }
    }

    public bool SelfClosing
    {
        get { return IsSelfClosing(); }
    }

    public Dictionary<string, string> Attributes { get; set; }

    [DebuggerStepThrough]
    public Entry()
    {
        GroupName = "";
        InnerText = "";
        Value = "";
        FileType = AspFileEnum.Html;
        TagType = TagTypeEnum.Content;
        Attributes = new Dictionary<string, string>();
        Children = new List<Entry>();
    }

    [DebuggerStepThrough]
    private bool IsSelfClosing()
    {
        var tags = new List<string>
        {
            "area", "base", "br", "col", "embed", "hr", "img", "input",
            "link", "meta", "param", "source", "track", "wbr"
        };

        return tags.Contains(Value.ToLower());
    }

    private bool RequiresChildren()
    {
        var tags = new List<string>
        {
            "form", "select", "table", "tr", "td", "div"
        };

        return tags.Contains(Value.ToLower());
    }

    [DebuggerStepThrough]
    public string GetFileType(AspFileEnum fileType)
    {
        return fileType == AspFileEnum.CodeBehind ? "Code" : "Html";
    }

    [DebuggerStepThrough]
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

    [DebuggerStepThrough]
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
            TagTypeEnum.Script => "script",
            TagTypeEnum.DocType => "docType",
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

    [DebuggerStepThrough]
    public override string ToString()
    {
        var fileType = GetFileType(this.FileType);
        var output = fileType + " | " + GetGroupName(this.TagType) + " | ";
        if (!string.IsNullOrEmpty(this.Name))
            output += this.Name + " | ";
        if (!string.IsNullOrEmpty(this.CodeFunction))
            output += this.CodeFunction + " | ";
        output += this.Value + " |";
        return output;
    }
}