// ========================================================
#undef DEBUG
namespace MB.KeroseneORM.Direct
{
	using global::System.Diagnostics;
	using global::System;
	using global::MB.Tools;

	// =====================================================
	/// <summary>
	/// Represents an object able to execute a command against a direct database and able to return just the number of
	/// records affected.
	/// </summary>
	public class KExecutorDirect : KExecutor
	{
		/// <summary>
		/// Creates a new <see cref="KExecutorDirect"/> instance for the given command.
		/// </summary>
		/// <param name="command">The command this executor will be created for.</param>
		public KExecutorDirect( IKCommandExecutable command ) : base( command )
		{
			DEBUG.IndentLine( "\n-- KExecutorDirect( Command )" );
			DEBUG.Unindent();
		}
		protected override void Dispose( bool disposing )
		{
			DEBUG.IndentLine( "\n-- KExecutorDirect.Dispose( Disposing={0} ) - This={1}", disposing, this );
			base.Dispose( disposing );
			DEBUG.Unindent();
		}

		public override string ToString()
		{
			return string.Format( "KExecutorDirect[ {0} ]", Command == null ? "null" : Command.ToString() );
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
		/// Executes the command and returns the number of affected records.
		/// </summary>
		/// <returns>The number of records affected.</returns>
		public override int Execute()
		{
			var surrogate = new KSurrogate( this.Command, iterable: false );
			int r = surrogate._NonQueryResult;

			surrogate.Dispose(); surrogate = null;
			return r;
		}
	}
}
// ========================================================
