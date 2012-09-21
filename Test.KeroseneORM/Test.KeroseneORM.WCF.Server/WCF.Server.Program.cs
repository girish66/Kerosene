// ======================================================== #undef DEBUG
namespace Test.KeroseneORM.WCF.Server
{
	using global::System.Diagnostics;
	using global::System;
	using global::System.ServiceModel;

	using global::MB.Tools;
	using global::MB.KeroseneORM;
	using global::MB.KeroseneORM.WCF;
	using global::MB.KeroseneORM.WCF.Server;
	using global::MB.Tools.Data.SQLServer;
	using global::MB.KeroseneORM.SQLServer.Direct;
	using global::MB.Tools.Dynamics;

	// =====================================================
	public class MyServer : KServerWCF
	{
		public MyServer() : base()
		{
			DEBUG.IndentLine( "\n-- MyServer()" );
			DEBUG.Unindent();
		}
		protected override void Dispose( bool disposing )
		{
			DEBUG.IndentLine( "\n-- MyServer.Dispose( Disposing={0} ) This={1}", disposing, this );
			base.Dispose( disposing );
			DEBUG.Unindent();
		}

		public override IKLink CreateLink()
		{
			DEBUG.IndentLine( "\n-- MyServer.CreateLink()" );
			DEBUG.WriteLine( "\n-- Connection Package = {0}", Package == null ? "<null>" : Package.ToString() );

			var dbServer = "Win2008Dev"; // Use your specific server
			var dbCatalog = "KeroseneDB"; // Use your specific database
			var multiple = true; // To support nested readers
			var connstr = SQLHelper.BuildConnectionString( dbServer, dbCatalog, multipleActiveResultSets: multiple );

			var link = new KLinkDirectSQL( connstr );
			link.AddParameterTransformer<CalendarDate>( x => CalendarDate.ToDateTime( x ) );
			link.AddParameterTransformer<ClockTime>( x => x.ToString() );

			DEBUG.Unindent();
			return link;
		}
	}

	// =====================================================
	class Program
	{
		static void Main( string[] args )
		{
			TextWriterTraceListener tr = new TextWriterTraceListener( System.Console.Out );
			Debug.Listeners.Add( tr ); Debug.AutoFlush = true; Debug.IndentSize = 3;
			ConsoleHelper.SetWindowPosition( 1, 32 * 12 + 30 );

			Console.WriteLine( "\n===== Press [Enter] to start the tests..." ); Console.ReadLine();

			Console.WriteLine( "\n===== Registering types for the WCF machinery ..." );
			KTypesWCF.AddType( typeof( CalendarDate ) );
			KTypesWCF.AddType( typeof( ClockTime ) );

			Console.WriteLine( "\n===== Starting the service host ..." );
			ServiceHost host = new ServiceHost( typeof( MyServer ) );
			host.Open();

			Console.WriteLine( "\n===== Press [Enter] to finish the service host ..." ); Console.ReadLine();
			try { host.Close( new TimeSpan( 0, 0, 5 ) ); }
			catch { host.Abort(); throw; }

			Console.WriteLine( "\n===== Press [Enter] to terminate program..." ); Console.ReadLine();
		}
	}
}
// ========================================================
