using System;

namespace ASP
{
	/// <summary>
	/// Specifies the type of <see cref="Tag"/>.
	/// </summary>
	public enum TagType
	{
		/// <summary>
		/// A <see cref="Document"/> object that, as the root of the document tree,
		/// provides access to the entire Asp document. 
		/// </summary>
		Root,

		/// <summary>
		/// Open tag.
		/// <para>
		/// Example: &lt;asp:label id="Label1">
		/// </para>
		/// </summary>
		Open,

		/// <summary>
		/// Close tag.
		/// <para>
		/// Example: &lt;/asp:label>
		/// </para>
		/// </summary>
		Close,

		/// <summary>
		/// Text tag.
		/// <para>
		/// Example: &lt;title>TEXT&lt;/title> - the TEXT will occur as a Text tag
		/// </para>
		/// </summary>
		Text,

		/// <summary>
		/// Directive tag.
		/// <para>
		/// Example: &lt;%@ Page language= "c#" %>
		/// </para>
		/// </summary>
		Directive,

		/// <summary>
		/// Code tag.
		/// <para>
		/// Example: &lt;% this.DoSomething() %>
		/// </para>
		/// </summary>
		Code,

		/// <summary>
		/// Comment tag.
		/// <para>
		/// Example: &lt;!-- COMMENTED --> or &lt;%-- COMMENTED --%>
		/// </para>
		/// </summary>
		Comment
	}
}
