// ========================================================
#undef DEBUG
namespace MB.KeroseneORM.WCF
{
	using global::System.Diagnostics;
	using global::System;
	using global::MB.Tools;

	// =====================================================
	/// <summary>
	/// Represents an object able to execute a command against a WCF database service and able to return just the number of
	/// records affected.
	/// </summary>
	public class KExecutorWCF : KExecutor
	{
		Guid _ExecutorId = Guid.Empty;

		/// <summary>
		/// Creates a new <see cref="KExecutorWCF"/> instance for the given command.
		/// </summary>
		/// <param name="command">The command this executor will be created for.</param>
		public KExecutorWCF( IKCommandExecutable command ) : base( command )
		{
			DEBUG.IndentLine( "\n-- KExecutorWCF( Command )" );

			var pars = command.Parameters.Clone(); foreach( var par in pars ) par.Value = command.Link.TransformParameterValue( par.Value );
			var text = command.CommandText( iterable: true );

			_ExecutorId = Link.Proxy.ExecutorCreate( text, pars );

			DEBUG.Unindent();
		}
		protected override void Dispose( bool disposing )
		{
			DEBUG.IndentLine( "\n-- KExecutorWCF.Dispose( Disposing={0} ) - This={1}", disposing, this );

			if( _ExecutorId != Guid.Empty ) {
				if( Link != null && Link.Proxy != null ) Link.Proxy.ExecutorDispose( _ExecutorId );
				_ExecutorId = Guid.Empty;
			}

			base.Dispose( disposing );
			DEBUG.Unindent();
		}

		public override string ToString()
		{
			return string.Format( "KExecutorWCF[ {0} = {1} ]",
				_ExecutorId.TagString(),
				Command == null ? "null" : Command.ToString() );
		}

		/// <summary>
		/// Gets the direct link this enumerator is associated with, as obtained from the command instance maintaned by this
		/// enumerator.
		/// </summary>
		public new KLinkWCF Link
		{
			get { return (KLinkWCF)base.Link; }
		}

		/// <summary>
		/// Executes the command and returns the number of affected records.
		/// </summary>
		/// <returns>The number of records affected.</returns>
		public override int Execute()
		{
			return Link.Proxy.ExecutorExecute( _ExecutorId );
		}
	}
}
// ========================================================
