using System;
using System.Collections;

using WebForms.RegEx;
using System.Text.RegularExpressions;

namespace ASP
{
	/// <summary>
	/// Represents an ASPX document.
	/// </summary>
	public class Document : Tag
	{
		/// <summary>
		/// Gets <see cref="Document"/> to which this tag belongs.
		/// </summary>
		/// <value>
		/// This property always returns this reference
		/// as the <b>Document</b> is always a root.
		/// </value>
		public override Document Root
		{
			get { return this; }
		}

		/// <summary>
		/// Gets the parent of this tag.
		/// </summary>
		/// <value>
		/// This property always returns a null reference
		/// as the <b>Document</b> is always a root.
		/// </value>
		public override Tag? Parent
		{
			get { return null; }
		}

		/// <summary>
		/// Gets the namespace prefix of this tag.
		/// </summary>
		/// <value>
		/// This property always returns an empty string (String.Empty)
		/// as the <b>Document</b> does not have a Prefix.
		/// </value>
		public override string Prefix
		{
			get { return ""; }
		}

		/// <summary>
		/// Gets the entire text of the ASP document.
		/// </summary>
		/// <value>
		/// The entire text of the ASP document.
		/// </value>
		public override string LocalName
		{
			get { return this._Value; }
		}

		/// <summary>
		/// Gets the entire text of the ASP document.
		/// </summary>
		/// <value>
		/// The entire text of the ASP document.
		/// </value>
		public override string Name
		{
			get { return this._Value; }
		}

		/// <summary>
		/// Gets <see cref="DocumentFragment"/> that
		/// represents <see cref="Name"/> of this tag.
		/// </summary>
		/// <value>
		/// <see cref="DocumentFragment"/> representing name of this tag (<see cref="Name"/>).
		/// </value>
		public override DocumentFragment NameFragment
		{
			get { return this._ValueFragment; }
		}

		private string _Value;
		/// <summary>
		/// Gets entire text of the ASP document.
		/// </summary>
		/// <value>
		/// Entire text of this document.
		/// </value>
		public override string Value
		{
			get { return this._Value; }
		}

		private DocumentFragment _ValueFragment;
		/// <summary>
		/// Gets <see cref="DocumentFragment"/> that
		/// represents <see cref="Value"/> of this tag.
		/// </summary>
		/// <value>
		/// <see cref="DocumentFragment"/> representing value of this tag (<see cref="Value"/>).
		/// </value>
		public override DocumentFragment ValueFragment
		{
			get { return this._ValueFragment; }
		}

		/// <summary>
		/// Initializes a new instance of the <b>Document</b> class
		/// with the Aspx document content.
		/// </summary>
		/// <param name="aspx">The Aspx document content that will be represented by the created <b>Document</b> object.</param>
		public Document(string aspx) : base(null, TagType.Root)
		{
			this._Value = aspx;
			this._ValueFragment = new DocumentFragment(this);
			this._ValueFragment.Set(0, aspx.Length);
			this.ParseStringInternal( this );
		}

		#region ASP Parsing

		//static Regex openTagRegex			= new Regex(@"\G<(?<tagname>[\w:\.]+)(\s+(?<attrname>[-\w]+)(\s*=\s*""(?<attrval>[^""]*)""|\s*=\s*'(?<attrval>[^']*)'|\s*=\s*(?<attrval><%#.*?%>)|\s*=\s*(?<attrval>[^\s=/>]*)|(?<attrval>\s*?)))*\s*(?<empty>/)?>", RegexOptions.Multiline | RegexOptions.Compiled);
		//static Regex closeTagRegex			= new Regex(@"\G</(?<tagname>[\w:\.]+)\s*>", RegexOptions.Multiline | RegexOptions.Compiled);
		//static Regex textRegex				= new Regex(@"\G[^<]+", RegexOptions.Multiline | RegexOptions.Compiled);
		//static Regex directiveRegex			= new Regex(@"\G<%\s*@(\s*(?<attrname>\w+(?=\W))(\s*(?<equal>=)\s*""(?<attrval>[^""]*)""|\s*(?<equal>=)\s*'(?<attrval>[^']*)'|\s*(?<equal>=)\s*(?<attrval>[^\s%>]*)|(?<equal>)(?<attrval>\s*?)))*\s*?%>", RegexOptions.Multiline | RegexOptions.Compiled);
		//static Regex simpleDirectiveRegex	= new Regex(@"<%\s*@(\s*(?<attrname>\w+(?=\W))(\s*(?<equal>=)\s*""(?<attrval>[^""]*)""|\s*(?<equal>=)\s*'(?<attrval>[^']*)'|\s*(?<equal>=)\s*(?<attrval>[^\s%>]*)|(?<equal>)(?<attrval>\s*?)))*\s*?%>", RegexOptions.Multiline | RegexOptions.Compiled);
		//static Regex aspxCodeRegex			= new Regex(@"\G<%(?!@)(?<code>.*?)%>", RegexOptions.Multiline | RegexOptions.Compiled);
		//static Regex aspxExprRegex			= new Regex(@"\G<%\s*?=(?<code>.*?)?%>", RegexOptions.Multiline | RegexOptions.Compiled);
		//static Regex databindRegex			= new Regex(@"\G<%#(?<code>.*?)?%>", RegexOptions.Multiline | RegexOptions.Compiled);
		//static Regex databind1Regex			= new Regex(@"\G\s*<%\s*?#(?<code>.*?)?%>\s*\z", RegexOptions.Multiline | RegexOptions.Compiled);
		//static Regex commentRegex			= new Regex(@"\G<%--(([^-]*)-)*?-%>", RegexOptions.Multiline | RegexOptions.Compiled);
		//static Regex includeRegex			= new Regex(@"\G<!--\s*#(?i:include)\s*(?<pathtype>[\w]+)\s*=\s*[""']?(?<filename>[^\""']*?)[""']?\s*-->", RegexOptions.Multiline | RegexOptions.Compiled);
		//static Regex GTRegex				= new Regex(@"[^%]>", RegexOptions.Multiline | RegexOptions.Compiled);
		//static Regex LTRegex				= new Regex(@"<[^%]", RegexOptions.Multiline | RegexOptions.Compiled);
		//static Regex serverTagsRegex		= new Regex(@"<%(?!#)(([^%]*)%)*?>", RegexOptions.Multiline | RegexOptions.Compiled);
		//static Regex runatServerRegex		= new Regex(@"runat\W*server", RegexOptions.Multiline | RegexOptions.Compiled);

		private static Regex tagRegex;
		private static Regex endtagRegex;
		private static Regex textRegex;
		private static Regex directiveRegex;
		private static Regex aspCodeRegex;
		private static Regex aspExprRegex;
		private static Regex databindExprRegex;
		private static Regex attributeDatabindExprRegex;
		private static Regex serverCommentRegex;
		private static Regex xmlCommentRegex = new Regex(@"\G<!--(([^-]*)-)*?->", RegexOptions.Multiline | RegexOptions.Compiled);
		private static Regex includeRegex;
		private static Regex gtRegex;
		private static Regex ltRegex;
		private static Regex serverTagsRegex;
		private static Regex runatServerRegex;

		static Document()
		{
			tagRegex = new TagRegex();
			directiveRegex = new DirectiveRegex();
			endtagRegex = new EndTagRegex();
			aspCodeRegex = new AspCodeRegex();
			aspExprRegex = new AspExprRegex();
			databindExprRegex = new DatabindExprRegex();
			attributeDatabindExprRegex = new DataBindRegex();
			serverCommentRegex = new CommentRegex();
			includeRegex = new IncludeRegex();
			textRegex = new TextRegex();
			gtRegex = new GTRegex();
			ltRegex = new LTRegex();
			serverTagsRegex = new ServerTagsRegex();
			runatServerRegex = new RunatServerRegex();
		}

		bool inScriptTag = false;
		bool closeTagDoesNotCloseScript;

		private void ParseStringInternal(Tag root)
		{
			string text = this._Value;
			int offset = 0;
			Match match;
			Tag current = root;
 
			do
			{
				match = textRegex.Match(text, offset);
				if( match.Success )
				{
					this.ProcessTextTag(current, match);
					offset = (match.Index + match.Length);
				}
				if( offset == text.Length )
				{
					break;
				}

				this.closeTagDoesNotCloseScript = false;
				if( ! this.inScriptTag )
				{
					match = directiveRegex.Match(text, offset);
					if( match.Success )
					{
						this.ProcessDirective(current, match);
						goto MOVE_OFFSET;
					}
				}

//				match = includeRegex.Match(text, offset);
//				if( match.Success )
//				{
//					//this.ProcessServerInclude(match);
//				}
//				else
				{
					match = serverCommentRegex.Match(text, offset);
					if( match.Success )
					{
						this.ProcessComment(current, match);
						goto MOVE_OFFSET;
					}

					match = xmlCommentRegex.Match(text, offset);
					if( match.Success )
					{
						this.ProcessComment(current, match);
						goto MOVE_OFFSET;
					}

					if( this.inScriptTag == false )
					{
						match = aspExprRegex.Match(text, offset);
						if( match.Success )
						{
							this.ProcessCode(current, match);
							goto MOVE_OFFSET;
						}

						match = databindExprRegex.Match(text, offset);
						if( match.Success )
						{
							this.ProcessCode(current, match);
							goto MOVE_OFFSET;
						}

						match = aspCodeRegex.Match(text, offset);
						if( match.Success )
						{
							this.ProcessCode(current, match);
							goto MOVE_OFFSET;
						}

						match = tagRegex.Match(text, offset);
						if( match.Success )
						{
							current = this.ProcessOpenTag(current, match);
							goto MOVE_OFFSET;
						}
					}

					match = endtagRegex.Match(text, offset);
					if( match.Success )
					{
						current = this.ProcessCloseTag(current, match);
					}
				}
 
			MOVE_OFFSET:
				if( (match == null) || (!match.Success || this.closeTagDoesNotCloseScript) )
				{	
					this.ProcessTextTag(current, offset, 1);
					offset = (offset + 1);
				}
				else
				{
					offset = (match.Index + match.Length);
				}
			}
			while (offset != text.Length);
		}

		private Tag ProcessOpenTag(Tag current, Match openTagMatch)
		{
			Tag openTag = new Tag( current, TagType.Open );
			openTag.ValueFragment.Set(openTagMatch.Index, openTagMatch.Length);
			Group tagName = openTagMatch.Groups["tagname"];
			openTag.NameFragment.Set(tagName.Index, tagName.Length);
			this.ProcessAttributes(openTag, openTagMatch);

			// Do not change current if openTag is empty (e.g.: <EMPTY_SAMPLE/>)
			Group emptyTag = openTagMatch.Groups["empty"];
			if( emptyTag.Success )
			{
				return current;
			}

			// Do not change current if the openTag does not require closing tag (e.g.: <BR>)
			if( openTag.RequiresClosingTag == false )
			{
				return current;
			}

			// Set error on the open tag.
			//  The error is cleaned when the open tag is closed with a close tag
			openTag.SetError(TagError.UnclosedOpenTag);

			// Change this.inScriptTag to true if the tag is <script>
			if( this.IsScriptTagName(openTag.Name) )
			{
				this.inScriptTag = true;
			}

			return openTag;
		}

		private bool IsScriptTagName(string name)
		{
			return (CaseInsensitiveComparer.DefaultInvariant.Compare("SCRIPT", name) == 0);
		}

		private Tag ProcessCloseTag(Tag? current, Match closeTagMatch)
        {
            if (current == null)
                return new Tag(null, TagType.Comment);
            
            Group tagName = closeTagMatch.Groups["tagname"];

			// Change inScriptTag flag to false if the tag is </script>
			if( this.IsScriptTagName(tagName.Value) )
			{
				this.inScriptTag = false;
			}
			else if( this.inScriptTag )
			{
				// Append close tag only if not inScriptTag or we are in the script and we encountered </script> tag
				this.closeTagDoesNotCloseScript = true;
				return current;
			}

			// Find open tag for the closing tag.
			Tag? openTag = null;
			for(openTag = current; openTag != null; openTag = openTag.Parent)
			{
				// Ignore all tags except Open tags
				if( openTag.TagType != TagType.Open )
					continue;

				// Check if the closeTag closes the one of its parents
				if( CaseInsensitiveComparer.DefaultInvariant.Compare(tagName.Value, openTag.Name) == 0 )
				{
					break;
				}
			}

			// If the Close tag matched with an Open one,
			//  then parent of the Open tag becomes current tag
			if( openTag != null )
			{
				// Clean the error on the open tag as it is closed
				openTag.SetError(TagError.None);
				current = openTag.Parent;
			}

			Tag closeTag = new Tag( current, TagType.Close );
			closeTag.ValueFragment.Set(closeTagMatch.Index, closeTagMatch.Length);
			closeTag.NameFragment.Set(tagName.Index, tagName.Length);
			closeTag.SetError((openTag == null) ? TagError.UnopenedCloseTag : TagError.None);

            return current ?? new Tag(null, TagType.Comment);
        }

		private void ProcessTextTag(Tag current, Match textMatch)
		{
			this.ProcessTextTag(current, textMatch.Index, textMatch.Length);
		}

		private void ProcessTextTag(Tag current, int index, int length)
		{
			// Merge text tags if they occur close to each other
			Tag previous = current.ChildTags.LastTag;
			if( previous != null && previous.TagType == TagType.Text )
			{
				previous.ValueFragment.Set(previous.ValueFragment.Index, index + length - previous.ValueFragment.Index);
				return;
			}

			Tag textTag = new Tag( current, TagType.Text );
			textTag.ValueFragment.Set(index, length);
			textTag.NameFragment.Set(index, length);
		}

		private void ProcessDirective(Tag current, Match directiveMatch)
		{
			Tag directiveTag = new Tag( current, TagType.Directive );
			directiveTag.ValueFragment.Set(directiveMatch.Index, directiveMatch.Length);
			directiveTag.NameFragment.Set(directiveMatch.Index, directiveMatch.Length);
			this.ProcessAttributes(directiveTag, directiveMatch);
		}

		private void ProcessCode(Tag current, Match aspxExpressionMatch)
		{
			Tag expressionTag = new Tag( current, TagType.Code );
			expressionTag.ValueFragment.Set(aspxExpressionMatch.Index, aspxExpressionMatch.Length);
			expressionTag.NameFragment.Set(aspxExpressionMatch.Index, aspxExpressionMatch.Length);
		}

		private void ProcessComment(Tag current, Match commentMatch)
		{
			Tag commentTag = new Tag( current, TagType.Comment );
			commentTag.ValueFragment.Set(commentMatch.Index, commentMatch.Length);
			commentTag.NameFragment.Set(commentMatch.Index, commentMatch.Length);
		}

		private void ProcessAttributes(Tag tag, Match match)
		{
			CaptureCollection names = match.Groups["attrname"].Captures;
			CaptureCollection values = match.Groups["attrval"].Captures;
			for(int i = 0; i < names.Count; i ++)
			{
				Capture name = names[i];
				Capture value = values[i];
				Attribute attribute = new Attribute(tag);
				attribute.BodyFragment.Set(name.Index, value.Index + value.Length - name.Index);
				char nextChar = this._Value[attribute.BodyFragment.Index + attribute.BodyFragment.Length];
				if( nextChar == '\'' || nextChar == '"' )
				{
					attribute.BodyFragment.Set(attribute.BodyFragment.Index, attribute.BodyFragment.Length + 1);
				}

				attribute.KeyFragment.Set(name.Index, name.Length);
				attribute.ValueFragment.Set(value.Index, value.Length);
				if( attributeDatabindExprRegex.Match(value.Value).Success )
					attribute.ItIsDataBound();
			}
		}

		#endregion
	}
}
