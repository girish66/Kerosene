// ========================================================
// #undef DEBUG
namespace MB.KeroseneORM.WCF.Server
{
	using global::System.Diagnostics;
	using global::System;
	using global::System.ServiceModel;
	using global::System.Collections.Generic;
	using global::System.Linq;
	using global::MB.Tools;
	using global::MB.Tools.Dynamics;

	// =====================================================
	/// <summary>
	/// Represents a WCF service that by implementing the <see cref="IKProxyWCF"/> will be able to connect to an underlying
	/// database on behalf of a client-side <see cref="KLinkWCF"/> object connected to it.
	/// </summary>
	[ServiceBehavior(
		InstanceContextMode = InstanceContextMode.PerSession, // Each client has its own session (proxy object)
		IncludeExceptionDetailInFaults = true // To send back the exceptions to the client
		)]
	public abstract class KServerWCF : IKProxyWCF
	{
		Guid _ProxyId = Guid.NewGuid();
		IKLink _Link = null;
		DeepObject _Package = null;
		Dictionary<Guid, object> _Elements = new Dictionary<Guid, object>();

		protected KServerWCF()
		{
			DEBUG.IndentLine( "\n-- KServerWCF() => UID={0}", _ProxyId.TagString() );
			DEBUG.Unindent();
		}

		protected virtual void Dispose( bool disposing )
		{
			DEBUG.IndentLine( "\n-- KServerWCF.Dispose( Disposing={0} ) - This={1}", disposing, this );

			ProxyDisconnect(); // For refactoring purposes
			_Elements = null;
			_ProxyId = Guid.Empty;

			DEBUG.Unindent();
		}
		public void Dispose()
		{
			DEBUG.IndentLine( "\n-- KServerWCF.Dispose() - This={0}", this );
			Dispose( true );
			GC.SuppressFinalize( this );
			DEBUG.Unindent();
		}
		~KServerWCF()
		{
			DEBUG.IndentLine( "\n-- KServerWCF~() - This={0}", this );
			Dispose( false );
			DEBUG.Unindent();
		}

		public override string ToString()
		{
			return string.Format( "KServerWCF[ UID={0} ]", _ProxyId.TagString() );
		}

		/// <summary>
		/// Gets the Guid that univocally identifies the server-side proxy created to attend the requests from a specific
		/// client-side <see cref="KLinkWCF"/> instance.
		/// </summary>
		public Guid ProxyId
		{
			get { return _ProxyId; }
		}

		/// <summary>
		/// Gets the actual link object created by this proxy to connect to the underlying database.
		/// </summary>
		public IKLink Link
		{
			get { return _Link; }
		}

		/// <summary>
		/// Gets the connection package sent by the client-side link when connecting to this proxy. It might be null if no
		/// such package was provided when connecting.
		/// </summary>
		public DeepObject Package
		{
			get { return _Package; }
		}

		/// <summary>
		/// The call-back method invoked by this proxy to create the instance of the actual link object to use to connect with
		/// the underlying database.
		/// </summary>
		/// <returns></returns>
		public abstract IKLink CreateLink();

		/// <summary>
		/// Call-back method to obtain the default transactional mode of the link object created in the server-side.
		/// </summary>
		/// <returns>The default transactional mode.</returns>
		public virtual KTransactionMode GetDefaultTransactionMode()
		{
			return KTransactionMode.Database; // By default
		}

		// --------------------------------------------------
		/// <summary>
		/// Returns the unique Guid that identifies this proxy object.
		/// </summary>
		/// <returns>The unique Guid of this proxy object.</returns>
		public Guid GetProxyId()
		{
			DEBUG.IndentLine( "\n-- [{0}] KServerWCF.GetProxyId()", _ProxyId.TagString() );
			try {
				return _ProxyId;
			}
			catch( Exception e ) { DEBUG.PrintException( e ); throw; }
			finally { DEBUG.Unindent(); }
		}

		/// <summary>
		/// Connects to the underlying database, creating the actual link object, using the optional connection package that
		/// might have been provided. This connection package is a dynamic object, an instance of <see cref="DeepObject"/>,
		/// able to carry an arbitrary number of properties and values as needed.
		/// <para>If this proxy is already connected with an underlying database, an exception will be thrown.</para>
		/// </summary>
		/// <param name="package">The connection package to use, or null.</param>
		/// <returns>The Guid of this proxy.</returns>
		public virtual Guid ProxyConnect( DeepObject package )
		{
			DEBUG.IndentLine( "\n-- [{0}] KServerWCF.ProxyConnect( Package={1} )", _ProxyId.TagString(), package == null ? "<null>" : package.ToString() );
			try {
				if( _Link != null ) throw new InvalidOperationException( "WCF Server is already connected." );

				_Package = package;
				_Link = CreateLink();
				if( _Link == null ) throw new InvalidOperationException( "Cannot create WCF Server's surrogate Link instance." );

				_Link.TransactionMode = GetDefaultTransactionMode();
				return _ProxyId;
			}
			catch( Exception e ) { DEBUG.PrintException( e ); throw; }
			finally { DEBUG.Unindent(); }
		}

		/// <summary>
		/// Disconnects this proxy from the client-side, and clears all the resources it holds.
		/// </summary>
		public virtual void ProxyDisconnect()
		{
			DEBUG.IndentLine( "\n-- [{0}] KServerWCF.ProxyDisconnect()", _ProxyId.TagString() );
			try {
				if( _Elements != null ) {
					foreach( var kvp in _Elements ) { if( kvp.Value != null && kvp.Value is IDisposable ) ( (IDisposable)kvp.Value ).Dispose(); }
					_Elements.Clear();
				}
				if( _Link != null ) { _Link.DbClose(); _Link = null; }
				_Package = null;
			}
			catch( Exception e ) { DEBUG.PrintException( e ); throw; }
			finally { DEBUG.Unindent(); }
		}

		/// <summary>
		/// Returns whether the table and column names in the underlying database are to be consider as case sensitive or not.
		/// </summary>
		/// <returns>Whether the table and column names are to be considered case sensitive or not.</returns>
		public bool DbCaseSensitiveNames()
		{
			DEBUG.IndentLine( "\n-- [{0}] KServerWCF.DbCaseSensitiveNames()", _ProxyId.TagString() );
			try {
				if( _Link == null ) throw new InvalidOperationException( "WCF Server is not yet connected." );
				return _Link.DbCaseSensitiveNames;
			}
			catch( Exception e ) { DEBUG.PrintException( e ); throw; }
			finally { DEBUG.Unindent(); }
		}
		
		/// <summary>
		/// Returns whether the connection against the underlying database is opened or not.
		/// </summary>
		/// <returns>Whether the connection against the underlying database is opened or not.</returns>
		public bool IsDbOpened()
		{
			DEBUG.IndentLine( "\n-- [{0}] KServerWCF.IsDbOpened()", _ProxyId.TagString() );
			try {
				if( _Link == null ) throw new InvalidOperationException( "WCF Server is not yet connected." );
				return _Link.IsDbOpened;
			}
			catch( Exception e ) { DEBUG.PrintException( e ); throw; }
			finally { DEBUG.Unindent(); }
		}
		
		/// <summary>
		/// Opens the connection against the underlying database. It may throw an exception if the connection is already
		/// opened.
		/// </summary>
		public void DbOpen()
		{
			DEBUG.IndentLine( "\n-- [{0}] KServerWCF.DbOpen()", _ProxyId.TagString() );
			try {
				if( _Link == null ) throw new InvalidOperationException( "WCF Server is not yet connected." );
				_Link.DbOpen();
			}
			catch( Exception e ) { DEBUG.PrintException( e ); throw; }
			finally { DEBUG.Unindent(); }
		}

		/// <summary>
		/// Closes the connection that might exist with the underlying database. If no such connection exists, it is assumed
		/// that this method will merely return without throwing any exceptions.
		/// </summary>
		public void DbClose()
		{
			DEBUG.IndentLine( "\n-- [{0}] KServerWCF.DbClose()", _ProxyId.TagString() );
			try {
				if( _Link == null ) throw new InvalidOperationException( "WCF Server is not yet connected." );
				_Link.DbClose();
			}
			catch( Exception e ) { DEBUG.PrintException( e ); throw; }
			finally { DEBUG.Unindent(); }
		}

		/// <summary>
		/// Sets the default transaction mode this proxy will use with the underlying database. If a transaction is already
		/// active, it might throw an exception.
		/// </summary>
		/// <param name="mode">The transaction mode to set as the default.</param>
		public void SetTransactionMode( KTransactionMode mode )
		{
			DEBUG.IndentLine( "\n-- [{0}] KServerWCF.SetTransactionMode( Mode={1} )", _ProxyId.TagString(), mode );
			try {
				if( _Link == null ) throw new InvalidOperationException( "WCF Server is not yet connected." );
				_Link.TransactionMode = mode;
			}
			catch( Exception e ) { DEBUG.PrintException( e ); throw; }
			finally { DEBUG.Unindent(); }
		}

		/// <summary>
		/// Gets the transactional mode this proxy object will use when needed.
		/// </summary>
		/// <returns>The default transactional mode.</returns>
		public KTransactionMode TransactionMode()
		{
			DEBUG.IndentLine( "\n-- [{0}] KServerWCF.TransactionMode()", _ProxyId.TagString() );
			try {
				if( _Link == null ) throw new InvalidOperationException( "WCF Server is not yet connected." );
				return _Link.TransactionMode;
			}
			catch( Exception e ) { DEBUG.PrintException( e ); throw; }
			finally { DEBUG.Unindent(); }
		}

		/// <summary>
		/// Returns the transactional state of this proxy object.
		/// </summary>
		/// <returns>The transactional state of this proxy object.</returns>
		public KTransactionState TransactionState()
		{
			DEBUG.IndentLine( "\n-- [{0}] KServerWCF.TransactionState()", _ProxyId.TagString() );
			try {
				if( _Link == null ) throw new InvalidOperationException( "WCF Server is not yet connected." );
				return _Link.TransactionState;
			}
			catch( Exception e ) { DEBUG.PrintException( e ); throw; }
			finally { DEBUG.Unindent(); }
		}

		/// <summary>
		/// Creates a transaction against the underlying database managed by this proxy object. It is assumed the surrogate
		/// link object will manage nested transactions as needed.
		/// </summary>
		public void TransactionStart()
		{
			DEBUG.IndentLine( "\n-- [{0}] KServerWCF.TransactionStart()", _ProxyId.TagString() );
			try {
				if( _Link == null ) throw new InvalidOperationException( "WCF Server is not yet connected." );
				_Link.TransactionStart();
			}
			catch( Exception e ) { DEBUG.PrintException( e ); throw; }
			finally { DEBUG.Unindent(); }
		}

		/// <summary>
		/// Commits the transaction that may exists, or merely returns otherwise. It is assumed the surrogate link object
		/// will manage nested transactions against the underlying database.
		/// </summary>
		public void TransactionCommit()
		{
			DEBUG.IndentLine( "\n-- [{0}] KServerWCF.TransactionCommit()", _ProxyId.TagString() );
			try {
				if( _Link == null ) throw new InvalidOperationException( "WCF Server is not yet connected." );
				_Link.TransactionCommit();
			}
			catch( Exception e ) { DEBUG.PrintException( e ); throw; }
			finally { DEBUG.Unindent(); }
		}

		/// <summary>
		/// Aborts the current transaction that may exists, setting the state to aborted.
		/// </summary>
		public void TransactionAbort()
		{
			DEBUG.IndentLine( "\n-- [{0}] KServerWCF.TransactionAbort()", _ProxyId.TagString() );
			try {
				if( _Link == null ) throw new InvalidOperationException( "WCF Server is not yet connected." );
				_Link.TransactionAbort();
			}
			catch( Exception e ) { DEBUG.PrintException( e ); throw; }
			finally { DEBUG.Unindent(); }
		}

		/// <summary>
		/// Creates a new enumerator for the command specified by its text and parameters.
		/// </summary>
		/// <param name="text">The text of the iterable command.</param>
		/// <param name="pars">The arguments of the command.</param>
		/// <returns>The unique Guid identifying the new created enumerator.</returns>
		public virtual Guid EnumeratorCreate( string text, KParameterList pars )
		{
			DEBUG.IndentLine( "\n-- [{0}] KServerWCF.EnumeratorCreate( {1}{2} )", _ProxyId.TagString(),
				text ?? "<null>",
				pars == null ? "" : string.Format( " - {0}", pars ) );
			try {
				if( _Link == null ) throw new InvalidOperationException( "WCF Server is not yet connected." );

				text = text.Validated( "Command Text" );
				IKCommandEnumerable cmd = _Link.Raw( text );
				if( pars != null ) foreach( var par in pars ) cmd.Parameters.Add( par.Clone() );

				IKEnumerator reader = cmd.GetEnumerator();
				if( reader == null ) throw new InvalidOperationException( "Cannot create Reader for command: " + cmd.TraceString() );

				Guid uid = Guid.NewGuid();
				_Elements.Add( uid, reader );

				DEBUG.WriteLine( "\n-- Enumerator created with UID = {0}", uid.TagString() );
				return uid;
			}
			catch( Exception e ) { DEBUG.PrintException( e ); throw; }
			finally { DEBUG.Unindent(); }
		}
		
		/// <summary>
		/// Disposes the enumerator identified by the given Guid.
		/// </summary>
		/// <param name="uid">The Guid of the enumerator.</param>
		public void EnumeratorDispose( Guid uid )
		{
			DEBUG.IndentLine( "\n-- [{0}] KServerWCF.EnumeratorDispose( {1} )", _ProxyId.TagString(), uid.TagString() );
			try {
				if( _Link == null ) throw new InvalidOperationException( "WCF Server is not yet connected." );

				object obj = null; if( _Elements.TryGetValue( uid, out obj ) ) {
					_Elements.Remove( uid );

					IKEnumerator reader = obj as IKEnumerator;
					if( reader != null ) {
						IKCommandEnumerable cmd = (IKCommandEnumerable)reader.Command;
						reader.Dispose();
						cmd.Dispose();
						return;
					}
				}
				throw new InvalidOperationException( "Reader not found: " + uid.TagString() );
			}
			catch( Exception e ) { DEBUG.PrintException( e ); throw; }
			finally { DEBUG.Unindent(); }
		}
		
		/// <summary>
		/// Resets the enumerator identified by the given Guid.
		/// </summary>
		/// <param name="uid">The Guid of the enumerator.</param>
		public void EnumeratorReset( Guid uid )
		{
			DEBUG.IndentLine( "\n-- [{0}] KServerWCF.EnumeratorReset( {1} )", _ProxyId.TagString(), uid.TagString() );
			try {
				if( _Link == null ) throw new InvalidOperationException( "WCF Server is not yet connected." );

				object obj = null; if( _Elements.TryGetValue( uid, out obj ) ) {
					IKEnumerator reader = obj as IKEnumerator;
					if( reader != null ) {
						reader.Reset();
						return;
					}
				}
				throw new InvalidOperationException( "Reader not found: " + uid.TagString() );
			}
			catch( Exception e ) { DEBUG.PrintException( e ); throw; }
			finally { DEBUG.Unindent(); }
		}
		
		/// <summary>
		/// Advances the enumerator identified by the given Guid to return the next available element.
		/// </summary>
		/// <param name="uid">The Guid of the enumerator.</param>
		/// <returns>True if the next element has been obtained, false if there are no more elements available.</returns>
		public bool EnumeratorMoveNext( Guid uid )
		{
			DEBUG.IndentLine( "\n-- [{0}] KServerWCF.EnumeratorMoveNext( {1} )", _ProxyId.TagString(), uid.TagString() );
			try {
				if( _Link == null ) throw new InvalidOperationException( "WCF Server is not yet connected." );

				object obj = null; if( _Elements.TryGetValue( uid, out obj ) ) {
					IKEnumerator reader = obj as IKEnumerator;
					if( reader != null ) {
						bool r = reader.MoveNext();
						return r;
					}
				}
				throw new InvalidOperationException( "Reader not found: " + uid.TagString() );
			}
			catch( Exception e ) { DEBUG.PrintException( e ); throw; }
			finally { DEBUG.Unindent(); }
		}
		
		/// <summary>
		/// Returns the schema produced by the execution of the enumerator identified by the given Guid.
		/// For technical reasons, the instance returned is a clone of the actual schema maintained by the actual enumerator.
		/// </summary>
		/// <param name="uid">The Guid of the enumerator.</param>
		/// <returns>The schema produced by the enumerator.</returns>
		public KSchema EnumeratorSchema( Guid uid )
		{
			DEBUG.IndentLine( "\n-- [{0}] KServerWCF.EnumeratorSchema( {1} )", _ProxyId.TagString(), uid.TagString() );
			try {
				if( _Link == null ) throw new InvalidOperationException( "WCF Server is not yet connected." );

				object obj = null; if( _Elements.TryGetValue( uid, out obj ) ) {
					IKEnumerator reader = obj as IKEnumerator;
					if( reader != null ) return reader.Schema.Clone(); // To avoid WCF induced disposition...
				}
				throw new InvalidOperationException( "Reader not found: " + uid.TagString() );
			}
			catch( Exception e ) { DEBUG.PrintException( e ); throw; }
			finally { DEBUG.Unindent(); }
		}
		
		/// <summary>
		/// Gets the current element of the enumerator identified by the given Guid, in the form of an instance of the
		/// <see cref="KRecord"/> class.
		/// </summary>
		/// <param name="uid">The Guid of the enumerator.</param>
		/// <returns>The current <see cref="KRecord"/> instance.</returns>
		public KRecord EnumeratorCurrent( Guid uid )
		{
			DEBUG.IndentLine( "\n-- [{0}] KServerWCF.EnumeratorCurrent( {1} )", _ProxyId.TagString(), uid.TagString() );
			try {
				if( _Link == null ) throw new InvalidOperationException( "WCF Server is not yet connected." );

				object obj = null; if( _Elements.TryGetValue( uid, out obj ) ) {
					IKEnumerator reader = obj as IKEnumerator;
					if( reader != null ) return reader.CurrentRecord;
				}
				throw new InvalidOperationException( "Reader not found: " + uid.TagString() );
			}
			catch( Exception e ) { DEBUG.PrintException( e ); throw; }
			finally { DEBUG.Unindent(); }
		}

		/// <summary>
		/// Creates a new executor for the command specified by its text and parameters.
		/// </summary>
		/// <param name="text">The text of the non-query command.</param>
		/// <param name="pars">The arguments of the command.</param>
		/// <returns>The unique Guid identifying the new created executor.</returns>
		public virtual Guid ExecutorCreate( string text, KParameterList pars )
		{
			DEBUG.IndentLine( "\n-- [{0}] KServerWCF.ExecutorCreate( {1}{2} )", _ProxyId.TagString(),
				text ?? "<null>",
				pars == null ? "" : string.Format( " - {0}", pars ) );
			try {
				if( _Link == null ) throw new InvalidOperationException( "WCF Server is not yet connected." );

				text = text.Validated( "Command Text" );
				IKCommandExecutable cmd = _Link.Raw( text );
				if( pars != null ) foreach( var par in pars ) cmd.Parameters.Add( par.Clone() );

				IKExecutor executor = cmd.GetExecutor();
				if( executor == null ) throw new InvalidOperationException( "Cannot create Executor for command: " + cmd.TraceString() );

				Guid uid = Guid.NewGuid();
				_Elements.Add( uid, executor );

				DEBUG.WriteLine( "\n-- Executor created with UID = {0}", uid.TagString() );
				return uid;
			}
			catch( Exception e ) { DEBUG.PrintException( e ); throw; }
			finally { DEBUG.Unindent(); }
		}

		/// <summary>
		/// Disposes the executor identified by the given Guid.
		/// </summary>
		/// <param name="uid">The Guid of the executor.</param>
		public void ExecutorDispose( Guid uid )
		{
			DEBUG.IndentLine( "\n-- [{0}] KServerWCF.ExecutorDispose( {1} )", _ProxyId.TagString(), uid.TagString() );
			try {
				if( _Link == null ) throw new InvalidOperationException( "WCF Server is not yet connected." );

				object obj = null; if( _Elements.TryGetValue( uid, out obj ) ) {
					_Elements.Remove( uid );

					IKExecutor executor = obj as IKExecutor;
					if( executor != null ) {
						IKCommandExecutable cmd = (IKCommandExecutable)executor.Command;
						executor.Dispose();
						cmd.Dispose();
						return;
					}
				}
				throw new InvalidOperationException( "Executor not found: " + uid.TagString() );
			}
			catch( Exception e ) { DEBUG.PrintException( e ); throw; }
			finally { DEBUG.Unindent(); }
		}
		
		/// <summary>
		/// Executes the executor identified by its Guid, and returns the number of records affected.
		/// </summary>
		/// <param name="uid">The Guid of the executor.</param>
		/// <returns>The number of records affected.</returns>
		public int ExecutorExecute( Guid uid )
		{
			DEBUG.IndentLine( "\n-- [{0}] KServerWCF.ExecutorExecute( {1} )", _ProxyId.TagString(), uid.TagString() );
			try {
				if( _Link == null ) throw new InvalidOperationException( "WCF Server is not yet connected." );

				object obj = null; if( _Elements.TryGetValue( uid, out obj ) ) {
					IKExecutor executor = obj as IKExecutor;
					if( executor != null ) {
						int r = executor.Execute();
						return r;
					}
				}
				throw new InvalidOperationException( "Executor not found: " + uid.TagString() );
			}
			catch( Exception e ) { DEBUG.PrintException( e ); throw; }
			finally { DEBUG.Unindent(); }
		}
	}
}
// ========================================================
