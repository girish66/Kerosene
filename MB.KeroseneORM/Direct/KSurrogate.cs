// ========================================================
#undef DEBUG
namespace MB.KeroseneORM.Direct
{
	using global::System.Diagnostics;
	using global::System;
	using global::System.Data;
	using global::System.Transactions;
	using global::MB.Tools;

	// =====================================================
	internal class KSurrogate : IDisposable
	{
		IKCommand _Command = null;
		KLinkDirect _Link = null;
		bool _LinkOpenedBySurrogate = false;

		internal int _NonQueryResult = -1;
		internal IDataReader _DataReader = null;
		internal KSchema _Schema = null;

		void Invoke( bool iterable )
		{
			// Opening if needed...
			if( !_Link.IsDbOpened ) { _Link.DbOpen(); _LinkOpenedBySurrogate = true; }

			// Creating the database command and its parameters...
			IDbCommand cmd = _Link.DbConnection.CreateCommand();
			cmd.CommandText = _Command.CommandText( iterable );

			string nullStr = _Command.Parser.Parse( null, nulls: true ); // To substitute parameters with NULL values...
			foreach( var par in _Command.Parameters ) {
				if( par.Value == null ) cmd.CommandText = cmd.CommandText.Replace( par.Name, nullStr );
				else {
					IDataParameter dbpar = cmd.CreateParameter();
					dbpar.ParameterName = par.Name;
					dbpar.Value = _Link.TransformParameterValue( par.Value );
					cmd.Parameters.Add( dbpar );
				}
			}

			// Setting the transaction...
			Transaction scope = Transaction.Current; if( scope != null ) { } // Managed by TransactionScope
			else {
				IDbTransaction tx = null; // Using the current connection's transaction, if exists...

				var innerConnection = TypeHelper.GetElementValue( _Link.DbConnection, "InnerConnection" );
				if( innerConnection != null ) {
					var currentTransaction = TypeHelper.GetElementValue( innerConnection, "CurrentTransaction" );
					if( currentTransaction != null ) {
						var parent = TypeHelper.GetElementValue( currentTransaction, "Parent" );
						if( parent != null ) tx = (IDbTransaction)parent;
					}
				}
				if( tx != null ) cmd.Transaction = tx;
			}

			// Execute if NON-QUERY...
			if( !iterable ) {
				try { _NonQueryResult = cmd.ExecuteNonQuery(); }
				catch { throw; }
				finally { cmd.Dispose(); cmd = null; }
				return;
			}

			// OTHERWISE, we need to capture the datareader and the schema returned from the database...
			try { _DataReader = cmd.ExecuteReader( CommandBehavior.KeyInfo ); }
			catch { throw; }
			finally { cmd.Dispose(); cmd = null; }

			// Creating the schema...
			DataTable table = _DataReader.GetSchemaTable();
			if( table == null ) throw new InvalidOperationException( "Cannot obtain schema table for command: " + cmd );

			string tablename = _Command is IKTableNameProvider ? ( (IKTableNameProvider)_Command ).TableName : null;

			_Schema = new KSchema( _Link.DbCaseSensitiveNames ); for( int i = 0; i < table.Rows.Count; i++ ) {
				DataRow row = table.Rows[i];
				string meta = null;
				object value = null;

				bool hidden = false; if( table.Columns.Contains( "IsHidden" ) ) {
					value = row[table.Columns["IsHidden"]];
					if( !( value is DBNull ) ) hidden = (bool)value;
				}
				if( hidden ) continue;

				KMetaColumn column = new KMetaColumn(); for( int j = 0; j < table.Columns.Count; j++ ) {
					meta = table.Columns[j].ColumnName;
					value = row[j] is DBNull ? null : row[j];
					if( value != null ) column[meta] = value;
				}
				if( column.BaseTableName == null && tablename != null ) column.BaseTableName = tablename;
				DEBUG.WriteLine( "-- Schema Column: {0}", column );
				_Schema.Add( column );
			}
			table.Dispose(); table = null;

			// Setting the aliases if needed...
			if( _Command is IKTableAliasListProvider )
				_Schema.TableAliasList.AddRange( ( (IKTableAliasListProvider)_Command ).TableAliasList );

			// And finishing with the schema...
			_Schema.Sealed = true;
		}

		internal KSurrogate( IKCommand command, bool iterable )
		{
			DEBUG.IndentLine( "\n-- KSurrogate( Iterable={0}, Command={1} )", iterable, command == null ? "null" : command.ToString() );

			if( ( _Command = command ) == null ) throw new ArgumentNullException( "command", "Command cannot be null." );
			if( _Command.Link == null ) throw new ArgumentException( "Command's link is null." );
			_Link = _Command.Link as KLinkDirect; if( _Link == null ) throw new ArgumentException( "Command's link is not a direct link." );

			Invoke( iterable );

			DEBUG.Unindent();
		}

		protected virtual void Dispose( bool disposing )
		{
			DEBUG.IndentLine( "\n-- KSurrogate.Dispose( Disposing={0} ) - This={1}", disposing, this );

			if( _DataReader != null ) {
				if( !_DataReader.IsClosed ) _DataReader.Close();
				_DataReader.Dispose();
				_DataReader = null;
			}
			if( _Link != null ) {
				if( _LinkOpenedBySurrogate ) _Link.DbClose();
				_Link = null;
			}

			_Schema = null; // It might be in use elsewhere
			_Command = null;

			DEBUG.Unindent();
		}
		public void Dispose()
		{
			DEBUG.IndentLine( "\n-- KSurrogate.Dispose() - This={0}", this );
			Dispose( true );
			GC.SuppressFinalize( this );
			DEBUG.Unindent();
		}
		~KSurrogate()
		{
			DEBUG.IndentLine( "\n-- KSurrogate~() - This={0}", this );
			Dispose( false );
			DEBUG.Unindent();
		}

		public override string ToString()
		{
			return string.Format( "KSurrogate[ {0} ]", _Command == null ? "null" : _Command.ToString() );
		}
	}
}
// ========================================================
