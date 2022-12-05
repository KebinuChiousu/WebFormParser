// Decompiled with JetBrains decompiler
// Type: System.Web.RegularExpressions.LTRegex
// Assembly: System.Web.RegularExpressions, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
// MVID: 1458DB28-0627-44A7-91C1-6FF8AF665233
// Assembly location: C:\Windows\Microsoft.NET\Framework\v4.0.30319\System.Web.RegularExpressions.dll
// XML documentation location: C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.8\System.Web.RegularExpressions.xml

using System.Text.RegularExpressions;

namespace WebForms.RegEx
{
  /// <summary>Provides a regular expression to look for a less than (&lt;) character in an ASP.NET web page.</summary>
  public class LTRegex : Regex
  {
    /// <summary>Initializes a new instance of the <see cref="T:WebForms.RegEx.LTRegex" /> class.</summary>
    public LTRegex()
    {
      this.pattern = "<[^%]";
      this.roptions = RegexOptions.Multiline | RegexOptions.Singleline;
      this.internalMatchTimeout = TimeSpan.FromTicks(-10000L);
      this.factory = (RegexRunnerFactory) new LTRegexFactory11();
      this.capsize = 1;
      this.InitializeReferences();
    }

    /// <summary>Initializes a new instance of the <see cref="T:WebForms.RegEx.LTRegex" /> class with a specified time-out value.</summary>
    /// <param name="A_1">A time-out interval, or <see cref="F:System.Text.RegularExpressions.Regex.InfiniteMatchTimeout" /> if matching operations should not time out.</param>
    public LTRegex(TimeSpan A_1)
      : this()
    {
      Regex.ValidateMatchTimeout(A_1);
      this.internalMatchTimeout = A_1;
    }
  }
}
