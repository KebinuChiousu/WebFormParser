using System;

namespace ASP
{
	/// <summary>
	/// Represents a single attribute of a <see cref="Tag"/>.
	/// </summary>
	public class Attribute
	{
		private Document _Root;
		/// <summary>
		/// Gets the <see cref="Document"/> to which this attribute belongs.
		/// </summary>
		/// <value>
		/// The <see cref="Document"/> to which this attribute belongs.
		/// </value>
		public Document Root
		{
			get { return this._Root; }
		}

		private Tag _Parent;
		/// <summary>
		/// Gets the <see cref="Tag"/> that the Atrribute belongs to.
		/// </summary>
		/// <value>
		/// The <see cref="Tag"/> that the Atrribute belongs to.
		/// </value>
		public virtual Tag Parent
		{
			get { return this._Parent; }
		}

		/// <summary>
		/// Gets the entire body of the <b>Attribute</b>.
		/// </summary>
		/// <value>
		/// The entire body of the <b>Attribute</b>.
		/// For example, Body is <c>id="Label1"</c> for the attribute <c>id="Label1"</c>.
		/// </value>
		public string Body
		{
			get { return this._BodyFragment.Text; }
		}

		private DocumentFragment _BodyFragment;
		/// <summary>
		/// Gets <see cref="DocumentFragment"/> that
		/// represents <see cref="Body"/> of this tag.
		/// </summary>
		/// <value>
		/// <see cref="DocumentFragment"/> representing entire body of this tag (<see cref="Body"/>).
		/// </value>
		public DocumentFragment BodyFragment
		{
			get { return this._BodyFragment; }
		}

		/// <summary>
		/// Gets the key of the attribute.
		/// </summary>
		/// <value>
		/// The key of the attribute.
		/// For example, Key is <c>id</c> for the attribute <c>id="Label1"</c>.
		/// </value>
		public string Key
		{
			get { return this._KeyFragment.Text; }
		}

		private DocumentFragment _KeyFragment;
		/// <summary>
		/// Gets <see cref="DocumentFragment"/> that
		/// represents <see cref="Key"/> of this tag.
		/// </summary>
		/// <value>
		/// <see cref="DocumentFragment"/> representing key of this tag (<see cref="Key"/>).
		/// </value>
		public DocumentFragment KeyFragment
		{
			get { return this._KeyFragment; }
		}

		/// <summary>
		/// Gets the value of the attribute.
		/// </summary>
		/// <value>
		/// The value of the attribute.
		/// For example, Value is <c>Label1</c> for the attribute <c>id="Label1"</c>.
		/// </value>
		public string Value
		{
			get { return this._ValueFragment.Text; }
		}

		private DocumentFragment _ValueFragment;
		/// <summary>
		/// Gets <see cref="DocumentFragment"/> that
		/// represents <see cref="Value"/> of this tag.
		/// </summary>
		/// <value>
		/// <see cref="DocumentFragment"/> representing value of this tag (<see cref="Value"/>).
		/// </value>
		public DocumentFragment ValueFragment
		{
			get { return this._ValueFragment; }
		}

		private bool _DataBound = false;
		/// <summary>
		/// Gets value indicating whether the attribute is databound.
		/// </summary>
		/// <value>
		/// <b>true</b> if the attribute is databound; otherwise, <b>false</b>.
		/// For example, DataBound is <c>true</c> for the attribute <c>Text='&lt;% this.ReturnText() %>'</c>.
		/// </value>
		public bool DataBound
		{
			get { return this._DataBound; }
		}

		/// <summary>
		/// Initializes a new instance of the <b>Attribute</b> class
		/// with the specified <see cref="Tag"/> that the attribute belongs to.
		/// </summary>
		/// <param name="parent"><see cref="Tag"/> to which the newly created Attribute belongs to.</param>
		internal Attribute(Tag parent)
		{
			parent.Attributes.Append(this);

			this._Root = parent.Root;
			this._Parent = parent;
			this._BodyFragment  = new DocumentFragment(this._Root);
			this._KeyFragment   = new DocumentFragment(this._Root);
			this._ValueFragment = new DocumentFragment(this._Root);
		}

		/// <summary>
		/// Sets the <see cref="DataBound"/> flag to true.
		/// </summary>
		internal void ItIsDataBound()
		{
			this._DataBound = true;
		}

		/// <summary>
		/// Gets string representation of the attribute.
		/// </summary>
		/// <returns>Entire attribute text (<see cref="Body"/>).</returns>
		public override string ToString()
		{
			return this.Body;
		}
	}
}
