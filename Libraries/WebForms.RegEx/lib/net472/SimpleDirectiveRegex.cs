// Decompiled with JetBrains decompiler
// Type: System.Web.RegularExpressions.SimpleDirectiveRegex
// Assembly: System.Web.RegularExpressions, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
// MVID: 1458DB28-0627-44A7-91C1-6FF8AF665233
// Assembly location: C:\Windows\Microsoft.NET\Framework\v4.0.30319\System.Web.RegularExpressions.dll
// XML documentation location: C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.8\System.Web.RegularExpressions.xml

using System.Collections;
using System.Text.RegularExpressions;

namespace System.Web.RegularExpressions
{
  /// <summary>Provides a regular expression to parse an ASP.NET data directive.</summary>
  public class SimpleDirectiveRegex : Regex
  {
    /// <summary>Initializes a new instance of the <see cref="T:System.Web.RegularExpressions.SimpleDirectiveRegex" /> class.</summary>
    public SimpleDirectiveRegex()
    {
      this.pattern = "<%\\s*@(\\s*(?<attrname>\\w[\\w:]*(?=\\W))(\\s*(?<equal>=)\\s*\"(?<attrval>[^\"]*)\"|\\s*(?<equal>=)\\s*'(?<attrval>[^']*)'|\\s*(?<equal>=)\\s*(?<attrval>[^\\s\"'%>]*)|(?<equal>)(?<attrval>\\s*?)))*\\s*?%>";
      this.roptions = RegexOptions.Multiline | RegexOptions.Singleline;
      this.internalMatchTimeout = TimeSpan.FromTicks(-10000L);
      this.factory = (RegexRunnerFactory) new SimpleDirectiveRegexFactory14();
      this.capnames = new Hashtable();
      this.capnames.Add((object) "1", (object) 1);
      this.capnames.Add((object) "attrval", (object) 5);
      this.capnames.Add((object) "equal", (object) 4);
      this.capnames.Add((object) "attrname", (object) 3);
      this.capnames.Add((object) "2", (object) 2);
      this.capnames.Add((object) "0", (object) 0);
      this.capslist = new string[6];
      this.capslist[0] = "0";
      this.capslist[1] = "1";
      this.capslist[2] = "2";
      this.capslist[3] = "attrname";
      this.capslist[4] = "equal";
      this.capslist[5] = "attrval";
      this.capsize = 6;
      this.InitializeReferences();
    }

    /// <summary>Initializes a new instance of the <see cref="T:System.Web.RegularExpressions.SimpleDirectiveRegex" /> class with a specified time-out value.</summary>
    /// <param name="A_1">A time-out interval, or <see cref="F:System.Text.RegularExpressions.Regex.InfiniteMatchTimeout" /> if matching operations should not time out.</param>
    public SimpleDirectiveRegex(TimeSpan A_1)
      : this()
    {
      Regex.ValidateMatchTimeout(A_1);
      this.internalMatchTimeout = A_1;
    }
  }
}
