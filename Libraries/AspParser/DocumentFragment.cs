using System;

namespace ASP
{
	/// <summary>
	/// Represents fragment of <see cref="Document"/> defined by index and length
	/// in the parent document.
	/// </summary>
	public class DocumentFragment
	{
		private Document? _Root;
		/// <summary>
		/// Gets the <see cref="Document"/> to which this fragment belongs.
		/// </summary>
		/// <value>
		/// The <see cref="Document"/> to which this fragment belongs.
		/// </value>
		public Document? Root
		{
			get { return this._Root; }
		}

		private int _Index;
		/// <summary>
		/// Gets index of the fragment in <see cref="Root"/> document.
		/// </summary>
		/// <value>
		/// Index of the fragment in <see cref="Root"/> document.
		/// </value>
		public int Index
		{
			get { return this._Index; }
		}

		private int _Length;
		/// <summary>
		/// Gets length of the fragment in <see cref="Root"/> document.
		/// </summary>
		/// <value>
		/// Length of the fragment in <see cref="Root"/> document.
		/// </value>
		public int Length
		{
			get { return this._Length; }
		}

		private int _LineNo;
		/// <summary>
		/// Gets the line number of the fragment in <see cref="Root"/> document.
		/// </summary>
		/// <value>
		/// The line number of the fragment in <see cref="Root"/> document.
		/// </value>
		public int LineNo
		{
			get { return this._LineNo; }
		}

		private int _ColumnNo;
		/// <summary>
		/// Gets the column number of the fragment in <see cref="Root"/> document.
		/// </summary>
		/// <value>
		/// The line column of the fragment in <see cref="Root"/> document.
		/// </value>
		public int ColumnNo
		{
			get { return this._ColumnNo; }
		}

		/// <summary>
		/// Defines index and length of the fragment in the <see cref="Root"/> document.
		/// </summary>
		/// <param name="index">Index at which the fragment occurs in <see cref="Root"/> document.</param>
		/// <param name="length">Length of the fragment in <see cref="Root"/> document.</param>
		/// <exception cref="ArgumentOutOfRangeException">
		/// <para>Index must be greater or equal to zero</para> 
		/// <para>Index cannot exceed document length</para> or
		/// <para>Length must be greater or equal to zero</para> or
		/// <para>Index plus length cannot exceed document length</para>
		/// </exception>
		internal void Set(int index, int length)
		{
			if( index < 0 )
				throw new ArgumentOutOfRangeException("index", "Index must be greater or equal to zero");

			if (this._Root != null)
                if( index > this._Root.Value.Length - 1 )
				    throw new ArgumentOutOfRangeException("index", "Index cannot exceed document length");

			if( length < 0 )
				throw new ArgumentOutOfRangeException("length", "Length must be greater or equal to zero");

            if (this._Root != null)
			    if( index + length > this._Root.Value.Length )
				    throw new ArgumentOutOfRangeException("index", "Index plus length cannot exceed document length");

			this._Index = index;
			this._Length = length;

			this._LineNo = 1;

            var aspx = string.Empty;

			if (this._Root != null)
                    aspx = this._Root.Value.Substring(0, index);
			
            int lastLineEnd = -1;
			for(int i = 0; i < index; i++)
			{
				if( aspx[i] == '\n' )
				{
					this._LineNo ++;
					lastLineEnd = i;
				}
			}

			this._ColumnNo = index - lastLineEnd;
		}

		/// <summary>
		/// Gets the text of the fragment.
		/// </summary>
		/// <value>
		/// The text of the fragment.
		/// </value>
		/// <exception cref="InvalidOperationException">Index and length not defined.</exception>
		public string Text
		{
			get 
			{
				if( ! this.Defined )
					throw new InvalidOperationException("Index and length not defined.");

                if (this._Root == null)
                    return string.Empty;

				return this._Root.Value.Substring(this._Index, this._Length); 
			}
		}

		/// <summary>
		/// Gets value indicating whether the fragment is defined.
		/// </summary>
		/// <value>
		/// <b>true</b> if the fragment is defined; otherwise, <b>false</b>.
		/// </value>
		public bool Defined
		{
			get { return (this._Index != -1 && this._Length != -1); }
		}

		/// <summary>
		/// Initializes a new instance of the <b>DocumentFragment</b> class
		/// with the specified <see cref="Document"/> that the fragment belongs to.
		/// </summary>
		/// <param name="root"><see cref="Document"/> to which the newly created fragment belongs to.</param>
		internal DocumentFragment(Document? root)
		{
			if (root != null)
                this._Root = root;

			this._Index    = -1;
			this._Length   = -1;
			this._LineNo   = -1;
			this._ColumnNo = -1;
		}
	}
}
