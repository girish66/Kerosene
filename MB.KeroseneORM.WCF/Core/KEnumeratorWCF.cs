// ========================================================
#undef DEBUG
namespace MB.KeroseneORM.WCF
{
	using global::System.Diagnostics;
	using global::System;
	using global::MB.Tools;

	// =====================================================
	/// <summary>
	/// Represents an object able to enumerate through the results of a command executed against a WCF database service.
	/// </summary>
	public class KEnumeratorWCF : KEnumerator
	{
		Guid _EnumeratorId = Guid.Empty;
		KSchema _Schema = null;

		/// <summary>
		/// Creates a new <see cref="KEnumeratorWCF"/> instance for the command given.
		/// </summary>
		/// <param name="command">The command this enumerator is created for.</param>
		public KEnumeratorWCF( IKCommandEnumerable command ) : base( command )
		{
			DEBUG.IndentLine( "\n-- KEnumeratorWCF( Command )" );

			var text = command.CommandText( iterable: true );
			var pars = command.Parameters;
			_EnumeratorId = Link.Proxy.EnumeratorCreate( text, pars );

			DEBUG.Unindent();
		}
		protected override void Dispose( bool disposing )
		{
			DEBUG.IndentLine( "\n-- KEnumeratorWCF.Dispose( Disposing={0} ) - This={1}", disposing, this );

			if( _EnumeratorId != Guid.Empty ) {
				if( Link != null && Link.Proxy != null ) Link.Proxy.EnumeratorDispose( _EnumeratorId );
				_EnumeratorId = Guid.Empty;
			}
			_Schema = null;

			base.Dispose( disposing ); // Calls-back Reset() - but with no effects...
			DEBUG.Unindent();
		}

		public override string ToString()
		{
			return string.Format( "KEnumeratorWCF[ {0} = {1} ]",
				_EnumeratorId.TagString(),
				Command == null ? "null" : Command.ToString() );
		}

		/// <summary>
		/// Gets the WCF link this enumerator is associated with, as obtained from the command instance maintaned by this
		/// enumerator.
		/// </summary>
		public new KLinkWCF Link
		{
			get { return (KLinkWCF)base.Link; }
		}

		/// <summary>
		/// Gets the schema describing the columns and contents of the records returned.
		/// </summary>
		public override KSchema Schema
		{
			get
			{
				if( _Schema == null ) {
					_Schema = Link.Proxy.EnumeratorSchema( _EnumeratorId );
					_Schema = _Schema.Clone(); // To avoid call-back WCF induced disposition...

					var cmd = Command as IKTableAliasListProvider;
					if( cmd != null ) _Schema.TableAliasList.AddRange( cmd.TableAliasList );
				}
				return _Schema;
			}
		}

		/// <summary>
		/// Gets the current <see cref="KRecord"/> instance.
		/// </summary>
		public override KRecord CurrentRecord
		{
			get
			{
				KRecord record = Link.Proxy.EnumeratorCurrent( _EnumeratorId );
				record.Schema = this.Schema;
				return record;
			}
		}

		/// <summary>
		/// Advances this enumerator to return the next element from the database. In this implementation these elements
		/// are not maintained in an in-memory structure, but rather are retrieved and maintained in the CurrentRecord
		/// property just until the next invocation of MoveNext().
		/// </summary>
		/// <returns>True if the next element has been obtained, false if there are no more elements available.</returns>
		public override bool MoveNext()
		{
			bool r = Link.Proxy.EnumeratorMoveNext( _EnumeratorId );
			return r;
		}

		/// <summary>
		/// Resets this enumerator.
		/// </summary>
		public override void Reset()
		{
			if( _EnumeratorId != Guid.Empty ) {
				if( Link != null && Link.Proxy != null ) Link.Proxy.EnumeratorReset( _EnumeratorId );
				_EnumeratorId = Guid.Empty;
			}
			_Schema = null; // Do not dispose, might be in use elsewhere...
		}
	}
}
// ========================================================
