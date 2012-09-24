// ======================================================== #undef DEBUG
namespace Test.KeroseneORM.Direct
{
	using global::System.Diagnostics;
	using global::System;
	using global::MB.Tools;
	using global::MB.Tools.Data.SQLServer;
	using global::MB.KeroseneORM;
	using global::MB.KeroseneORM.Direct;
	using global::MB.KeroseneORM.SQLServer.Direct;
	using global::Test.KeroseneORM.Common;

	// =====================================================
	class Program
	{
		static void Main( string[] args )
		{
			TextWriterTraceListener tr = new TextWriterTraceListener( System.Console.Out );
			Debug.Listeners.Add( tr ); Debug.AutoFlush = true; Debug.IndentSize = 3;
			ConsoleHelper.SetWindowPosition( 0, 80 );

			Console.WriteLine( "\n===== Press [Enter] to start the tests..." ); Console.ReadLine();
			Test_Direct_SQLServer();
			Console.WriteLine( "\n===== Press [Enter] to terminate program..." ); Console.ReadLine();
		}
		static void Test_Direct_SQLServer()
		{
			Console.WriteLine( "\n===== Creating the link and registering the transformers ..." );
			var dbServer = "Win2008Dev"; // Use your specific server
			var dbCatalog = "KeroseneDB"; // Use your specific database
			var multiple = true; // To support nested readers
			var connstr = SQLHelper.BuildConnectionString( dbServer, dbCatalog, multipleActiveResultSets: multiple );

			var link = new KLinkDirectSQL( connstr );
			link.AddParameterTransformer<CalendarDate>( x => CalendarDate.ToDateTime( x ) );
			link.AddParameterTransformer<ClockTime>( x => x.ToString() );

			Console.WriteLine( "\n===== Executing the examples..." );
			Test_EasyMaps.Dispatcher( link );

			Console.Write( "\n===== Press [Enter] to dispose the link..." ); Console.ReadLine();
			link.Dispose();
		}
	}
}
// ========================================================
