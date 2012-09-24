// ========================================================
namespace MB.Tools.Data.SQLServer
{
	using global::System;
	using global::System.Data;
	using global::System.Data.SqlClient;

	// =====================================================
	/// <summary>
	/// Defines the valid network libraries types to use with Microsoft SQL Server databases.
	/// </summary>
	public enum SQLNetworkLibrary
	{
		TcpIP, NamedPipes, SharedMemory, MultiProtocol,
		AppleTalk, Via, IpxSpx
	}
	public static class SQLNetworkLibraryHelper
	{
		/// <summary>
		/// Returns a string identifying the network library, useful to be included in a connection string.
		/// </summary>
		/// <param name="lib">The network library.</param>
		/// <returns>A string identifying the network library, useful to be included in a connection string.</returns>
		public static string LibraryIdentifier( this SQLNetworkLibrary lib )
		{
			switch( lib ) {
				case SQLNetworkLibrary.TcpIP: return "dbmssocn";
				case SQLNetworkLibrary.NamedPipes: return "dbnmpntw";
				case SQLNetworkLibrary.SharedMemory: return "dbmslpcn";
				case SQLNetworkLibrary.MultiProtocol: return "dbmsrpcn";
				case SQLNetworkLibrary.AppleTalk: return "dbmsadsn";
				case SQLNetworkLibrary.Via: return "dbmsgnet";
				case SQLNetworkLibrary.IpxSpx: return "dbmsspxn";
			}
			throw new ArgumentException( "Invalid protocol id: " + lib );
		}
	}

	// =====================================================
	/// <summary>
	/// Utilities to be used for Microsoft SQL Server databases.
	/// </summary>
	public static class SQLHelper
	{
		/// <summary>
		/// Returns the SQL type whose name is given, or Variant if the name is invalid.
		/// </summary>
		/// <param name="dbTypeName">The SQL type name.</param>
		/// <param name="raise">True to raise an exception in case the name is invalid.</param>
		/// <returns>The SQL type associated with the name given, or Variant as the default value.</returns>
		public static SqlDbType ParseDbType( string dbTypeName, bool raise = true )
		{
			dbTypeName = dbTypeName.Validated( "SQL Type name" );

			switch( dbTypeName.ToLower() ) {
				case "bigint": return SqlDbType.BigInt;
				case "binary": return SqlDbType.Binary;
				case "bit": return SqlDbType.Bit;
				case "char": return SqlDbType.Char;
				case "date": return SqlDbType.Date;
				case "datetime": return SqlDbType.DateTime;
				case "datetime2": return SqlDbType.DateTime2;
				case "datetimeoffset": return SqlDbType.DateTimeOffset;
				case "numeric":
				case "decimal": return SqlDbType.Decimal;
				case "float": return SqlDbType.Float;
				case "image": return SqlDbType.Image;
				case "int": return SqlDbType.Int;
				case "money": return SqlDbType.Money;
				case "nchar": return SqlDbType.NChar;
				case "ntext": return SqlDbType.NText;
				case "nvarchar": return SqlDbType.NVarChar;
				case "real": return SqlDbType.Real;
				case "smalldatetime": return SqlDbType.SmallDateTime;
				case "smallint": return SqlDbType.SmallInt;
				case "smallmoney": return SqlDbType.SmallMoney;
				case "structured": return SqlDbType.Structured;
				case "text": return SqlDbType.Text;
				case "time": return SqlDbType.Time;
				case "timestamp": return SqlDbType.Timestamp;
				case "tinyint": return SqlDbType.TinyInt;
				case "udt": return SqlDbType.Udt;
				case "uniqueidentifier": return SqlDbType.UniqueIdentifier;
				case "varbinary": return SqlDbType.VarBinary;
				case "varchar": return SqlDbType.VarChar;
				case "sql_variant":
				case "variant": return SqlDbType.Variant;
				case "xml": return SqlDbType.Xml;
			}
			if( raise ) throw new ArgumentException( "SqlDbType not found: " + dbTypeName );
			return SqlDbType.Variant;
		}

		/// <summary>
		/// Builds a connection string for Microsoft SQL Server databases using the parameters given.
		/// </summary>
		/// <param name="serverName">The server's name or IP address.</param>
		/// <param name="databaseName">The database name.</param>
		/// <param name="user">If not null, the user to use for the connection.</param>
		/// <param name="password">If user is not null, the password to use to authenticate this user.</param>
		/// <param name="library">The SQL library to use.</param>
		/// <param name="persistSecurityInfo">True to persist security info.</param>
		/// <param name="multipleActiveResultSets">True to activate support for multiple simultaneous results sets.</param>
		/// <returns>The connection string built using the parameters given.</returns>
		public static string BuildConnectionString(
			string serverName, string databaseName,
			string user = null, string password = null,
			SQLNetworkLibrary library = SQLNetworkLibrary.TcpIP,
			bool persistSecurityInfo = false,
			bool multipleActiveResultSets = true )
		{
			serverName = serverName.Validated( "Server's name or IP address" );
			databaseName = databaseName.Validated( "Database's name" );

			// The basic is with integrated security...
			SqlConnectionStringBuilder sb = new SqlConnectionStringBuilder();
			sb.DataSource = serverName;
			sb.InitialCatalog = databaseName;
			sb.PersistSecurityInfo = persistSecurityInfo;
			sb.NetworkLibrary = library.LibraryIdentifier();

			// Using explicit (user / password) security...
			if( user != null ) {
				user = user.Validated( "User's login" );
				sb.IntegratedSecurity = false;
				sb.UserID = user;
				if( !string.IsNullOrEmpty( password ) ) sb.Password = password;
			}
			else sb.IntegratedSecurity = true; // Or using integrated security mode

			// Allowing multiple result sets if requested
			if( multipleActiveResultSets ) sb.MultipleActiveResultSets = true;

			// Returning...
			return sb.ToString();
		}
	}
}
// ========================================================
