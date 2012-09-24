// ========================================================
#undef DEBUG
namespace MB.KeroseneORM.WCF
{
	using global::System.Diagnostics;
	using global::System;
	using global::System.ServiceModel;
	using global::MB.Tools;
	using global::MB.Tools.Dynamics;

	// =====================================================
	/// <summary>
	/// Represents a link object for a WCF database service. This service is responsible to connect to the real database
	/// on behalf of this link object, or to fulfill the requests in any other way it decides.
	/// </summary>
	public class KLinkWCF : KLink, IKLink
	{
		IKProxyWCF _Proxy = null; Guid _ProxyId = Guid.Empty;
		string _EndPoint = null;
		DeepObject _Package = null;

		/// <summary>
		/// Creates a new	<see cref="KLinkWCF"/> for the personality of the database defined by the factory argument.
		/// </summary>
		/// <param name="factory">The factory defining the personality of the database.</param>
		public KLinkWCF( IKFactory factory ) : base( factory )
		{
			DEBUG.IndentLine( "\n-- KLinkWCF( Factory )" );
			DEBUG.Unindent();
		}
		
		/// <summary>
		/// Creates a new	<see cref="KLinkWCF"/> for the personality of the database defined by the factory argument, and
		/// connects to the service located by the endpoint argument using the connection package given.
		/// </summary>
		/// <param name="factory">The factory defining the personality of the database.</param>
		/// <param name="endpoint">The endpoint to use to locate the WCF service.</param>
		/// <param name="package">The connection package to use by the service to modulate how it will connect with the real
		/// underlying database, if needed, or null if it is not needed.</param>
		public KLinkWCF( IKFactory factory, string endpoint, DeepObject package = null ) : base( factory )
		{
			DEBUG.IndentLine( "\n-- KLinkWCF( Factory, EndPoint={0}, Package={1} )", endpoint ?? "<null>", package == null ? "<null>" : package.ToString() );
			ProxyConnect( endpoint, package );
			DEBUG.Unindent();
		}
		
		protected override void Dispose( bool disposing )
		{
			DEBUG.IndentLine( "\n-- KLinkWCF.Dispose( Disposing={0} ) - This={1}", disposing, this );
			ProxyDisconnect();
			base.Dispose( disposing ); // Calls-back DbClose()...
			DEBUG.Unindent();
		}

		public override string ToString()
		{
			return string.Format( "KLinkWCF[ EndPoint={0}, Proxy={1}, Package={2} ]",
				_EndPoint ?? "<null>",
				_ProxyId.TagString(),
				_Package == null ? "<null>" : _Package.ToString() );
		}

		/// <summary>
		/// Clones this link object.
		/// </summary>
		/// <returns>The new cloned link.</returns>
		public KLinkWCF Clone()
		{
			KLinkWCF cloned = new KLinkWCF( Factory ); OnClone( cloned );
			if( Endpoint != null ) cloned.ProxyConnect( Endpoint, Package == null ? null : Package.Clone() );
			return cloned;
		}
		object ICloneable.Clone()
		{
			return this.Clone();
		}

		/// <summary>
		/// Gets the proxy instance to the WCF service this link object will use.
		/// </summary>
		public IKProxyWCF Proxy
		{
			get { return _Proxy; }
		}

		/// <summary>
		/// Gets the endpoint used to connect to the database. It might be null if this link is not connected yet.
		/// </summary>
		public string Endpoint
		{
			get { return _EndPoint; }
		}
		
		/// <summary>
		/// Gets the connection package used when connecting to the WCF service. It can be null if this link is not connected
		/// yet, or when it was not needed.
		/// </summary>
		public DeepObject Package
		{
			get { return _Package; }
		}

		/// <summary>
		/// Connects this link object with the WCF service. If it has been already connected, an exceptio will be thrown.
		/// </summary>
		/// <param name="endpoint">The endpoint to use to locate the WCF service.</param>
		/// <param name="package">The connection package to use by the service to modulate how it will connect with the real
		/// underlying database, if needed, or null if it is not needed.</param>
		public virtual void ProxyConnect( string endpoint, DeepObject package )
		{
			DEBUG.IndentLine( "\n-- KLinkWCF.ProxyConnect( EndPoint={0}, Package={1}", endpoint ?? "<null>", package == null ? "<null>" : package.ToString() );

			if( _Proxy != null ) throw new InvalidOperationException( "This link is already connected." );
			_EndPoint = endpoint.Validated( "EndPoint" );
			_Package = package;

			var channelFactory = new ChannelFactory<IKProxyWCF>( _EndPoint );
			_Proxy = channelFactory.CreateChannel();
			_ProxyId = _Proxy.ProxyConnect( _Package );

			DEBUG.WriteLine( "\n-- Connected with UID = {0}", _ProxyId.TagString() );
			DEBUG.Unindent();
		}
		
		/// <summary>
		/// Disconnects this link from the WCF service, freeing all the server resources it might have been used. If it was
		/// not connected, this method merely returns.
		/// </summary>
		public virtual void ProxyDisconnect()
		{
			DEBUG.IndentLine( "\n-- [{0}] KLinkWCF.ProxyDisconnect()", _ProxyId.TagString() );

			if( _Proxy != null ) {
				_Proxy.ProxyDisconnect();
				_Proxy.Dispose();
				_Proxy = null;
				_ProxyId = Guid.Empty;

				_Package = null;
				_EndPoint = null;
				_DbCaseSensitiveNames = null;
			}

			DEBUG.Unindent();
		}
		
		/// <summary>
		/// Gets whether this link object is connected or not.
		/// </summary>
		public bool IsConnected
		{
			get { return _Proxy == null ? false : true; }
		}

		/// <summary>
		/// Gets whether the names of the tables and columns in the underlying database should be consider as case sensitive
		/// or not. For performance reasons the implementation of this property caches this value avoiding to call the WCF
		/// service more than one time.
		/// </summary>
		public override bool DbCaseSensitiveNames
		{
			get
			{
				if( _Proxy == null ) throw new InvalidOperationException( "This link is not connected." );
				if( _DbCaseSensitiveNames == null ) {
					_DbCaseSensitiveNames = _Proxy.DbCaseSensitiveNames();
				}
				return (bool)_DbCaseSensitiveNames;
			}
		}
		bool? _DbCaseSensitiveNames = null;

		/// <summary>
		/// Gets whether a connection maintained by the WCF service on behalf of this link object exists and is in an opened
		/// state.
		/// </summary>
		public override bool IsDbOpened
		{
			get
			{
				if( _Proxy == null ) throw new InvalidOperationException( "This link is not connected." );
				return _Proxy.IsDbOpened();
			}
		}

		/// <summary>
		/// Creates and opens manually the connection the WCF service maintains with the underlying database on behalf of
		/// this link object. If a connection already exists, it is possible that an exception will be thrown depending
		/// upon how the WCF service might react.
		/// <para>This method has little use as this connection is maintained automatically by the WCF service, and there is
		/// no way to access to it from this client-side link.</para>
		/// </summary>
		public override void DbOpen()
		{
			if( _Proxy == null ) throw new InvalidOperationException( "This link is not connected." );
			_Proxy.DbOpen();
		}
		
		/// <summary>
		/// Manually closes and destroys the connection the WCF service maintains with the underlying database on behalf of
		/// this link object. It such connection does not exist, it is assumed that the WCF service will merely return but
		/// will not throw any exception.
		/// <para>This method has little use as this connection is maintained automatically by the WCF service, and there is
		/// no way to access to it from this client-side link.</para>
		/// </summary>
		public override void DbClose()
		{
			if( _Proxy == null ) {
				DEBUG.WriteLine( "\n-- This link is not connected." );
				return; // DbClose() might be called from Dispose();
			}
			_Proxy.DbClose();
		}

		/// <summary>
		/// Gets or sets the default transactional mode to use by the WCF service to create automatic transactions on the
		/// server-side when such operation is. If a transaction already exists, it is possible that an exception will be
		/// thrown by the WCF service.
		/// </summary>
		public override KTransactionMode TransactionMode
		{
			get
			{
				if( _Proxy == null ) throw new InvalidOperationException( "This link is not connected." );
				return _Proxy.TransactionMode();
			}
			set
			{
				if( _Proxy == null ) throw new InvalidOperationException( "This link is not connected." );
				_Proxy.SetTransactionMode( value );
			}
		}

		/// <summary>
		/// Gets the transactional state of the proxy maintained by the WCF service to attend this link object. If this link
		/// object is not yet connected, an Empty state is returned by default.
		/// </summary>
		public override KTransactionState TransactionState
		{
			get
			{
				if( _Proxy == null ) {
					DEBUG.WriteLine( "\n-- This link is not connected." );
					return KTransactionState.Empty; // Might be call from DbClose() and/or Dispose();
				}
				return _Proxy.TransactionState();
			}
		}

		/// <summary>
		/// Creates a transaction on the server-side proxy this link object refers to, using the transactional mode that
		/// has been established for the proxy though this link object. It is assumed that the WCF service will accept
		/// nested transactions.
		/// </summary>
		public override void TransactionStart()
		{
			if( _Proxy == null ) throw new InvalidOperationException( "This link is not connected." );
			_Proxy.TransactionStart();
		}
		
		/// <summary>
		/// Commits the transaction that might be active on the server-side. If no such transaction exists, it is assumed
		/// the WCF service will merely returns. Also, it is assumed it will manage nested transactions as needed.
		/// </summary>
		public override void TransactionCommit()
		{
			if( _Proxy == null ) throw new InvalidOperationException( "This link is not connected." );
			_Proxy.TransactionCommit();
		}
		
		/// <summary>
		/// Aborts the current transaction that might exists at the server-side, setting its state to Aborted.
		/// </summary>
		public override void TransactionAbort()
		{
			if( _Proxy == null ) throw new InvalidOperationException( "This link is not connected." );
			_Proxy.TransactionAbort();
		}

		/// <summary>
		/// Creates a new <see cref="KEnumeratorWCF"/> instance associated with the given command.
		/// </summary>
		/// <param name="command">The command the enumerator will be associated with.</param>
		/// <returns>The new enumerator created.</returns>
		public KEnumeratorWCF CreateEnumerator( IKCommandEnumerable command )
		{
			return new KEnumeratorWCF( command );
		}
		IKEnumerator IKLink.CreateEnumerator( IKCommandEnumerable command )
		{
			return this.CreateEnumerator( command );
		}

		/// <summary>
		/// Creates a new <see cref="KExecutorWCF"/> instance associated with the given command.
		/// </summary>
		/// <param name="command">The command the executor will be associated with.</param>
		/// <returns>The new executor created.</returns>
		public KExecutorWCF CreateExecutor( IKCommandExecutable command )
		{
			return new KExecutorWCF( command );
		}
		IKExecutor IKLink.CreateExecutor( IKCommandExecutable command )
		{
			return this.CreateExecutor( command );
		}
	}
}
// ========================================================
