// ========================================================
#undef DEBUG
namespace MB.KeroseneORM.Direct
{
	using global::System.Diagnostics;
	using global::System;
	using global::MB.Tools;

	// =====================================================
	/// <summary>
	/// Represents an object able to enumerate through the results of a command executed against a direct database.
	/// </summary>
	public class KEnumeratorDirect : KEnumerator
	{
		KSurrogate _Surrogate = null;
		KSchema _Schema = null;
		KRecord _CurrentRecord = null;

		/// <summary>
		/// Creates a new <see cref="KEnumeratorDirect"/> instance for the command given.
		/// </summary>
		/// <param name="command">The command this enumerator is created for.</param>
		public KEnumeratorDirect( IKCommandEnumerable command ) : base( command )
		{
			DEBUG.IndentLine( "\n-- KEnumeratorDirect( Command )" );
			DEBUG.Unindent();
		}
		protected override void Dispose( bool disposing )
		{
			DEBUG.IndentLine( "\n-- KEnumeratorDirect.Dispose( Disposing={0} ) - This={1}", disposing, this );
			base.Dispose( disposing ); // Calls-back Reset()...
			DEBUG.Unindent();
		}

		public override string ToString()
		{
			return string.Format( "KEnumeratorDirect[ {0} ]", Command == null ? "null" : Command.ToString() );
		}

		/// <summary>
		/// Gets the direct link this enumerator is associated with, as obtained from the command instance maintaned by this
		/// enumerator.
		/// </summary>
		public new KLinkDirect Link
		{
			get { return (KLinkDirect)base.Link; }
		}

		/// <summary>
		/// Gets the schema describing the columns and contents of the records returned.
		/// </summary>
		public override KSchema Schema
		{
			get { return _Schema; }
		}

		/// <summary>
		/// Gets the current <see cref="KRecord"/> instance.
		/// </summary>
		public override KRecord CurrentRecord
		{
			get { return _CurrentRecord; }
		}

		/// <summary>
		/// Advances this enumerator to return the next element from the database.
		/// <para>In this implementation these elements are not maintained in an in-memory structure, but rather are retrieved
		/// and maintained in the CurrentRecord property just until the next invocation of MoveNext().
		/// </summary>
		/// <returns>True if the next element has been obtained, false if there are no more elements available.</returns>
		public override bool MoveNext()
		{
			// First iteration...
			if( _Surrogate == null ) {
				_Surrogate = new KSurrogate( this.Command, iterable: true );
				_Schema = _Surrogate._Schema;
			}

			// If we have finished, cleaning it up...
			if( !_Surrogate._DataReader.Read() ) {
				DEBUG.IndentLine( "\n-- MoveNext: null" ); DEBUG.Unindent();
				_Surrogate.Dispose(); _Surrogate = null;
				return false;
			}

			// Else, generating the record...			
			_CurrentRecord = new KRecord( _Schema ); for( int i = 0; i < _Schema.Count; i++ ) {
				object value = _Surrogate._DataReader.IsDBNull( i ) ? null : _Surrogate._DataReader.GetValue( i );
				_CurrentRecord[i] = value;
			}
			DEBUG.IndentLine( "\n-- MoveNext: {0}", CurrentRecord.ToString() ); DEBUG.Unindent();
			return true;
		}

		/// <summary>
		/// Resets this enumerator.
		/// </summary>
		public override void Reset()
		{
			if( _Surrogate != null ) { _Surrogate.Dispose(); _Surrogate = null; }
			_Schema = null;
			_CurrentRecord = null;
		}
	}
}
// ========================================================
