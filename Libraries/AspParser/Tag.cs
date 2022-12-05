using System;
using System.Collections;
using System.Text.RegularExpressions;

namespace ASP
{
	/// <summary>
	/// Represents a single tag in the ASPX document.
	/// </summary>
	public class Tag
	{
		private Document _Root;
		/// <summary>
		/// Gets the <see cref="Document"/> to which this tag belongs.
		/// </summary>
		/// <value>
		/// The <see cref="Document"/> to which this tag belongs.
		/// </value>
		public virtual Document Root
		{
			get { return this._Root; }
		}

		private Tag _Parent;
		/// <summary>
		/// Gets the parent of this tag.
		/// </summary>
		/// <value>
		/// The <b>Tag</b> that is the parent of the current tag.
		/// </value>
		public virtual Tag Parent
		{
			get { return this._Parent; }
		}

		/// <summary>
		/// Gets the namespace prefix of this tag.
		/// </summary>
		/// <value>
		/// The namespace prefix of this tag.
		/// For example, Prefix is <c>asp</c> for the tag <c>&lt;asp:label></c>.
		/// If there is no prefix, this property returns String.Empty.
		/// </value>
		public virtual string Prefix
		{
			get
			{
				if( this.TagType == TagType.Open || this.TagType == TagType.Close )
				{
					int index = this.Name.IndexOf(":");
					if( index > 0 )
						return this.Name.Substring(0, index);
				}

				return "";
			}
		}

		/// <summary>
		/// Gets local name of the tag.
		/// </summary>
		/// <value>
		/// The name of the tag with the prefix removed.
		/// For example, LocalName is <c>label</c> for the tag <c>&lt;asp:label></c>.
		/// </value>
		public virtual string LocalName
		{
			get
			{
				if( this.TagType == TagType.Open || this.TagType == TagType.Close )
				{
					int index = this.Name.IndexOf(":");
					if( index > 0 )
						return this.Name.Substring(index + 1);
				}

				return "";
			}
		}

		/// <summary>
		/// Gets the qualified name of the tag.
		/// </summary>
		/// <value>
		/// The qualified name of the tag.
		/// For example, Name is <c>asp:label</c> for the tag <c>&lt;asp:label></c>.
		/// </value>
		public virtual string Name
		{
			get { return this._NameFragment.Text; }
		}

		private DocumentFragment _NameFragment;
		/// <summary>
		/// Gets <see cref="DocumentFragment"/> that
		/// represents <see cref="Name"/> of this tag.
		/// </summary>
		/// <value>
		/// <see cref="DocumentFragment"/> representing name of this tag (<see cref="Name"/>).
		/// </value>
		public virtual DocumentFragment NameFragment
		{
			get { return this._NameFragment; }
		}

		/// <summary>
		/// Gets the entire text of this tag.
		/// </summary>
		/// <value>
		/// Entire text of this tag.
		/// For example, Value is <c>&lt;asp:label id="Label1"></c> for the tag <c>&lt;asp:label id="Label1"></c>.
		/// </value>
		public virtual string Value
		{
			get { return this._ValueFragment.Text; }
		}

		private DocumentFragment _ValueFragment;
		/// <summary>
		/// Gets <see cref="DocumentFragment"/> that
		/// represents <see cref="Value"/> of this tag.
		/// </summary>
		/// <value>
		/// <see cref="DocumentFragment"/> representing entire text of this tag (<see cref="Value"/>).
		/// </value>
		public virtual DocumentFragment ValueFragment
		{
			get { return this._ValueFragment; }
		}

		private AttributeList _Attributes;
		/// <summary>
		/// Gets an <see cref="AttributeList"/> containing the attributes of this tag.
		/// </summary>
		/// <value>
		/// An <see cref="AttributeList"/> containing the attributes of the tag.
		/// </value>
		public AttributeList Attributes
		{
			get { return this._Attributes; }
		}

		private TagList _ChildTags;
		/// <summary>
		/// Gets all the child tags of the tag.
		/// </summary>
		/// <value>
		/// An <see cref="TagList"/> that contains all the child tags of the tag.
		/// </value>
		public TagList ChildTags
		{
			get { return this._ChildTags; }
		}

		/// <summary>
		/// Gets a value indicating whether this tag has any child tags.
		/// </summary>
		/// <value>
		/// <b>true</b> if the tag has child tags; otherwise, <b>false</b>.
		/// </value>
		public bool HasChildTags
		{
			get { return (this._ChildTags.Count != 0); }
		}

		/// <summary>
		/// Gets the first child of the tag.
		/// </summary>
		/// <value>
		/// The first child of the tag.
		/// If there is no such tag, a null reference is returned.
		/// </value>
		public Tag FirstChild
		{
			get { return this._ChildTags.FirstTag; }
		}

		/// <summary>
		/// Gets the last child of the tag.
		/// </summary>
		/// <value>
		/// The last child of the tag.
		/// If there is no such tag, a null reference is returned.
		/// </value>
		public Tag LastChild
		{
			get { return this._ChildTags.LastTag; }
		}

		private TagType _TagType;
		/// <summary>
		/// Gets the type of the current tag.
		/// </summary>
		/// <value>
		/// One of the <see cref="TagType"/> values.
		/// </value>
		public TagType TagType
		{
			get { return this._TagType; }
		}

		private TagError _Error;
		/// <summary>
		/// Gets the error of the current tag.
		/// </summary>
		/// <value>
		/// One of the <see cref="TagError"/> values.
		/// </value>
		public TagError Error
		{
			get { return this._Error; }
		}

		#region Empty Tags definitions

		private static Hashtable emptyTags;
		static Tag()
		{
			emptyTags = new Hashtable(CaseInsensitiveHashCodeProvider.DefaultInvariant, CaseInsensitiveComparer.DefaultInvariant);
			emptyTags.Add("!DOCTYPE", null);
			emptyTags.Add("WBR", null);
			emptyTags.Add("RT", null);
			emptyTags.Add("PLAINTEXT", null);
			emptyTags.Add("PARAM", null);
			// DO REQUIRE CLOSING </P> TAG THOUGH MSDN STATES THAT <P> DOES NOT REQUIRE </P>
			//emptyTags.Add("P", null);
			emptyTags.Add("OPTION", null);
			emptyTags.Add("META", null);
			emptyTags.Add("LINK", null);
			emptyTags.Add("LI", null);
			emptyTags.Add("INPUT", null);
			emptyTags.Add("IMG", null);
			emptyTags.Add("HR", null);
			emptyTags.Add("FRAME", null);
			emptyTags.Add("EMBED", null);
			emptyTags.Add("COL", null);
			emptyTags.Add("BR", null);
			emptyTags.Add("BGSOUND", null);
			emptyTags.Add("BASEFONT", null);
			emptyTags.Add("BASE", null);
			emptyTags.Add("AREA", null);
		}

		#endregion

		/// <summary>
		/// Gets value indicating whether the tag requires to be closed with a close tag.
		/// </summary>
		/// <value>
		/// <b>true</b> if the tag requires to be closed; otherwise, <b>false</b>.
		/// </value>
		public virtual bool RequiresClosingTag
		{
			get
			{
				if( this.TagType == TagType.Open )
				{
					return (emptyTags.ContainsKey(this.Name) == false);
				}

				return false;
			}
		}

		/// <summary>
		/// Initializes a new instance of the <b>Tag</b> class
		/// with the specified parent and type.
		/// </summary>
		/// <param name="parent">Parent of the created tag.</param>
		/// <param name="type">Type of the created tag.</param>
		internal Tag(Tag parent, TagType type)
		{
			this._Parent = parent;
			if( parent != null )
			{
				this._Root = parent.Root;
				this._Parent.ChildTags.Append(this);
			}

			this._TagType = type;

			this._ChildTags  = new TagList();
			this._Attributes = new AttributeList();

			this._NameFragment  = new DocumentFragment(this.Root);
			this._ValueFragment = new DocumentFragment(this.Root);

			this._Error = TagError.None;
		}

		/// <summary>
		/// Sets error to the current tag.
		/// </summary>
		/// <param name="error">Error to set.</param>
		internal void SetError(TagError error)
		{
			this._Error = error;
		}

		/// <summary>
		/// Gets string representation of the tag.
		/// </summary>
		/// <returns>Entire tag text (<see cref="Value"/>).</returns>
		public override string ToString()
		{
			return this.Value;
		}
	}
}
