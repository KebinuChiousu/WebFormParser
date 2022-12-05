// Decompiled with JetBrains decompiler
// Type: System.Web.RegularExpressions.DataBindRegex
// Assembly: System.Web.RegularExpressions, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
// MVID: 1458DB28-0627-44A7-91C1-6FF8AF665233
// Assembly location: C:\Windows\Microsoft.NET\Framework\v4.0.30319\System.Web.RegularExpressions.dll
// XML documentation location: C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.8\System.Web.RegularExpressions.xml

using System.Collections;
using System.Text.RegularExpressions;
using WebForms.RegEx.Factory;

namespace WebForms.RegEx
{
  /// <summary>Provides a regular expression to parse an ASP.NET data binding expression.</summary>
  public class DataBindRegex : Regex
  {
    /// <summary>Initializes a new instance of the <see cref="T:WebForms.RegEx.DataBindRegex" /> class.</summary>
    public DataBindRegex()
    {
      this.pattern = "\\G\\s*<%\\s*?#(?<encode>:)?(?<code>.*?)?%>\\s*\\z";
      this.roptions = RegexOptions.Multiline | RegexOptions.Singleline;
      this.internalMatchTimeout = TimeSpan.FromTicks(-10000L);
      this.factory = (RegexRunnerFactory) new DataBindRegexFactory15();
      this.capnames = new Hashtable();
      this.capnames.Add((object) "0", (object) 0);
      this.capnames.Add((object) "code", (object) 2);
      this.capnames.Add((object) "encode", (object) 1);
      this.capslist = new string[3];
      this.capslist[0] = "0";
      this.capslist[1] = "encode";
      this.capslist[2] = "code";
      this.capsize = 3;
      this.InitializeReferences();
    }

    /// <summary>Initializes a new instance of the <see cref="T:WebForms.RegEx.DataBindRegex" /> class with a specified time-out value.</summary>
    /// <param name="A_1">A time-out interval, or <see cref="F:System.Text.RegularExpressions.Regex.InfiniteMatchTimeout" /> if matching operations should not time out.</param>
    public DataBindRegex(TimeSpan A_1)
      : this()
    {
      Regex.ValidateMatchTimeout(A_1);
      this.internalMatchTimeout = A_1;
    }
  }
}
