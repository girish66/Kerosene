// ========================================================
#undef DEBUG
namespace MB.KeroseneORM
{
	using global::System.Diagnostics;
	using global::System;
	using global::MB.Tools;

	// =====================================================
	/// <summary>
	/// Provides a base implementation for the <see cref="IKExecutor"/> interface.
	/// </summary>
	public abstract class KExecutor : IKExecutor
	{
		IKCommandExecutable _Command = null;

		protected KExecutor( IKCommandExecutable command )
		{
			DEBUG.IndentLine( "\n-- KExecutor( Command={0} )", command == null ? "null" : command.ToString() );
			if( ( _Command = command ) == null ) throw new ArgumentNullException( "command", "Command cannot be null." );
			DEBUG.Unindent();
		}

		protected virtual void Dispose( bool disposing )
		{
			DEBUG.IndentLine( "\n-- KExecutor.Dispose( Disposing={0} ) - This={1}", disposing, this );
			_Command = null;
			DEBUG.Unindent();
		}
		public void Dispose()
		{
			DEBUG.IndentLine( "\n-- KExecutor.Dispose() - This={0}", this );
			Dispose( true );
			GC.SuppressFinalize( this );
			DEBUG.Unindent();
		}
		~KExecutor()
		{
			DEBUG.IndentLine( "\n-- KExecutor~() - This={0}", this );
			Dispose( false );
			DEBUG.Unindent();
		}

		public override string ToString()
		{
			return string.Format( "KExecutor[ {0} ]", _Command == null ? "null" : _Command.ToString() );
		}

		/// <summary>
		/// Gets the command this executor is associated with.
		/// </summary>
		public IKCommandExecutable Command
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
		/// Executes the command and returns the number of affected records.
		/// </summary>
		/// <returns>The number of records affected.</returns>
		public abstract int Execute();
	}
}
// ========================================================
