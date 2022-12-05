// Decompiled with JetBrains decompiler
// Type: System.Web.RegularExpressions.RunatServerRegex
// Assembly: System.Web.RegularExpressions, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
// MVID: 1458DB28-0627-44A7-91C1-6FF8AF665233
// Assembly location: C:\Windows\Microsoft.NET\Framework\v4.0.30319\System.Web.RegularExpressions.dll
// XML documentation location: C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.8\System.Web.RegularExpressions.xml

using System.Text.RegularExpressions;
using WebForms.RegEx.Factory;

namespace WebForms.RegEx
{
  /// <summary>Provides a regular expression to parse an ASP.NET <see langword="runat" /> attribute.</summary>
  public class RunatServerRegex : Regex
  {
    /// <summary>Initializes a new instance of the <see cref="T:WebForms.RegEx.RunatServerRegex" /> class.</summary>
    public RunatServerRegex()
    {
      this.pattern = "runat\\W*server";
      this.roptions = RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Singleline | RegexOptions.CultureInvariant;
      this.internalMatchTimeout = TimeSpan.FromTicks(-10000L);
      this.factory = (RegexRunnerFactory) new RunatServerRegexFactory13();
      this.capsize = 1;
      this.InitializeReferences();
    }

    /// <summary>Initializes a new instance of the <see cref="T:WebForms.RegEx.RunatServerRegex" /> class with a specified time-out value.</summary>
    /// <param name="A_1">A time-out interval, or <see cref="F:System.Text.RegularExpressions.Regex.InfiniteMatchTimeout" /> if matching operations should not time out.</param>
    public RunatServerRegex(TimeSpan A_1)
      : this()
    {
      Regex.ValidateMatchTimeout(A_1);
      this.internalMatchTimeout = A_1;
    }
  }
}
