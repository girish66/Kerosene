// ========================================================
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
	public class Test_EasyMaps
	{
		public class Region
		{
			public string Id { get; set; }
			public string Name { get; set; }
			public string ParentId { get; set; }

			public Region() { Clear(); }
			public void Clear() { Id = null; Name = null; ParentId = null; }

			public string ToString( bool extended )
			{
				StringBuilder sb = new StringBuilder();

				sb.AppendFormat( "[ Id:{0}", Id ?? "<null>" );
				if( extended && Name != null ) sb.AppendFormat( ", Name:{0}", Name ?? "<null>" );
				if( ParentId != null ) sb.AppendFormat( ", Parent:{0}", ParentId );

				sb.Append( " ]" );
				return sb.ToString();
			}
			public override string ToString()
			{
				return ToString( extended: true );
			}
		}

		// Use this method to specify the test(s) to run...
		public static void Dispatcher( IKLink link )
		{
			Console.WriteLine( "\n===== Creating the maps..." ); Console.ReadLine();
			KMaps.StartCollector( 2500, true ); // Just for DEBUG purposes
			var map = new KMap<Region>( link, x => x.Regions ); map.Validate();

			var reg = Test_Insert( link );
			Test_Update( link, reg );
			Test_Delete( link, reg );
		}

		public static Region Test_FindRegion( IKLink link, string id )
		{
			Console.WriteLine( "\n===== Finding a given region..." ); Console.ReadLine();
			Region reg = link.Find<Region>( x => x.Id == id );
			Console.WriteLine( "\n> Found Region: {0}", reg );
			return reg;
		}
		public static void Test_Cache_Unicity( IKLink link, Region reg )
		{
			Console.WriteLine( "\n===== Testing unicity of the cache..." ); Console.ReadLine();
			Region neo = link.Find<Region>( x => x.Id == reg.Id );
			Console.WriteLine( "\n> New Region: {0}", neo );
			Console.WriteLine( "\n>> Same entity?: {0}", object.ReferenceEquals( reg, neo ) );
		}
		public static void Test_Refresh( IKLink link, Region reg )
		{
			Console.WriteLine( "\n===== Refreshing a region..." ); Console.ReadLine();
			reg = link.Refresh( reg );
			Console.WriteLine( "\n> Refreshed Region: {0}", reg );
		}

		public static void Test_Query( IKLink link)
		{
			Console.WriteLine( "\n===== Querying some regions..." ); Console.ReadLine();
			var cmd = link.Query<Region>().Where( x => x.Id >= "3" );
			foreach( var obj in cmd ) Console.WriteLine( "\n>> Region: {0}", obj );
		}
		public static Region Test_Insert( IKLink link )
		{
			Console.WriteLine( "\n===== Inserting a region ..." ); Console.ReadLine();
			Region reg = new Region() { Id = "XXX", ParentId = "200" };
			reg = link.Insert( reg ).Execute();
			Console.WriteLine( "\n>> Inserted Region: {0}", reg );
			return reg;
		}
		public static void Test_Update( IKLink link, Region reg )
		{
			Console.WriteLine( "\n===== Updating a region ..." ); Console.ReadLine();
			reg.Name = "New Name";
			reg = link.Update( reg ).Execute();
			Console.WriteLine( "\n>> Updated Region: {0}", reg );
		}
		public static void Test_Delete( IKLink link, Region reg )
		{
			Console.WriteLine( "\n===== Deleting a region ..." ); Console.ReadLine();
			reg = link.Delete( reg ).Execute();
			Console.WriteLine( "\n>> Delete Region: {0}", reg == null ? "null" : reg.ToString() );
		}
	}
}
// ========================================================
