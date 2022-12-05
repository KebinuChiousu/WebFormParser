using System;

namespace ASP
{
	/// <summary>
	/// Specifies error of <see cref="Tag"/>.
	/// </summary>
	public enum TagError
	{
		/// <summary>
		/// No error.
		/// </summary>
		None,

		/// <summary>
		/// The tag is not closed with a closing tag.
		/// </summary>
		UnclosedOpenTag,

		/// <summary>
		/// Close tag occurs without open tag.
		/// </summary>
		UnopenedCloseTag
	}
}
