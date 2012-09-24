// ======================================================== #undef DEBUG
namespace Test.KeroseneORM.WCF.Client
{
	using global::System.Diagnostics;
	using global::System;
	using global::MB.Tools;
	using global::MB.KeroseneORM;
	using global::MB.KeroseneORM.WCF;
	using global::MB.KeroseneORM.SQLServer;
	using global::MB.Tools.Dynamics;
	using global::Test.KeroseneORM.Common;

	// =====================================================
	class Program
	{
		static void Main( string[] args )
		{
			TextWriterTraceListener tr = new TextWriterTraceListener( System.Console.Out );
			Debug.Listeners.Add( tr ); Debug.AutoFlush = true; Debug.IndentSize = 3;
			ConsoleHelper.SetWindowPosition( 1, 10 );

			Console.WriteLine( "\n===== Press [Enter] to start the program..." ); Console.ReadLine();

			Console.WriteLine( "\n===== Registering types for the WCF machinery ..." );
			KTypesWCF.AddType( typeof( CalendarDate ) );
			KTypesWCF.AddType( typeof( ClockTime ) );

			Console.WriteLine( "\n===== Creating the Link object ..." );
			dynamic package = new DeepObject(); // Or use null if you don't need it
			package.Login = "MyLogin";
			package.Password = "MyPassword";
			package.NowDate = new CalendarDate( DateTime.Now );
			package.NowTime = new ClockTime( DateTime.Now );
			var endpoint = "Service_OnTcp_Endpoint"; // Use the endpoint on you App.config file
			var factory = new KFactorySQL(); // Use the appropriate factory
			var link = new KLinkWCF( factory, endpoint, package );

			Console.WriteLine( "\n== Executing your examples ..." );
			Test_Core.Dispatcher( link );

			Console.WriteLine( "\n== Press [Enter] to dispose the link ..." ); Console.ReadLine();
			link.Dispose();
			
			Console.WriteLine( "\n===== Press [Enter] to terminate program..." ); Console.ReadLine();
		}
	}
}
// ========================================================
