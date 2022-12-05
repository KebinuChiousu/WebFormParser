using System;
using System.Collections;

namespace ASP
{
	/// <summary>
	/// Represents a list of tags that can be accessed by name or index.
	/// </summary>
	public class TagList : IEnumerable
	{
		private ArrayList tags;

		/// <summary>
		/// Gets the first tag with the specified qualified <see cref="Tag.Name"/>.
		/// </summary>
		/// <param name="name">The qualified name of the tag to retrieve.</param>
		/// <value>The first <see cref="Tag"/> that matches the specified name.</value>
		public Tag this[string name]
		{
			get
			{
				foreach(Tag tag in this.tags)
				{
					if( CaseInsensitiveComparer.DefaultInvariant.Compare(tag.Name, name) == 0 )
						return tag;
				}

				return null;
			}
		}

		/// <summary>
		/// Gets the tag with the specified index.
		/// </summary>
		/// <param name="index">The index of the tag.</param>
		/// <value>
		/// The <see cref="Tag"/> with the specified index.
		/// </value>
		public Tag this[int index]
		{
			get { return (Tag) this.tags[index]; }
		}

		/// <summary>
		/// Gets the first <see cref="Tag"/> in the list.
		/// </summary>
		/// <value>
		/// The first <see cref="Tag"/> in the list.
		/// </value>
		public Tag FirstTag
		{
			get { return (Tag)((tags.Count > 0) ? tags[0] : null); }
		}

		/// <summary>
		/// Gets the last <see cref="Tag"/> in the list.
		/// </summary>
		/// <value>
		/// The last <see cref="Tag"/> in the list.
		/// </value>
		public Tag LastTag
		{
			get { return (Tag)((tags.Count > 0) ? tags[tags.Count - 1] : null); }
		}

		/// <summary>
		/// Gets the number of tags in the list.
		/// </summary>
		/// <value>
		/// The number of tags.
		/// </value>
		public int Count
		{
			get { return this.tags.Count; }
		}

		/// <summary>
		/// Initializes a new instance of the <b>TagList</b> class.
		/// The list is initialy empty.
		/// </summary>
		internal TagList()
		{
			this.tags = new ArrayList();
		}

		/// <summary>
		/// Appends specified <see cref="Tag"/> at the end of the list.
		/// </summary>
		/// <param name="tag">The <see cref="Tag"/> to append.</param>
		internal void Append(Tag tag)
		{
			this.tags.Add(tag);
		}

		/// <summary>
		/// Provides support for the "foreach" style iteration over the list of tags in the <b>TagList</b>.
		/// </summary>
		/// <returns>An <see cref="IEnumerator"/>.</returns>
		public IEnumerator GetEnumerator()
		{
			return this.tags.GetEnumerator();
		}
	}
}
