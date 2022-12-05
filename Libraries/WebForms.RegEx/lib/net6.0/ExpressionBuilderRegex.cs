// Decompiled with JetBrains decompiler
// Type: System.Web.RegularExpressions.ExpressionBuilderRegex
// Assembly: System.Web.RegularExpressions, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
// MVID: 1458DB28-0627-44A7-91C1-6FF8AF665233
// Assembly location: C:\Windows\Microsoft.NET\Framework\v4.0.30319\System.Web.RegularExpressions.dll
// XML documentation location: C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.8\System.Web.RegularExpressions.xml

using System.Collections;
using System.Text.RegularExpressions;
using WebForms.RegEx.Factory;

namespace WebForms.RegEx
{
  internal class ExpressionBuilderRegex : Regex
  {
    public ExpressionBuilderRegex()
    {
      this.pattern = "\\G\\s*<%\\s*\\$\\s*(?<code>.*)?%>\\s*\\z";
      this.roptions = RegexOptions.Multiline | RegexOptions.Singleline;
      this.internalMatchTimeout = TimeSpan.FromTicks(-10000L);
      this.factory = (RegexRunnerFactory) new ExpressionBuilderRegexFactory16();
      this.capnames = new Hashtable();
      this.capnames.Add((object) "0", (object) 0);
      this.capnames.Add((object) "code", (object) 1);
      this.capslist = new string[2];
      this.capslist[0] = "0";
      this.capslist[1] = "code";
      this.capsize = 2;
      this.InitializeReferences();
    }

    public ExpressionBuilderRegex(TimeSpan A_1)
      : this()
    {
      Regex.ValidateMatchTimeout(A_1);
      this.internalMatchTimeout = A_1;
    }
  }
}
