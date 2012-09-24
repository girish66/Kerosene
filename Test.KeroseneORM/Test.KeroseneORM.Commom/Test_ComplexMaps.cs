// ========================================================
#undef DEBUG
namespace Test.KeroseneORM.Common
{
	using global::System.Diagnostics;
	using global::System;
	using global::System.Text;
	using global::System.Collections.Generic;
	using global::MB.Tools;
	using global::MB.KeroseneORM;
	using global::MB.KeroseneORM.Maps;

	// =====================================================
	public class Test_ComplexMaps
	{
		public class Region
		{
			public string Id { get; set; }
			public string Name { get; set; }
			public Region Parent { get; set; }
			public List<Region> ChildRegions { get; private set; }
			public List<Country> Countries { get; private set; }

			public Region()
			{
				ChildRegions = new List<Region>();
				Countries = new List<Country>();
				Clear();
			}
			public void Clear()
			{
				Id = null;
				Name = null;
				Parent = null;
				if( ChildRegions != null ) ChildRegions.Clear();
				if( Countries != null ) Countries.Clear();
			}

			public string ToString( bool extended )
			{
				StringBuilder sb = new StringBuilder();

				sb.AppendFormat( "[ Id:{0}", Id ?? "<null>" );
				if( extended && Name != null ) sb.AppendFormat( ", Name:{0}", Name ?? "<null>" );

				if( Parent != null ) sb.AppendFormat( ", Parent:{0}", Parent.ToString( extended: false ) );

				if( ChildRegions != null && ChildRegions.Count != 0 ) {
					sb.Append( ", Childs:{" ); bool first = true; foreach( var child in ChildRegions ) {
						if( !first ) sb.Append( ", " ); else { sb.Append( " " ); first = false; }
						sb.Append( extended ? child.ToString( extended: false ) : child.Id );
					}
					if( first ) sb.Append( "-" ); else sb.Append( " " );
					sb.Append( "}" );
				}
				if( Countries != null && Countries.Count != 0 ) {
					sb.Append( ", Countries:{" ); bool first = true; foreach( var ctry in Countries ) {
						if( !first ) sb.Append( ", " ); else { sb.Append( " " ); first = false; }
						sb.Append( extended ? ctry.ToString( extended: false ) : ctry.Id );
					}
					if( first ) sb.Append( "-" ); else sb.Append( " " );
					sb.Append( "}" );
				}

				sb.Append( " ]" );
				return sb.ToString();
			}
			public override string ToString()
			{
				return ToString( extended: true );
			}
		}
		public class Country
		{
			public string Id { get; set; }
			public string Name { get; set; }
			public Region Region { get; set; }
			public List<Employee> Employees { get; private set; }

			public Country() { Employees = new List<Employee>(); Clear(); }
			public void Clear()
			{
				Id = null;
				Name = null;
				Region = null;
				if( Employees != null ) Employees.Clear();
			}

			public string ToString( bool extended )
			{
				StringBuilder sb = new StringBuilder();
				sb.AppendFormat( "[ Id:{0}", Id ?? "<null>" );
				if( extended && Name != null ) sb.AppendFormat( ", Name:{0}", Name ?? "<null>" );

				if( Region != null ) sb.AppendFormat( ", Region:{0}", Region.ToString( extended: false ) );

				if( Employees != null && Employees.Count != 0 ) {
					sb.Append( ", Employees:{" ); bool first = true; foreach( var emp in Employees ) {
						if( !first ) sb.Append( ", " ); else { sb.Append( " " ); first = false; }
						sb.Append( extended ? emp.ToString( extended: false ) : emp.Id );
					}
					if( first ) sb.Append( "-" ); else sb.Append( " " );
					sb.Append( "}" );
				}

				sb.Append( " ]" );
				return sb.ToString();
			}
			public override string ToString()
			{
				return ToString( extended: true );
			}
		}
		public class Employee
		{
			public string Id { get; set; }
			public string FirstName { get; set; }
			public string LastName { get; set; }
			public CalendarDate BirthDate { get; set; }
			public bool? Active { get; set; }
			public Employee Manager { get; set; }
			public Country Country { get; set; }
			public CalendarDate JoinDate { get; set; }
			public ClockTime StartTime { get; set; }
			public byte[] Photo { get; set; }
			public List<Employee> Employees { get; set; }

			public Employee() { Employees = new List<Employee>(); Clear(); }
			public void Clear()
			{
				Id = null;
				FirstName = null;
				LastName = null;
				BirthDate = null;
				Active = null;
				JoinDate = null;
				StartTime = null;
				Photo = null;

				Manager = null;
				Country = null;
				if( Employees != null ) Employees.Clear();
			}

			public string ToString( bool extended )
			{
				StringBuilder sb = new StringBuilder();
				sb.AppendFormat( "[ Id:{0}", Id ?? "<null>" );

				if( extended ) {
					if( FirstName != null ) sb.AppendFormat( ", FirstName:{0}", FirstName );
					if( LastName != null ) sb.AppendFormat( ", LastName:{0}", LastName );
					if( BirthDate != null ) sb.AppendFormat( ", BirthDate:{0}", BirthDate );
					if( Active != null ) sb.AppendFormat( ", Active:{0}", Active );
					if( JoinDate != null ) sb.AppendFormat( ", JoinDate:{0}", JoinDate );
					if( StartTime != null ) sb.AppendFormat( ", StartTime:{0}", StartTime );
					if( Photo != null ) sb.AppendFormat( ", Photo:{0}", TypeHelper.ToString( Photo, "[]" ) );
				}

				if( Manager != null ) sb.AppendFormat( ", Manager:{0}", Manager.ToString( extended: false ) );
				if( Country != null ) sb.AppendFormat( ", Country:{0}", Country.ToString( extended: false ) );

				if( Employees != null && Employees.Count != 0 ) {
					sb.Append( ", Employees:{" ); bool first = true; foreach( var emp in Employees ) {
						if( !first ) sb.Append( ", " ); else { sb.Append( " " ); first = false; }
						sb.Append( extended ? emp.ToString( extended: false ) : emp.Id );
					}
					if( first ) sb.Append( "-" ); else sb.Append( " " );
					sb.Append( "}" );
				}

				sb.Append( " ]" );
				return sb.ToString();
			}
			public override string ToString()
			{
				return ToString( extended: true );
			}
		}

		// --------------------------------------------------
		public static KMap<Region> CreateMapRegion( IKLink link )
		{
			var map = new KMap<Region>( link, x => x.Regions );
			map.UnManagedColumns( "ParentId" );

			map.CreateInstance = () => { return new Region(); };
			map.ClearInstance = obj => { obj.Clear(); };

			map.WriteRecord = ( obj, record ) => {
				DEBUG.IndentLine( "\n-- Map<Region>.WriteRecord() Instance = {0}", obj );
				map.BaseWriteRecord( obj, record );
				record.OnSet( "ParentId", () => { return obj.Parent == null ? null : obj.Parent.Id; } );
				DEBUG.Unindent();
			};
			map.LoadRecord = ( record, obj ) => {
				DEBUG.IndentLine( "\n-- Map<Region>.LoadRecord() Record = {0}", record );
				map.BaseLoadRecord( record, obj );
				record.OnGet( "ParentId", val => { obj.Parent = val == null ? null : map.Find( x => x.Id == val ); } );
				DEBUG.Unindent();
			};
			map.OnRefresh = obj => {
				DEBUG.IndentLine( "\n-- Map<Region>.OnRefresh() Instance = {0}", obj );
				obj = map.BaseOnRefresh( obj );
				obj.ChildRegions.Clear(); obj.ChildRegions.AddRange( link.Query<Region>().Where( x => x.ParentId == obj.Id ).ToList() );
				obj.Countries.Clear(); obj.Countries.AddRange( link.Query<Country>().Where( x => x.RegionId == obj.Id ).ToList() );
				DEBUG.Unindent(); return obj;
			};

			map.OnInsert = obj => {
				DEBUG.IndentLine( "\n-- Map<Region>.OnInsert() Instance = {0} ...", obj );
				obj.Parent = link.Sync( obj.Parent, KMaps.SyncReason.Insert );
				obj = map.BaseOnInsert( obj );
				link.Sync<Region>( obj.ChildRegions, KMaps.SyncReason.Insert );
				link.Sync<Country>( obj.Countries, KMaps.SyncReason.Insert );
				DEBUG.Unindent(); return obj;
			};
			map.OnUpdate = obj => {
				DEBUG.IndentLine( "\n-- Map<Region>.OnUpdate() Instance = {0} ...", obj );
				obj.Parent = link.Sync( obj.Parent, KMaps.SyncReason.Update );
				obj = map.BaseOnUpdate( obj );
				link.Sync<Region>( obj.ChildRegions, KMaps.SyncReason.Update );
				link.Sync<Country>( obj.Countries, KMaps.SyncReason.Update );
				DEBUG.Unindent(); return obj;
			};
			map.OnDelete = obj => {
				DEBUG.IndentLine( "\n-- Map<Region>.OnDelete() Instance = {0} ...", obj );
				link.Sync<Region>( obj.ChildRegions, KMaps.SyncReason.Delete );
				link.Sync<Country>( obj.Countries, KMaps.SyncReason.Delete );
				obj = map.BaseOnDelete( obj );
				DEBUG.Unindent(); return obj;
			};

			map.Validate(); return map;
		}
		public static KMap<Country> CreateMapCountry( IKLink link )
		{
			var map = new KMap<Country>( link, x => x.Countries );
			map.UnManagedColumns( "RegionId" );

			map.CreateInstance = () => { return new Country(); };
			map.ClearInstance = obj => { obj.Clear(); };

			map.WriteRecord = ( obj, record ) => {
				DEBUG.IndentLine( "\n-- Map<Country>.WriteRecord() Instance = {0}", obj );
				map.BaseWriteRecord( obj, record );
				record.OnSet( "RegionId", () => { return obj.Region == null ? null : obj.Region.Id; } );
				DEBUG.Unindent();
			};
			map.LoadRecord = ( record, obj ) => {
				DEBUG.IndentLine( "\n-- Map<Country>.LoadRecord() Record = {0}", record );
				map.BaseLoadRecord( record, obj );
				record.OnGet( "RegionId", val => { obj.Region = val == null ? null : link.Find<Region>( x => x.Id == val ); } );
				DEBUG.Unindent();
			};
			map.OnRefresh = obj => {
				DEBUG.IndentLine( "\n-- Map<Country>.OnRefresh() Instance = {0}", obj );
				obj = map.BaseOnRefresh( obj );
				obj.Employees.Clear(); obj.Employees.AddRange( link.Query<Employee>().Where( x => x.CountryId == obj.Id ).ToList() );
				DEBUG.Unindent(); return obj;
			};

			map.OnInsert = obj => {
				DEBUG.IndentLine( "\n-- Map<Country>.OnInsert() Instance = {0} ...", obj );
				obj.Region = link.Sync( obj.Region, KMaps.SyncReason.Insert );
				obj = map.BaseOnInsert( obj );
				link.Sync<Employee>( obj.Employees, KMaps.SyncReason.Insert );
				DEBUG.Unindent(); return obj;
			};
			map.OnUpdate = obj => {
				DEBUG.IndentLine( "\n-- Map<Country>.OnUpdate() Instance = {0} ...", obj );
				obj.Region = link.Sync( obj.Region, KMaps.SyncReason.Update );
				obj = map.BaseOnUpdate( obj );
				link.Sync<Employee>( obj.Employees, KMaps.SyncReason.Update );
				DEBUG.Unindent(); return obj;
			};
			map.OnDelete = obj => {
				DEBUG.IndentLine( "\n-- Map<Country>.OnDelete() Instance = {0} ...", obj );
				link.Sync<Employee>( obj.Employees, KMaps.SyncReason.Delete );
				obj = map.BaseOnDelete( obj );
				DEBUG.Unindent(); return obj;
			};

			map.Validate(); return map;
		}
		public static KMap<Employee> CreateMapEmployee( IKLink link )
		{
			var map = new KMap<Employee>( link, x => x.Employees );
			map.UnManagedColumns( "ManagerId", "CountryId" );

			map.CreateInstance = () => { return new Employee(); };
			map.ClearInstance = obj => { obj.Clear(); };

			map.WriteRecord = ( obj, record ) => {
				DEBUG.IndentLine( "\n-- Map<Employee>.WriteRecord() Instance = {0}", obj );
				map.BaseWriteRecord( obj, record );
				record.OnSet( "ManagerId", () => { return obj.Manager == null ? null : obj.Manager.Id; } );
				record.OnSet( "CountryId", () => { return obj.Country == null ? null : obj.Country.Id; } );
				DEBUG.Unindent();
			};
			map.LoadRecord = ( record, obj ) => {
				DEBUG.IndentLine( "\n-- Map<Employee>.LoadRecord() Record = {0}", record );
				map.BaseLoadRecord( record, obj );
				record.OnGet( "ManagerId", val => { obj.Manager = val == null ? null : link.Find<Employee>( x => x.Id == val ); } );
				record.OnGet( "CountryId", val => { obj.Country = val == null ? null : link.Find<Country>( x => x.Id == val ); } );
				DEBUG.Unindent();
			};
			map.OnRefresh = obj => {
				DEBUG.IndentLine( "\n-- Map<Employee>.OnRefresh() Instance = {0}", obj );
				obj = map.BaseOnRefresh( obj );
				obj.Employees.Clear(); obj.Employees.AddRange( link.Query<Employee>().Where( x => x.ManagerId == obj.Id ).ToList() );
				DEBUG.Unindent(); return obj;
			};

			map.OnInsert = obj => {
				DEBUG.IndentLine( "\n-- Map<Employee>.OnInsert() Instance = {0} ...", obj );
				obj.Manager = link.Sync( obj.Manager, KMaps.SyncReason.Insert );
				obj.Country = link.Sync( obj.Country, KMaps.SyncReason.Insert );
				obj = map.BaseOnInsert( obj );
				link.Sync<Employee>( obj.Employees, KMaps.SyncReason.Insert );
				DEBUG.Unindent(); return obj;
			};
			map.OnUpdate = obj => {
				DEBUG.IndentLine( "\n-- Map<Employee>.OnUpdate() Instance = {0} ...", obj );
				obj.Manager = link.Sync( obj.Manager, KMaps.SyncReason.Update );
				obj.Country = link.Sync( obj.Country, KMaps.SyncReason.Update );
				obj = map.BaseOnUpdate( obj );
				link.Sync<Employee>( obj.Employees, KMaps.SyncReason.Update );
				DEBUG.Unindent(); return obj;
			};
			map.OnDelete = obj => {
				DEBUG.IndentLine( "\n-- Map<Employee>.OnDelete() Instance = {0} ...", obj );
				link.Sync<Employee>( obj.Employees, KMaps.SyncReason.Delete );
				obj = map.BaseOnDelete( obj );
				DEBUG.Unindent(); return obj;
			};

			map.Validate();
			Console.WriteLine( "\n> Map: {0} -- Discarded: {1} -- Schema: {2}",
				map, TypeHelper.ToString( map.DiscardedColumns, "[]" ), map.Schema );
			return map;
		}

		// --------------------------------------------------
		// Use this method to specify the test(s) to run...
		public static void Dispatcher( IKLink link )
		{
			Console.WriteLine( "\n===== Creating the maps..." );
			KMaps.StartCollector( 2500, true ); // Just for DEBUG purposes
			CreateMapEmployee( link );
			CreateMapRegion( link );
			CreateMapCountry( link );
			
			Test_Scenario( link );
			Console.WriteLine( "\n> Still in Transient Mode: {0}", KMaps.OnTransientMode );
		}

		static string IndentString( int level, string str = null )
		{
			if( string.IsNullOrWhiteSpace( str ) ) str = "  ";

			string indent = string.Empty;
			while( ( --level ) >= 0 ) { indent += str; }
			return indent;
		}
		static void PrintEmployee( string title, Employee obj, int level )
		{
			if( string.IsNullOrWhiteSpace( title ) ) title = "Employee";
			Console.WriteLine( "\n{0}> {1}: {2}", IndentString( level ), title, obj );

			level++; foreach( var child in obj.Employees ) PrintEmployee( "Child Employee", child, level );
		}
		static void PrintCountry( string title, Country obj, int level )
		{
			if( string.IsNullOrWhiteSpace( title ) ) title = "Country";
			Console.WriteLine( "\n{0}> {1}: {2}", IndentString( level ), title, obj );

			level++; foreach( var child in obj.Employees ) PrintEmployee( "Child Employee", child, level );
		}
		static void PrintRegion( string title, Region obj, int level )
		{
			if( string.IsNullOrWhiteSpace( title ) ) title = "Region";
			Console.WriteLine( "\n{0}> {1}: {2}", IndentString( level ), title, obj );

			foreach( var ctry in obj.Countries ) PrintCountry( "Child Country", ctry, level + 3 );
			level++; foreach( var child in obj.ChildRegions ) PrintRegion( "Child Region", child, level );
		}

		public static void Test_Find_SingleEmployee( IKLink link )
		{
			Console.WriteLine( "\n===== Find single Employee ..." );
			Employee emp = link.Find<Employee>( x => x.Id = "1001" );

			Console.WriteLine( "\n===== Printing Employees ..." );
			int level = 0;
			PrintEmployee( null, emp, level );
		}
		public static void Test_Find_SingleCountry( IKLink link )
		{
			Console.WriteLine( "\n===== Find single Country ..." );
			Country ctry = link.Find<Country>( x => x.Id = "es" );

			Console.WriteLine( "\n===== Printing Countries ..." );
			int level = 0;
			PrintCountry( null, ctry, level );
		}
		public static void Test_Find_SingleRegion( IKLink link )
		{
			Console.WriteLine( "\n===== Find single Region ..." );
			Region reg = link.Find<Region>( x => x.Id = "200" );

			Console.WriteLine( "\n===== Printing Regions ..." );
			int level = 0;
			PrintRegion( null, reg, level );
		}

		public static Employee Test_Insert( IKLink link )
		{
			Console.WriteLine( "\n===== Insert..." );
			Country ctry = link.Find<Country>( x => x.Id == "us" );
			Employee manager = link.Find<Employee>( x => x.Id == "E2001" );

			Employee emp = new Employee() {
				Id = "007", FirstName = "James", LastName = "Bond",
				Country = ctry,
				Manager = manager
			};

			emp = link.Insert( emp ).Execute(); PrintEmployee( null, emp, 0 );
			return emp;
		}
		public static void Test_Delete( IKLink link, Employee emp )
		{
			Console.WriteLine( "\n===== Delete..." );
			emp = link.Delete( emp ).Execute(); Console.WriteLine( "\n> Deleted: ", emp );
		}

		public static void Test_Delete_Cascading( IKLink link )
		{
			Console.WriteLine( "\n===== Delete All by Cascading ..." );
			Region reg = link.Find<Region>( x => x.Id == "000" );
			reg = link.Delete( reg ).Execute();

			Console.WriteLine( "\n>> Region: {0}", reg == null ? "<null>" : reg.ToString() );
		}

		public static void Test_Scenario( IKLink link )
		{
			Console.WriteLine( "\n===== Cleaning the scenario ...[Enter]" ); Console.ReadLine();
			link.Delete( x => x.Employees ).Execute();
			link.Delete( x => x.Countries ).Execute();
			link.Delete( x => x.Regions ).Execute();

			Console.WriteLine( "\n===== Persisting the hierarchy through a leaf object...[Enter]" ); Console.ReadLine();
			Region rEMEA = new Region() { Id = "200", Name = "EMEA" };
			Region rNE = new Region() { Id = "210", Name = "North Europe", Parent = rEMEA };
			Country cUK = new Country() { Id = "uk", Name = "United Kingdom", Region = rNE };
			Employee eM = new Employee() { Id = "M", Country = cUK, FirstName = "M" };

			Employee e007 = new Employee() { Id = "007", Country = cUK, FirstName = "James", LastName = "Bond", Manager = eM };
			e007.BirthDate = new CalendarDate( 1940, 1, 1 );
			e007.Active = true;
			e007.JoinDate = new CalendarDate( 1965, 1, 1 );
			e007.StartTime = new ClockTime( 8, 0, 0 );
			e007.Photo = new byte[] { 0, 0, 7 };
			e007 = link.Insert( e007 ).Execute();

			Console.WriteLine( "\n>> Region EMEA: {0}", rEMEA );
			Console.WriteLine( "\n>> Region North Europe: {0}", rNE );
			Console.WriteLine( "\n>> Country UK: {0}", cUK );
			Console.WriteLine( "\n>> Employee M: {0}", eM );
			Console.WriteLine( "\n>> Employee 007: {0}", e007 );

			Console.WriteLine( "\n===== Creating another set ...[Enter]" ); Console.ReadLine();
			Region rAMS = new Region() { Id = "100", Name = "Americas" }; link.Insert( rAMS ).Execute();
			Region rNA = new Region() { Id = "110", Name = "North America", Parent = rAMS }; link.Insert( rNA ).Execute();
			Console.WriteLine( "\n> Inserted theatre: {0}", rAMS );

			Country cUSA = new Country() { Id = "us", Name = "United States of America", Region = rNA };
			Employee e009 = new Employee() { Id = "009", FirstName = "John", LastName = "Smith", Country = cUSA };
			e009 = link.Insert( e009 ).Execute();
			Console.WriteLine( "\n> Inserted spy: {0}", e009 );

			Console.WriteLine( "\n===== An update with change in the key columns ...[Enter]" ); Console.ReadLine();
			e009.Country = cUK; e009 = link.Update( e009 ).Execute();
			cUSA.Employees.Remove( e009 ); link.Update( cUSA ).Execute();

			Console.WriteLine( "\n> Updated spy: {0}", e009 );
			Console.WriteLine( "\n>> UK Employees: {0}", TypeHelper.ToString( cUK.Employees ) );
			Console.WriteLine( "\n>> USA Employees: {0}", TypeHelper.ToString( cUSA.Employees ) );

			Console.WriteLine( "\n===== Removing by cascading deletion ...[Enter]" ); Console.ReadLine();
			link.Delete( rEMEA ).Execute();
			link.Delete( rAMS).Execute();
		}
	}
}
// ========================================================
