// Decompiled with JetBrains decompiler
// Type: System.Web.RegularExpressions.AspCodeRegexFactory4
// Assembly: System.Web.RegularExpressions, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
// MVID: 1458DB28-0627-44A7-91C1-6FF8AF665233
// Assembly location: C:\Windows\Microsoft.NET\Framework\v4.0.30319\System.Web.RegularExpressions.dll
// XML documentation location: C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.8\System.Web.RegularExpressions.xml

using System.Text.RegularExpressions;
using WebForms.RegEx.Runner;

namespace WebForms.RegEx.Factory
{
  internal class AspCodeRegexFactory4 : RegexRunnerFactory
  {

      protected override RegexRunner CreateInstance() => (RegexRunner) new AspCodeRegexRunner4();
  }
}
