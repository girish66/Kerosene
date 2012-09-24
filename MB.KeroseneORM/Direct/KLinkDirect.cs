// ========================================================
#undef DEBUG
namespace MB.KeroseneORM.Direct
{
	using global::System.Diagnostics;
	using global::System;
	using global::System.Data;
	using global::System.Transactions;
	using global::System.Linq;
	using global::MB.Tools;

	// =====================================================
	/// <summary>
	/// Represents a link object with a given direct database (direct meaning by virtue of a connection string).
	/// </summary>
	public class KLinkDirect : KLink, IKLink
	{
		bool _DbCaseSensitiveNames = false;
		string _DbConnectionString = null;
		IDbConnection _DbConnection = null;
		KTransactionMode _TransactionMode = KTransactionMode.Scope;

		TransactionScope _TransactionScope = null;
		IDbTransaction _DbTransaction = null;
		bool _LinkOpenedByTransaction = false;
		int _TransactionLevel = 0;
		bool _TransactionAborted = false;

		/// <summary>
		/// Creates a new <see cref="KLinkDirect"/> instance initialized with the arguments given.
		/// </summary>
		/// <param name="factory">The factory defining the personality of the database.</param>
		/// <param name="connString">The connection string to be used to create connections when needed.</param>
		/// <param name="caseSensitiveNames">Whether the names of tables and columns in the database are to be consider case
		/// sensitive or not.</param>
		/// <param name="mode">The default transactional mode to use when creating automatic transactions.</param>
		public KLinkDirect( IKFactoryDirect factory, string connString,
			bool caseSensitiveNames = false, KTransactionMode mode = KTransactionMode.Scope )
			: base( factory )
		{
			DEBUG.IndentLine( "\n-- KLinkDirect( Factory, Connection={0}, CaseSensitive={1}, Mode={2} )", connString ?? "null", caseSensitiveNames, mode );

			_DbCaseSensitiveNames = caseSensitiveNames;
			_TransactionMode = mode;
			DbConnectionString = connString; // Through the property

			DEBUG.Unindent();
		}

		protected override void Dispose( bool disposing )
		{
			DEBUG.IndentLine( "\n-- KLinkDirect.Dispose( Disposing={0} ) - This={1}", disposing, this );
			base.Dispose( disposing ); // Calls-back DbClose()...
			DEBUG.Unindent();
		}

		/// <summary>
		/// Clones this link object, cloning also all the relevant elements it conceptually contains.
		/// <para>If any of those implements the <see cref="IKCloneable"/> interface, they are cloned using its Clone(link)
		/// method where the link instance is the new cloned one.</para>
		/// <para>Otherwise, they are cloned only if they implement the <see cref="ICloneable"/> interface and, if not, their
		/// original value/reference is used for the new cloned link.</para>
		/// </summary>
		/// <returns>A new cloned object.</returns>
		public KLinkDirect Clone()
		{
			var cloned = new KLinkDirect( Factory, _DbConnectionString, _DbCaseSensitiveNames, _TransactionMode );
			OnClone( cloned );
			return cloned;
		}
		object ICloneable.Clone()
		{
			return this.Clone();
		}

		protected string _Server = null; // Used to facilitate ToString()
		protected string _Catalog = null; // Used to facilitate ToString()
		public override string ToString()
		{
			return string.Format( "KLinkDirect[{0}.{1}, {2}]",
				_Server ?? "null", _Catalog ?? "null",
				Factory == null ? "null" : Factory.ToString() );
		}

		/// <summary>
		/// Gets the factory that describes the personality of the direct database to communicate with.
		/// </summary>
		public new IKFactoryDirect Factory
		{
			get { return (IKFactoryDirect)base.Factory; }
		}

		/// <summary>
		/// Gets or sets the connection string this link will use to create an automatic connection against the database
		/// when needed.
		/// <para>The setter will fail is there is an active connection maintained by this link instance.</para>
		/// </summary>
		public string DbConnectionString
		{
			get { return _DbConnectionString; }
			set
			{
				if( _DbConnection != null ) throw new InvalidOperationException( "A connection already exists." );
				var sb = Factory.DbProviderFactory.CreateConnectionStringBuilder();
				sb.ConnectionString = value;
				_DbConnectionString = sb.ConnectionString;

				_Server = sb.ContainsKey( "Data Source" ) ? (string)sb["Data Source"] : "<Server>";
				_Catalog = sb.ContainsKey( "Initial Catalog" ) ? (string)sb["Initial Catalog"] : "<Database>";
			}
		}

		/// <summary>
		/// Gets the connection in use by this link instance, or null.
		/// <para>If it is not null, it has been created automatically by this object, and it will be managed automatically
		/// by it as needed.</para>
		/// </summary>
		public IDbConnection DbConnection
		{
			get { return _DbConnection; }
		}

		/// <summary>
		/// Gets whether the names of the tables and columns in the database should be consider as case sensitive or not.
		/// </summary>
		public override bool DbCaseSensitiveNames
		{
			get { return _DbCaseSensitiveNames; }
		}

		/// <summary>
		/// Creates and opens manually the connection this link object maintains against the database.
		/// <para>If a connection already exists an exception will be thrown.</para>
		/// </summary>
		public override void DbOpen()
		{
			if( _DbConnection != null ) throw new InvalidOperationException( "A connection already exists." );
			_DbConnection = Factory.DbProviderFactory.CreateConnection();
			_DbConnection.ConnectionString = _DbConnectionString;
			_DbConnection.Open();
		}

		/// <summary>
		/// Closes and destroys the connection against the database this link object is maintaining.
		/// <para>If it does not exist it merely returns. Along the way, if a transaction exists, it will be aborted.</para>
		/// </summary>
		public override void DbClose()
		{
			if( _TransactionLevel >= 1 ) TransactionAbort();

			if( _DbConnection != null ) {
				if( _DbConnection.State != ConnectionState.Closed ) _DbConnection.Close();
				_DbConnection.Dispose();
				_DbConnection = null;
			}
		}

		/// <summary>
		/// Gets whether a connection exists and is in an opened state.
		/// </summary>
		public override bool IsDbOpened
		{
			get { return _DbConnection == null ? false : ( _DbConnection.State == ConnectionState.Open ? true : false ); }
		}

		/// <summary>
		/// Gets or sets the default transactional mode to use to create automatic transactions when such operation is
		/// requested. If a transaction already exists, an exception will be thrown.
		/// </summary>
		public override KTransactionMode TransactionMode
		{
			get { return _TransactionMode; }
			set
			{
				if( TransactionState == KTransactionState.Active ) throw new InvalidOperationException( "Cannot modify TransactionMode because a transaction is active." );
				_TransactionMode = value;
			}
		}

		/// <summary>
		/// Gets the transactional state of this link object.
		/// </summary>
		public override KTransactionState TransactionState
		{
			get
			{
				if( _TransactionAborted ) return KTransactionState.Aborted;
				if( _TransactionLevel >= 1 ) return KTransactionState.Active;
				return KTransactionState.Empty;
			}
		}

		/// <summary>
		/// Creates a transaction in this link object, using the transactional mode established for this link object, and
		/// accepting nested transactions. In this case, it merely maintains an  internal counter to keep track of how many
		/// start operations have been requested.
		/// </summary>
		public override void TransactionStart()
		{
			if( _TransactionLevel >= 1 ) _TransactionLevel++;
			else {
				if( _TransactionMode == KTransactionMode.Database ) {
					if( !IsDbOpened ) { DbOpen(); _LinkOpenedByTransaction = true; }
					_DbTransaction = DbConnection.BeginTransaction();
				}
				else {
					_TransactionScope = new TransactionScope();
					if( !IsDbOpened ) { DbOpen(); _LinkOpenedByTransaction = true; }
				}
				_TransactionLevel = 1;
				_TransactionAborted = false;
			}
		}

		/// <summary>
		/// Commits the transaction that might be active in this link object. If no transaction exists, it merely returns.
		/// It uses an internal counter to keep track of nested transactions, so that the actual commit operation will be
		/// executed only when this counter reaches cero.
		/// </summary>
		public override void TransactionCommit()
		{
			if( _TransactionLevel == 0 ) return;
			if( _TransactionLevel > 1 ) { _TransactionLevel--; return; }
			_TransactionLevel = 0; // To signal we are finalizing...

			if( _TransactionMode == KTransactionMode.Database ) {
				_DbTransaction.Commit(); _DbTransaction.Dispose(); _DbTransaction = null;
				if( _LinkOpenedByTransaction ) DbClose();
			}
			else {
				_TransactionScope.Complete(); _TransactionScope.Dispose(); _TransactionScope = null;
				if( _LinkOpenedByTransaction ) DbClose();
			}

			_TransactionAborted = false;
			_LinkOpenedByTransaction = false;
		}

		/// <summary>
		/// Aborts the current transaction of this link object if such exists. The internal counter is resetted to cero,
		/// and the internal state is set to Aborted in order to inform of this circumstance.
		/// </summary>
		public override void TransactionAbort()
		{
			if( _TransactionLevel == 0 ) return;
			_TransactionLevel = 0; // To signal we are finalizing...

			if( _TransactionMode == KTransactionMode.Database ) {
				_DbTransaction.Rollback(); _DbTransaction.Dispose(); _DbTransaction = null;
				if( _LinkOpenedByTransaction ) DbClose();
			}
			else {
				_TransactionScope.Dispose(); _TransactionScope = null;
				if( _LinkOpenedByTransaction ) DbClose();
			}

			_TransactionAborted = true; // Setting the flag for informative purposes
			_LinkOpenedByTransaction = false;
		}

		/// <summary>
		/// Creates a new <see cref="KEnumeratorDirect"/> instance associated with the given command.
		/// </summary>
		/// <param name="command">The command the enumerator will be associated with.</param>
		/// <returns>The new enumerator created.</returns>
		public KEnumeratorDirect CreateEnumerator( IKCommandEnumerable command )
		{
			return new KEnumeratorDirect( command );
		}
		IKEnumerator IKLink.CreateEnumerator( IKCommandEnumerable command )
		{
			return this.CreateEnumerator( command );
		}

		/// <summary>
		/// Creates a new <see cref="KExecutorDirect"/> instance associated with the given command.
		/// </summary>
		/// <param name="command">The command the executor will be associated with.</param>
		/// <returns>The new executor created.</returns>
		public KExecutorDirect CreateExecutor( IKCommandExecutable command )
		{
			return new KExecutorDirect( command );
		}
		IKExecutor IKLink.CreateExecutor( IKCommandExecutable command )
		{
			return this.CreateExecutor( command );
		}
	}
}
// ========================================================
