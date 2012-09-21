// ========================================================
#undef DEBUG
namespace MB.KeroseneORM
{
	using global::System.Diagnostics;
	using global::System;
	using global::System.Collections.Generic;
	using global::MB.Tools;

	// =====================================================
	/// <summary>
	/// Represents a link with a given database. It is an agnostic object, so for instance this link can be either against
	/// a direct physical one, or through a surrogate that this link object will communicate with, etc.
	/// </summary>
	public interface IKLink : IDisposable, ICloneable
	{
		/// <summary>
		/// Gets the factory that describes the personality of the database to communicate with.
		/// </summary>
		IKFactory Factory { get; }

		/// <summary>
		/// Gets the dictionary instance that carries arbitrary extended information for this link object.
		/// </summary>
		Dictionary<string, object> ExtendedInfo { get; }

		/// <summary>
		/// Gets whether the names of tables and columns in the database are to be considered case sensitive or not.
		/// </summary>
		bool DbCaseSensitiveNames { get; }

		/// <summary>
		/// Opens the connection with the database. It might throw an exception if a connection is already opened.
		/// </summary>
		void DbOpen();
		
		/// <summary>
		/// Closes, if exists, the connection with the database.
		/// </summary>
		void DbClose();
		
		/// <summary>
		/// Gets whether a connection with the database exists and is opened.
		/// </summary>
		bool IsDbOpened { get; }

		/// <summary>
		/// Gets or sets the default transaction mode to use with this link.
		/// <para>If a transaction is currently active, the setter might throw an exception.</para>
		/// </summary>
		KTransactionMode TransactionMode { get; set; }

		/// <summary>
		/// Gets the transacional state of this link when this property is obtained.
		/// </summary>
		KTransactionState TransactionState { get; }
		
		/// <summary>
		/// Starts a transaction using the default transaction mode.
		/// <para>The actual implementation might accept nested transactions or not.</para>
		/// </summary>
		void TransactionStart();
		
		/// <summary>
		/// Commits the current transaction. It should not throw an exception if no transaction exists.
		/// </summary>
		void TransactionCommit();
		
		/// <summary>
		/// Aborts the current transaction. It should not throw an exception if no transaction exists.
		/// </summary>
		void TransactionAbort();

		/// <summary>
		/// Creates a new <see cref="IKEnumerator"/> instance associated with the given command.
		/// </summary>
		/// <param name="command">The command the enumerator will be associated with.</param>
		/// <returns>The new enumerator created.</returns>
		IKEnumerator CreateEnumerator( IKCommandEnumerable command );

		/// <summary>
		/// Creates a new <see cref="IKExecutor"/> instance associated with the given command.
		/// </summary>
		/// <param name="command">The command the executor will be associated with.</param>
		/// <returns>The new executor created.</returns>
		IKExecutor CreateExecutor( IKCommandExecutable command );
	}

	// =====================================================
	/// <summary>
	/// Represents the state if the current transaction of a link object.
	/// </summary>
	public enum KTransactionState
	{
		/// <summary>
		/// There is no transaction active.
		/// </summary>
		Empty,

		/// <summary>
		/// A transaction is currently active.
		/// </summary>
		Active,

		/// <summary>
		/// The last transaction has been aborted.
		/// <para>This state is only cleared when a new transaction is initiated.</para>
		/// </summary>
		Aborted
	}

	// =====================================================
	/// <summary>
	/// Represents the default transaction mode to use with a link object.
	/// </summary>
	public enum KTransactionMode
	{
		/// <summary>
		/// To use the database transaction mode.
		/// </summary>
		Database,
		
		/// <summary>
		/// To use the TransactionScope transaction mechanism.
		/// </summary>
		Scope
	}

	// =====================================================
	/// <summary>
	/// Represents an object that, in order be cloned, needs a <see cref="IKLink"/> instance to create the new clone
	/// to return, instead of having a parameterless Clone() method.
	/// </summary>
	public interface IKCloneable
	{
		/// <summary>
		/// Clones this object associating it with the given <see cref="IKLink"/> instance.
		/// </summary>
		/// <param name="link">The instance of the link the cloned object will be associated with.</param>
		/// <returns>A new clone associated with the given link instance.</returns>
		object Clone( IKLink link );
	}

	// =====================================================
	/// <summary>
	/// Provides a base implementation for the <see cref="IKLink"/> interface
	/// </summary>
	public abstract class KLink : IKLink
	{
		IKFactory _Factory = null;
		Dictionary<string, object> _ExtendedInfo = new Dictionary<string, object>();

		protected KLink( IKFactory factory )
		{
			DEBUG.IndentLine( "\n-- KLink( Factory={0} )", factory == null ? "null" : factory.ToString() );
			if( ( _Factory = factory ) == null ) throw new ArgumentNullException( "factory", "Factory cannot be null." );
			DEBUG.Unindent();
		}

		protected virtual void Dispose( bool disposing )
		{
			DEBUG.IndentLine( "\n-- KLink.Dispose( Disposing={0} ) - This={1}", disposing, this );

			if( TransactionState == KTransactionState.Active ) TransactionAbort();
			DbClose();

			if( _ExtendedInfo != null ) {
				List<IDisposable> list = new List<IDisposable>();
				foreach( var kvp in _ExtendedInfo ) { if( kvp.Value != null && kvp.Value is IDisposable ) list.Add( (IDisposable)kvp.Value ); }
				foreach( var item in list ) item.Dispose();
				list.Clear(); list = null;

				_ExtendedInfo.Clear();
				_ExtendedInfo = null;
			}
			_Factory = null;

			DEBUG.Unindent();
		}
		public void Dispose()
		{
			DEBUG.IndentLine( "\n-- KLink.Dispose() - This={0}", this );
			Dispose( true );
			GC.SuppressFinalize( this );
			DEBUG.Unindent();
		}
		~KLink()
		{
			DEBUG.IndentLine( "\n-- KLink~() - This={0}", this );
			Dispose( false );
			DEBUG.Unindent();
		}

		public override string ToString()
		{
			return string.Format( "KLink[ {0} ]", _Factory == null ? "null" : _Factory.ToString() );
		}

		protected virtual void OnClone( KLink cloned )
		{
			foreach( var kvp in ExtendedInfo ) {
				if( kvp.Value != null ) {
					if( kvp.Value is IKCloneable ) cloned.ExtendedInfo.Add( kvp.Key, ( (IKCloneable)kvp.Value ).Clone( cloned ) );
					else cloned.ExtendedInfo.Add( kvp.Key, TypeHelper.TryClone( kvp.Value ) );
				}
			}
		}
		object ICloneable.Clone() { throw new NotImplementedException( "Abstract KLink.Clone() called." ); }

		/// <summary>
		/// Gets the factory that describes the personality of the database to communicate with.
		/// </summary>
		public IKFactory Factory
		{
			get { return _Factory; }
		}

		/// <summary>
		/// Gets the dictionary instance that carries arbitrary extended information for this link object.
		/// </summary>
		public Dictionary<string, object> ExtendedInfo
		{
			get { return _ExtendedInfo; }
		}

		/// <summary>
		/// Gets whether the names of tables and columns in the database are to be considered case sensitive or not.
		/// </summary>
		public abstract bool DbCaseSensitiveNames { get; }

		/// <summary>
		/// Opens the connection with the database.
		/// <para>It might throw an exception if a connection is already opened.</para>
		/// </summary>
		public abstract void DbOpen();

		/// <summary>
		/// Closes, if exists, the connection with the database.
		/// </summary>
		public abstract void DbClose();

		/// <summary>
		/// Gets whether a connection with the database exists and is opened.
		/// </summary>
		public abstract bool IsDbOpened { get; }

		/// <summary>
		/// Gets or sets the default transaction mode to use with this link.
		/// <para>If a transaction is currently active, the setter might throw an exception.</para>
		/// </summary>
		public abstract KTransactionMode TransactionMode { get; set; }

		/// <summary>
		/// Gets the transacional state of this link when this property is obtained.
		/// </summary>
		public abstract KTransactionState TransactionState { get; }

		/// <summary>
		/// Starts a transaction using the default transaction mode.
		/// <para>The actual implementation might accept nested transactions or not.</para>
		/// </summary>
		public abstract void TransactionStart();

		/// <summary>
		/// Commits the current transaction. It should not throw an exception if no transaction exists.
		/// </summary>
		public abstract void TransactionCommit();

		/// <summary>
		/// Aborts the current transaction. It should not throw an exception if no transaction exists.
		/// </summary>
		public abstract void TransactionAbort();

		IKEnumerator IKLink.CreateEnumerator( IKCommandEnumerable command ) { throw new NotImplementedException( "Abstract KLink.CreateEnumerator() called." ); }
		IKExecutor IKLink.CreateExecutor( IKCommandExecutable command ) { throw new NotImplementedException( "Abstract KLink.CreateExecutor() called." ); }
	}
}
// ========================================================
