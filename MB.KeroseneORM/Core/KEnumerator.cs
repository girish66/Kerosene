// ========================================================
#undef DEBUG
namespace MB.KeroseneORM
{
	using global::System.Diagnostics;
	using global::System;
	using global::System.Collections;
	using global::MB.Tools;

	// =====================================================
	/// <summary>
	/// Provides a base implementation of the <see cref="IKEnumerator"/> interface.
	/// </summary>
	public abstract class KEnumerator : IKEnumerator
	{
		IKCommandEnumerable _Command = null;
		Func<KRecord, object> _Converter = null;
		bool _SealRecords = false;

		protected KEnumerator( IKCommandEnumerable command )
		{
			DEBUG.IndentLine( "\n-- KEnumerator( Command={0} )", command == null ? "null" : command.ToString() );

			if( ( _Command = command ) == null ) throw new ArgumentNullException( "command", "Command cannot be null." );

			DEBUG.Unindent();
		}

		protected virtual void Dispose( bool disposing )
		{
			DEBUG.IndentLine( "\n-- KEnumerator.Dispose( Disposing={0} ) - This={1}", disposing, this );

			Reset();
			_Converter = null;
			_Command = null;

			DEBUG.Unindent();
		}
		public void Dispose()
		{
			DEBUG.IndentLine( "\n-- KEnumerator.Dispose() - This={0}", this );
			Dispose( true );
			GC.SuppressFinalize( this );
			DEBUG.Unindent();
		}
		~KEnumerator()
		{
			DEBUG.IndentLine( "\n-- KEnumerator~() - This={0}", this );
			Dispose( false );
			DEBUG.Unindent();
		}

		public KEnumerator GetEnumerator()
		{
			return this;
		}
		IEnumerator IEnumerable.GetEnumerator()
		{
			return this.GetEnumerator();
		}

		public override string ToString()
		{
			return string.Format( "KEnumerator[ {0} ]", _Command == null ? "null" : _Command.ToString() );
		}

		/// <summary>
		/// The command this enumerator is associated with.
		/// </summary>
		public IKCommandEnumerable Command
		{
			get { return _Command; }
		}

		/// <summary>
		/// Gets the link this enumerator is associated with, as obtained from the command instance maintaned by this
		/// enumerator.
		/// </summary>
		public IKLink Link
		{
			get { return _Command == null ? null : _Command.Link; }
		}

		/// <summary>
		/// The converter to invoke to convert the results obtained from the database to whatever object the converter wants
		/// to produce.
		/// <para>This converter will receive the <see cref="KRecord"/> instance as thet are returned from the database, and
		/// can manipulate them the way it wishes to return any arbitrary object of any type (as far as it is a class instance
		/// and not a struct or a value object).</para>
		/// </summary>
		public Func<KRecord, object> Converter
		{
			get { return _Converter; }
			set { _Converter = value; }
		}

		/// <summary>
		/// Gets or sets whether to seal the records as soon as they are obtained from the database.
		/// </summary>
		public bool SealRecords
		{
			get { return _SealRecords; }
			set { _SealRecords = value; }
		}

		/// <summary>
		/// Gets the schema describing the columns and contents of the records returned.
		/// </summary>
		public abstract KSchema Schema { get; }

		/// <summary>
		/// Advances this enumerator to return the next element from the database.
		/// <para>Typically these elements won't be maintained in an in-memory structure, but rather retrieved one by one
		/// from the database.</para>
		/// <para>It will be the application's responsibility to treat or store them if needed as it wishes.</para>
		/// </summary>
		/// <returns>True if the next element has been obtained, false if there are no more elements available.</returns>
		public abstract bool MoveNext();

		/// <summary>
		/// Resets this enumerator.
		/// </summary>
		public abstract void Reset();

		/// <summary>
		/// Gets the current <see cref="KRecord"/> instance.
		/// </summary>
		public abstract KRecord CurrentRecord { get; }

		/// <summary>
		/// Gets the current element of this enumerator. It can be either the original <see cref="KRecord"/> instance
		/// obtained from the database, or the object produced by the converted assigned to this enumerator, if such
		/// exists.
		/// </summary>
		public object Current
		{
			get
			{
				KRecord record = CurrentRecord; if( _SealRecords && record != null ) record.Sealed = true;
				object r = record == null ? null : ( _Converter == null ? record : _Converter( record ) );
				return r;
			}
		}
	}
}
// ========================================================
