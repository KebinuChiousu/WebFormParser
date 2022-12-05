// Decompiled with JetBrains decompiler
// Type: System.Web.RegularExpressions.CommentRegex
// Assembly: System.Web.RegularExpressions, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
// MVID: 1458DB28-0627-44A7-91C1-6FF8AF665233
// Assembly location: C:\Windows\Microsoft.NET\Framework\v4.0.30319\System.Web.RegularExpressions.dll
// XML documentation location: C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.8\System.Web.RegularExpressions.xml

using System.Text.RegularExpressions;

namespace System.Web.RegularExpressions
{
  /// <summary>Provides a regular expression to parse an ASP.NET comment block.</summary>
  public class CommentRegex : Regex
  {
    /// <summary>Initializes a new instance of the <see cref="T:System.Web.RegularExpressions.CommentRegex" /> class.</summary>
    public CommentRegex()
    {
      this.pattern = "\\G<%--(([^-]*)-)*?-%>";
      this.roptions = RegexOptions.Multiline | RegexOptions.Singleline;
      this.internalMatchTimeout = TimeSpan.FromTicks(-10000L);
      this.factory = (RegexRunnerFactory) new CommentRegexFactory7();
      this.capsize = 3;
      this.InitializeReferences();
    }

    /// <summary>Initializes a new instance of the <see cref="T:System.Web.RegularExpressions.CommentRegex" /> class with the specified time-out value.</summary>
    /// <param name="A_1">A time-out interval, or <see cref="F:System.Text.RegularExpressions.Regex.InfiniteMatchTimeout" /> if matching operations should not time out.</param>
    public CommentRegex(TimeSpan A_1)
      : this()
    {
      Regex.ValidateMatchTimeout(A_1);
      this.internalMatchTimeout = A_1;
    }
  }
}
