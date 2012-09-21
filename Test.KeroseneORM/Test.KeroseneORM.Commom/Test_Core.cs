// ======================================================== #undef DEBUG
namespace Test.KeroseneORM.Common
{
	using global::System.Diagnostics;
	using global::System;
	using global::System.Text;
	using global::System.Collections.Generic;
	using global::MB.Tools;
	using global::MB.KeroseneORM;

	// =====================================================
	public class Test_Core
	{
		// Use this method to specify the test(s) to run...
		public static void Dispatcher( IKLink link )
		{
			Nested_Readers( link );
		}

		public static void Raw_Query( IKLink link )
		{
			Console.WriteLine( "\n===== Raw Query..." );

			var cmd = link.Raw( "SELECT * FROM Employees WHERE BirthDate >= {0}", new CalendarDate( 1969, 1, 1 ) );
			Console.WriteLine( "\n> Command => {0}", cmd );

			foreach( var obj in cmd ) Console.WriteLine( "\n> Record => {0}", obj );
			cmd.Dispose();
		}
		public static void Raw_Query_MultipleTables( IKLink link )
		{
			Console.WriteLine( "\n===== Raw Query with Multiple Tables..." );
			var cmd = link.Raw(
				"SELECT * FROM Employees AS Emp, Countries as Ctry WHERE Emp.BirthDate >= {0} AND Emp.CountryId = Ctry.Id",
				new CalendarDate( 1969, 1, 1 ) );
			Console.WriteLine( "\n> Command => {0}", cmd );

			foreach( var obj in cmd ) Console.WriteLine( "\n> Record => {0}", obj );
			cmd.Dispose();
		}
		public static void Raw_Insert_JamesBond_Enumerate( IKLink link )
		{
			Console.WriteLine( "\n===== Raw Insert James Bond with Enumerate..." );

			// Note that to enumerate in a non-SELECT Raw command, we need to explicitly include "OUTPUT INSERTED.*"...
			var cmd = link.Raw(
				"INSERT INTO Employees ( Id, FirstName, LastName, CountryId, BirthDate, JoinDate, Photo )"
				+ " OUTPUT INSERTED.*"
				+ " VALUES ( {0}, {1}, {2}, {3}, {4}, {5}, {6} )",
				"007", "James", "Bond", "uk", new CalendarDate( 1969, 1, 1 ), null, new byte[] { 0, 0, 7 } );
			Console.WriteLine( "\n> Command => {0}", cmd );

			foreach( var obj in cmd ) Console.WriteLine( "\n> Record => {0}", obj );
			cmd.Dispose();
		}
		public static void Raw_Delete_JamesBond_NonEnumerate( IKLink link )
		{
			Console.WriteLine( "\n===== Raw Delete James Bond but with no Enumeration..." );

			var cmd = link.Raw( "DELETE FROM Employees WHERE Id = {0}", "007" );
			Console.WriteLine( "\n> Command => {0}", cmd );

			int n = cmd.Execute();
			Console.WriteLine( "\n> Result => {0}", n );

			cmd.Dispose();
		}
		public static void Raw_StoredProcedure( IKLink link )
		{
			Console.WriteLine( "\n===== Invoking a Stored Procedure through a Raw Command..." );

			// Inserting an employee through a stored procedure...
			var cmd = link.Raw(
				"EXEC employee_insert @FirstName = {0}, @LastName = {1}",
				"James", "Bond" );
			Console.WriteLine( "\n> Command => {0}", cmd );

			object[] list = cmd.ToArray();
			foreach( var obj in list ) Console.WriteLine( "\n> Record => {0}", obj );
			cmd.Dispose();

			// Deleting to leave everything as it was...
			foreach( var obj in list ) {
				dynamic d = obj;
				string id = d.Id; // We are implicitly using the default table...

				var delete = link.Raw( "DELETE FROM Employees WHERE Id = {0}", id );
				Console.WriteLine( "\n> Command => {0}", delete );

				int n = delete.Execute();
				Console.WriteLine( "\n> Result => {0}", n );

				delete.Dispose();
			}
		}

		public static void WithTransaction( IKLink link, params Action<IKLink>[] actions )
		{
			try {
				Console.WriteLine( "\n===== Transaction started..." );
				link.TransactionStart();
				foreach( var action in actions ) action( link );
				link.TransactionCommit();
				Console.WriteLine( "\n===== Transaction committed..." );
			}
			catch { link.TransactionAbort(); throw; }
		}
		public static void WithNestedTransactions( IKLink link, params Action<IKLink>[] actions )
		{
			List<Action<IKLink>> list = actions != null ? new List<Action<IKLink>>( actions ) : null;
			if( list != null ) _NestedListTransaction( link, list );
		}
		static void _NestedListTransaction( IKLink link, List<Action<IKLink>> actions )
		{
			Action<IKLink> action = actions.Count == 0 ? null : actions[0]; if( action == null ) return;
			try {
				Console.WriteLine( "\n===== Transaction started..." );
				link.TransactionStart();
				action( link );

				actions.Remove( action );
				_NestedListTransaction( link, actions );
				link.TransactionCommit();
				Console.WriteLine( "\n===== Transaction committed..." );
			}
			catch { link.TransactionAbort(); throw; }
		}

		public static void Query_Basic( IKLink link )
		{
			Console.WriteLine( "\n===== Basic Query..." );

			var cmd = link.From( x => x.Employees ).Where( x => x.LastName >= "C" );
			Console.WriteLine( "\n> Command => {0}", cmd );

			foreach( var obj in cmd ) Console.WriteLine( "\n> Record => {0}", obj );
			cmd.Dispose();
		}
		public static void Query_Basic_WithDynamicColumns( IKLink link )
		{
			Console.WriteLine( "\n===== Basic Query with Dynamic Columns..." );

			var cmd = link.From( x => x.Employees.As( x.Emp ) ).Where( x => x.LastName >= "C" );
			Console.WriteLine( "\n> Command => {0}", cmd );

			foreach( dynamic obj in cmd )
				Console.WriteLine( "\n> Generated => {0}: {1} {2}",
					obj.Employees.Id, // Using the table name
					obj.Emp.FirstName, // Using the table alias
					obj.LastName ); // We are using the Employees table implicitly as there is only one LastName column

			cmd.Dispose();
		}
		public static void Query_Basic_WithIndexedColumns( IKLink link )
		{
			Console.WriteLine( "\n===== Basic Query with Indexed columns..." );

			var cmd = link.From( x => x.Employees.As( x.Emp ) ).Where( x => x.LastName >= "C" );
			Console.WriteLine( "\n> Command => {0}", cmd );

			foreach( KRecord obj in cmd )
				Console.WriteLine( "\n> Generated => {0}: {1} {2}",
					obj["Id"],
					obj["Emp", "FirstName"],
					obj["Employees", "LastName"] );

			cmd.Dispose();
		}

		public class EmployeeTable
		{
			public string FullName
			{
				get
				{
					if( LastName == null && FirstName == null ) return null;

					StringBuilder sb = new StringBuilder();
					if( LastName != null ) sb.Append( LastName );
					if( LastName != null && FirstName != null ) sb.Append( ", " );
					if( FirstName != null ) sb.Append( FirstName );
					return sb.ToString();
				}
			}

			public string Id { get; set; }
			public string FirstName { get; set; }
			public string LastName { get; set; }
			public CalendarDate BirthDate { get; set; }
			public bool? Active { get; set; }
			public string ManagerId { get; set; }
			public string CountryId { get; set; }
			public CalendarDate JoinDate { get; set; }
			public ClockTime StartTime { get; set; }
			public byte[] Photo { get; set; }

			public EmployeeTable() { Clear(); }
			public void Clear()
			{
				Id = null;
				FirstName = LastName = null;
				BirthDate = null;
				Active = null;
				ManagerId = null; CountryId = null;
				JoinDate = null;
				StartTime = null;
				Photo = null;
			}
			public override string ToString()
			{
				StringBuilder sb = new StringBuilder();
				sb.AppendFormat( "Id:'{0}' ", Id );
				if( FullName != null ) sb.AppendFormat( "Name:'{0}' ", FullName );
				if( BirthDate != null ) sb.AppendFormat( "BirthDate:'{0}' ", BirthDate );
				if( Active != null ) sb.AppendFormat( "Active:'{0}' ", Active );
				if( ManagerId != null ) sb.AppendFormat( "ManagerId:'{0}' ", ManagerId );
				if( CountryId != null ) sb.AppendFormat( "CountryId:'{0}' ", CountryId );
				if( JoinDate != null ) sb.AppendFormat( "JoinDate:'{0}' ", JoinDate );
				if( StartTime != null ) sb.AppendFormat( "StartTime:'{0}' ", StartTime );
				if( Photo != null ) sb.AppendFormat( "Photo:'{0}' ", TypeHelper.ToString( Photo ) );
				return sb.ToString();
			}
		}
		public class CountryTable
		{
			public string Id { get; set; }
			public string Name { get; set; }
			public string RegionId { get; set; }
			public List<EmployeeTable> Employees = new List<EmployeeTable>();

			public CountryTable() { Clear(); }
			public void Clear()
			{
				Id = null;
				Name = null;
				RegionId = null;
				Employees.Clear();
			}
			public override string ToString()
			{
				StringBuilder sb = new StringBuilder();
				sb.AppendFormat( "Id:'{0}' ", Id );
				if( Name != null ) sb.AppendFormat( "Name:'{0}' ", Name );
				if( RegionId != null ) sb.AppendFormat( "RegionId:'{0}' ", RegionId );
				if( Employees != null ) {
					sb.Append( "{" ); bool first = true; foreach( var emp in Employees ) {
						if( !first ) sb.Append( ", " ); else { sb.Append( " " ); first = false; }
						sb.Append( emp.Id );
					}
					if( !first ) sb.Append( " " ); else sb.Append( "-" );
					sb.Append( "}" );
				}
				return sb.ToString();
			}
		}

		public static void Query_Basic_ConversionToAnonymous( IKLink link )
		{
			Console.WriteLine( "\n===== Basic Query with conversion to anonymous..." );

			var cmd = link.From( x => x.Employees.As( x.Emp ) ).Where( x => x.LastName >= "C" );
			Console.WriteLine( "\n> Command => {0}", cmd );

			foreach( var obj in cmd.ConvertBy( rec => {
				// Using several ways of accessing the columns...
				dynamic d = rec;
				return new {
					d.Id,
					Name = string.Format( "{0}, {1}", rec["LastName"], rec["Emp", "FirstName"] )
				};
			} ) ) Console.WriteLine( "\n> Converted => {0}", obj );

			cmd.Dispose();
		}
		public static void Query_Basic_ConversionToType( IKLink link )
		{
			Console.WriteLine( "\n===== Basic Query with conversion to a given type..." );

			var cmd = link.From( x => x.Employees.As( x.Emp ) );
			Console.WriteLine( "\n> Command => {0}", cmd );

			foreach( EmployeeTable obj in cmd.ConvertBy( rec => {
				// Using several ways of accessing the columns...
				EmployeeTable emp = new EmployeeTable(); dynamic d = rec;
				emp.Id = d.Id;
				emp.FirstName = (string)rec["FirstName"];
				emp.LastName = (string)rec["Emp", "LastName"];

				// Using the hard way, the indexed one, we need to take care of the type conversions...
				// object orig = rec["Employees", "BirthDate"];
				// CalendarDate date = orig == null ? null : new CalendarDate( (DateTime)orig );
				// emp.BirthDate = date;

				emp.BirthDate = d.BirthDate; // If using dynamics no conversion specification is needed
				emp.JoinDate = d.JoinDate;
				emp.StartTime = d.StartTime;

				emp.Active = d.Active;
				emp.ManagerId = d.Emp.ManagerId; // Using the alias
				emp.CountryId = d.Employees.CountryId; // Using the table name
				emp.Photo = d.Photo;
				return emp;
			} ) ) Console.WriteLine( "\n> Employee => {0}", obj );

			cmd.Dispose();
		}

		public static void Query_GettingOneValue( IKLink link )
		{
			Console.WriteLine( "\n===== Query getting one value instead of a record..." );

			// As anonymous columns are not supported, we need to give a name to it
			// The easiest way is by using an alias for the object returned by the database

			var cmd = link
				.From( x => x.Employees )
				.Select( x => x.Count( x.Id ).As( x.SumOfEmployees ) );
			Console.WriteLine( "\n> Command => {0}", cmd );

			dynamic obj = cmd.First();
			Console.WriteLine( "\n> Using the record => {0}", obj );
			Console.WriteLine( "\n> Using the property => {0}", obj.SumOfEmployees );

			cmd.Dispose();
		}

		public static void Query_MultipleTables_AndSkipTake( IKLink link )
		{
			Console.WriteLine( "\n===== Query multiples tables..." );

			var cmd = link
				.From( x => x.Employees.As( x.Emp ) ).Where( x => x.Emp.JoinDate >= new CalendarDate( 2000, 1, 1 ) )
				.From( x => x.Countries.As( x.Ctry ) ).Where( x => x.Ctry.Id == x.Emp.CountryId )
				.Select( x => x.Ctry.All() )
				.Select( x => x.Emp.Id, x => x.Emp.BirthDate, x => x.Emp.LastName );
			Console.WriteLine( "\n> Command => {0}", cmd );

			foreach( var obj in cmd ) Console.WriteLine( "\n> Record => {0}", obj );

			Console.WriteLine( "\n===== Query multiples tables with skip/take..." );

			foreach( var obj in cmd.SkipTake( 2, 2 ) )
				Console.WriteLine( "\n> Record => {0}", obj );

			cmd.Dispose();
		}
		public static void Query_WithSourceFroms( IKLink link )
		{
			Console.WriteLine( "\n===== Query using a command as the From source..." );

			var cmd = link
				.From(
					link.From( x => x.Countries.As( x.Ctry ) ).Where( x => x.Ctry.Id == "us" ),
					x => x.Location )
				.From( x => x.Employees.As( x.Emp ) ).Where( x => x.Emp.CountryId == x.Location.Id )
				.Select( x => x.Emp.All() );
			Console.WriteLine( "\n> Command => {0}", cmd );

			foreach( var obj in cmd ) Console.WriteLine( "\n> Record => {0}", obj );
			cmd.Dispose();
		}
		public static void Query_With_In_and_Equal( IKLink link )
		{
			Console.WriteLine( "\n===== Query using the IN and EQUAL syntax..." );

			var cmd = link
				.From( x => x.Employees ).Where( x => !x.CountryId.In(
					link.From( y => y.Countries ).Select( y => y.Id ).Where( y => y.RegionId.In(
						link.From( z => z.Regions ).Select( z => z.Id ).Where( z => z.ParentId =
							link.From( p => p.Regions ).Select( p => p.Id )
							.Where( p => p.Name == "Europe, Middle East & Africa" )
				) ) ) ) );
			Console.WriteLine( "\n> Command => {0}", cmd );

			foreach( var obj in cmd ) Console.WriteLine( "\n> Record => {0}", obj );
			cmd.Dispose();
		}
		public static void Query_WithJoins( IKLink link )
		{
			Console.WriteLine( "\n===== Query using Joins..." );

			var cmd = link
				.From( x => x.Employees.As( x.Emp ) )
				.Join( x => x.Countries.As( x.Ctry ).On( x.Ctry.Id == x.Emp.CountryId ) )
				.Join( x => x.Regions.As( x.Reg ).On( x.Reg.Id == x.Ctry.RegionId ) )
				.Join( x => x.Regions.As( x.Super ).On( x.Super.Id == x.Reg.ParentId ) )
				.Where( x => x.Super.Name == "Europe, Middle East & Africa" )
				.Select( x => x.Emp.All() )
				.Select( x => x.Reg.All() )
				.OrderBy( x => x.Reg.Id ).OrderBy( x => x.Emp.Id );
			Console.WriteLine( "\n> Command => {0}", cmd );

			foreach( var obj in cmd ) Console.WriteLine( "\n> Record => {0}", obj );
			cmd.Dispose();
		}
		public static void Query_WithChainedFroms( IKLink link )
		{
			Console.WriteLine( "\n===== Query using a chained From syntax..." );

			var cmd = link
				.From( x => x.Regions.As( x.Super ) ).Where( x => x.Super.Name == "Europe, Middle East & Africa" )
				.From( x => x.Regions.As( x.Reg ) ).Where( x => x.Reg.ParentId == x.Super.Id )
				.From( x => x.Countries.As( x.Ctry ) ).Where( x => x.Ctry.RegionId == x.Reg.Id )
				.From( x => x.Employees.As( x.Emp ) ).Where( x => x.Emp.CountryId == x.Ctry.Id )
				.Select( x => x.Emp.All() )
				.Select( x => x.Reg.All() )
				.OrderBy( x => x.Reg.Id ).OrderBy( x => x.Emp.Id );
			Console.WriteLine( "\n> Command => {0}", cmd );

			foreach( var obj in cmd ) Console.WriteLine( "\n> Record => {0}", obj );
			cmd.Dispose();
		}

		public static void Fake_Query_Extended_Syntax( IKLink link )
		{
			Console.WriteLine( "\n== A fake query to show the extended syntax usage..." );

			var cmd = link
				.From( "Employees AS Emp" )
				.GroupBy( x => x.Cube( x.C1, "C2 with Embedded Spaces" ) )
				.OrderBy( x => x( x.C3, "Text without meaning", x.C4 >= 9 ) )
				.Select( "C5 as C6" );

			Console.WriteLine( "\n> Command => {0}", cmd );
			Console.WriteLine( "\n> NOT EXECUTED AS IT HAS NO MEANING ..." );
		}

		public static void Update_Enumerate( IKLink link )
		{
			Console.WriteLine( "\n===== Update enumerating the results..." );

			var cmd = link.Update( x => x.Employees )
				.Where( x => x.FirstName >= "E" )
				.Column(
					x => x.ManagerId = null,
					x => x.LastName = x.LastName + "_1",
					x => x.Photo = new byte[] { 99, 98, 97, 96 }
				);
			Console.WriteLine( "\n> Command => {0}", cmd );

			foreach( var obj in cmd ) Console.WriteLine( "\n> Record => {0}", obj );
			cmd.Dispose();
		}
		public static void Insert_JamesBond_Enumerate( IKLink link )
		{
			Console.WriteLine( "\n===== Insert James Bond enumerating the results..." );

			var cmd = link.Insert( x => x.Employees ).Column(
					x => x.Id = "007",
					x => x.FirstName = "James",
					x => x.LastName = "Bond",
					x => x.CountryId = "uk",
					x => x.JoinDate = null,
					x => x.Photo = new byte[] { 0, 0, 7 }
				);
			Console.WriteLine( "\n> Command => {0}", cmd );

			foreach( var obj in cmd ) Console.WriteLine( "\n> Record => {0}", obj );
			cmd.Dispose();
		}
		public static void Delete_JamesBond_Enumerate( IKLink link )
		{
			Console.WriteLine( "\n===== Delete James Bond enumerating the results..." );

			var cmd = link.Delete( x => x.Employees )
				.Where( x => x.Id == "007" );
			Console.WriteLine( "\n> Command => {0}", cmd );

			foreach( var obj in cmd ) Console.WriteLine( "\n> Record => {0}", obj );
			cmd.Dispose();
		}

		public static void Delete_ManagerNotNull_Enumerate( IKLink link )
		{
			Console.WriteLine( "\n===== Delete the employees whose manager is NOT null enumerating the results..." );

			var cmd = link.Delete( x => x.Employees )
				.Where( x => x.ManagerId != null );
			Console.WriteLine( "\n> Command => {0}", cmd );

			foreach( var obj in cmd ) Console.WriteLine( "\n> Record => {0}", obj );
			cmd.Dispose();
		}
		public static void Delete_ManagerNotNull_NonQuery( IKLink link )
		{
			Console.WriteLine( "\n===== Delete the employees whose manager is NOT just returning the number..." );

			var cmd = link.Delete( x => x.Employees )
				.Where( x => x.ManagerId != null );
			Console.WriteLine( "\n> Command => {0}", cmd );

			int n = cmd.Execute();
			Console.WriteLine( "\n> Result => {0}", n );

			cmd.Dispose();
		}

		public static void Nested_Readers( IKLink link )
		{
			Console.WriteLine( "\n===== Nested readers..." );

			var cmd = link.From( x => x.Countries ).OrderBy( x => x.Name );
			Console.WriteLine( "\n> Command => {0}", cmd );

			foreach( var ctry in cmd.ConvertBy( rec => {
				CountryTable table = new CountryTable(); dynamic d = rec;
				table.Id = d.Id;
				table.Name = d.Name;
				table.RegionId = d.RegionId;

				_Nested_Readers_Employees( link, table );
				return table;
			} ) ) Console.WriteLine( "\n> Country => {0}", ctry );
			cmd.Dispose();
		}
		static void _Nested_Readers_Employees( IKLink link, CountryTable ctry )
		{
			var cmd = link.From( x => x.Employees ).Where( x => x.CountryId == ctry.Id );
			DEBUG.IndentLine( "\n> Command => {0}", cmd );

			foreach( var emp in cmd.ConvertBy( rec => {
				EmployeeTable table = new EmployeeTable(); dynamic d = rec;
				table.Id = d.Id;
				table.FirstName = d.FirstName;
				table.LastName = d.LastName;
				table.CountryId = d.CountryId;

				ctry.Employees.Add( table );
				return table;
			} ) ) DEBUG.WriteLine( "\n> Employee => {0}", emp );
			cmd.Dispose();

			DEBUG.Unindent();
		}

		public static void Nested_UpdateReaders( IKLink link )
		{
			Console.WriteLine( "\n===== Nested Updates..." );

			KCommandRaw raw = link.Raw();
			Console.WriteLine( "\n== SUSPENDING THE CONSTRAINTS..." );
			raw.Set( "ALTER TABLE Countries NOCHECK CONSTRAINT ALL" ); raw.Execute();
			raw.Set( "ALTER TABLE Employees NOCHECK CONSTRAINT ALL" ); raw.Execute();

			var cmd = link.Update( x => x.Countries )
				.Where( x => x.Id == "es" )
				.Column( x => x.Id = "es#" );
			Console.WriteLine( "\n> Command => {0}", cmd );

			foreach( var ctry in cmd.ConvertBy( rec => {
				CountryTable table = new CountryTable(); dynamic d = rec;
				table.Id = d.Id;
				table.Name = d.Name;
				table.RegionId = d.RegionId;

				_Nested_UpdateReaders_Employees( link );
				return table;
			} ) ) Console.WriteLine( "\n> Country => {0}", ctry );
			cmd.Dispose();

			Console.WriteLine( "\n== REACTIVATING THE CONSTRAINTS..." );
			raw.Set( "ALTER TABLE Countries CHECK CONSTRAINT ALL" ); raw.Execute();
			raw.Set( "ALTER TABLE Employees CHECK CONSTRAINT ALL" ); raw.Execute();
		}
		static void _Nested_UpdateReaders_Employees( IKLink link )
		{
			var cmd = link.Update( x => x.Employees )
				.Where( x => x.CountryId == "es" )
				.Column( x => x.CountryId = "es#" );
			DEBUG.IndentLine( "\n> Command => {0}", cmd );

			foreach( var emp in cmd.ConvertBy( rec => {
				EmployeeTable table = new EmployeeTable(); dynamic d = rec;
				table.Id = d.Id;
				table.FirstName = d.FirstName;
				table.LastName = d.LastName;
				table.CountryId = d.CountryId;

				return table;
			} ) ) DEBUG.WriteLine( "\n> Employee => {0}", emp );

			cmd.Dispose();
			DEBUG.Unindent();
		}
	}
}
// ========================================================
