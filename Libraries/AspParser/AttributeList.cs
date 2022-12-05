using System;
using System.Collections;

namespace ASP
{
	/// <summary>
	/// Represents a list of attributes that can be accessed by key or index.
	/// </summary>
	public class AttributeList : IEnumerable
	{
		private ArrayList attributes;

		/// <summary>
		/// Gets the attribute with the specified key.
		/// </summary>
		/// <param name="attributeKey">The key of the attribute.</param>
		/// <value>
		/// The <see cref="Attribute"/> with the specified key.
		/// </value>
		public Attribute this[string attributeKey]
		{
			get
			{
				foreach( Attribute attribute in this.attributes )
				{
					if( CaseInsensitiveComparer.DefaultInvariant.Compare(attribute.Key, attributeKey) == 0 )
						return attribute;
				}

				return null;
			}
		}

		/// <summary>
		/// Gets the attribute with the specified index.
		/// </summary>
		/// <param name="index">The index of the attribute.</param>
		/// <value>
		/// The <see cref="Attribute"/> with the specified index.
		/// </value>
		public Attribute this[int index]
		{
			get { return (Attribute) this.attributes[index]; }
		}

		/// <summary>
		/// Gets the number of attributes in the list.
		/// </summary>
		/// <value>
		/// The number of attributes.
		/// </value>
		public int Count
		{
			get { return this.attributes.Count; }
		}

		/// <summary>
		/// Initializes a new instance of the <b>AttributeList</b> class.
		/// The list is initialy empty.
		/// </summary>
		internal AttributeList()
		{
			this.attributes = new ArrayList();
		}

		/// <summary>
		/// Appends specified <see cref="Attribute"/> at the end of the list.
		/// </summary>
		/// <param name="attribute">The <see cref="Attribute"/> to append.</param>
		internal void Append(Attribute attribute)
		{
			this.attributes.Add(attribute);
		}

		/// <summary>
		/// Provides support for the "foreach" style iteration over the lis of attributes in the <b>AttributeList</b>.
		/// </summary>
		/// <returns>An <see cref="IEnumerator"/>.</returns>
		public IEnumerator GetEnumerator()
		{
			return this.attributes.GetEnumerator();
		}
	}
}
