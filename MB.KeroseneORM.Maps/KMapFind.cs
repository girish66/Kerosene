// ========================================================
#undef DEBUG
namespace MB.KeroseneORM.Maps
{
	using global::System.Diagnostics;
	using global::System;
	using global::MB.Tools;

	// =====================================================
	public static partial class KMaps
	{
		/// <summary>
		/// Tries to find in the internal cache the business instance identified by the record given.
		/// <para>If it is not found in  the cache, then a query is executed against the database.</para>
		/// <para>In both cases, only the first match is returned, or null if the object is not found.</para>
		/// </summary>
		/// <typeparam name="T">The type of the business class.</typeparam>
		/// <param name="link">This link object.</param>
		/// <param name="find">The record containing the columns to locate the business instance.</param>
		/// <returns>The object found, or null.</returns>
		public static T Find<T>( this IKLink link, KRecord find ) where T : class
		{
			if( link == null ) throw new ArgumentNullException( "link", "Link cannot be null." );
			var map = link.GetMap<T>( raise: true, forceValidation: true );
			return map.Find( find );
		}

		/// <summary>
		/// Tries to find in the internal cache the business instance identified by the columns and values specified by the
		/// dynamic lambda expressions given.
		/// <para>If it is not found in  the cache, then a query is executed against the database.</para>
		/// <para>In both cases, only the first match is returned, or null if the object is not found.</para>
		/// </summary>
		/// <typeparam name="T">The type of the business class.</typeparam>
		/// <param name="link">This link object.</param>
		/// <param name="specs">The dynamic lambda expressions containing the columns and values to use to locate the business
		/// instance.</param>
		/// <returns>The object found, or null.</returns>
		public static T Find<T>( this IKLink link, params Func<dynamic, object>[] specs ) where T : class
		{
			if( link == null ) throw new ArgumentNullException( "link", "Link cannot be null." );
			var map = link.GetMap<T>( raise: true, forceValidation: true );
			return map.Find( specs );
		}
	}

	// =====================================================
	public partial class KMap<T>
	{
		/// <summary>
		/// Tries to find in the internal cache the business instance identified by the record given.
		/// <para>If it is not found in  the cache, then a query is executed against the database.</para>
		/// <para>In both cases, only the first match is returned, or null if the object is not found.</para>
		/// </summary>
		/// <param name="find">The record containing the columns to locate the business instance.</param>
		/// <returns>The object found, or null.</returns>
		public T Find( KRecord find )
		{
			if( !IsValidated ) throw new InvalidOperationException( "This map is not validated yet." );
			if( find == null ) throw new ArgumentNullException( "find", "Find record cannot be null." );
			if( find.Count == 0 ) throw new ArgumentException( "Record contains no columns." );

			DEBUG.IndentLine( "\n-- MapFind() => Record: {0}", find );
			T obj = null;

			// Finding in the cache is the main topic of Find()...
			var meta = KMetaEntity.First( this, find );
			if( meta != null ) {
				DEBUG.WriteLine( "\n-- MapFind() => Cached entity: {0}", meta );

				if( meta.State != KMaps.MetaState.Empty
					&& meta.State != KMaps.MetaState.Deleted ) {
					DEBUG.Unindent();
					meta.State = KMaps.MetaState.Dirty;
					return (T)meta.Host;
				}
			}

			// Now we have to go to the database...
			var cmd = _Link.Query().From( _Table ).Top( 1 );
			for( int i = 0; i < find.Count; i++ ) cmd.Where( x => x( find.Schema[i].ColumnName ) == find[i] );
			KRecord record = (KRecord)cmd.First(); cmd.Dispose(); cmd = null;

			if( record == null ) {
				DEBUG.WriteLine( "\n-- MapFind() => Not found in the database..." ); DEBUG.Unindent();
				if( meta != null ) meta.State = KMaps.MetaState.Deleted; // Signalling for future removal...
				obj = null;
			}

			else {
				if( meta != null ) {
					DEBUG.WriteLine( "\n-- MapFind() => Updating cache with: {0}", record );
					meta.Record = record;
					meta.State = KMaps.OnTransientMode ? KMaps.MetaState.Dirty : KMaps.MetaState.Ready;
					LoadRecord( record, (T)meta.Host );

					if( !KMaps.OnTransientMode ) obj = OnRefresh( (T)meta.Host );
					else obj = (T)meta.Host;
				}
				else {
					DEBUG.WriteLine( "\n-- MapFind() => New managed entity: {0}", record );
					obj = CreateInstance();
					meta = KMetaEntity.Get( obj, createMeta: true );
					meta.InsertOn( this );

					meta.Record = record;
					meta.State = KMaps.OnTransientMode ? KMaps.MetaState.Dirty : KMaps.MetaState.Ready;
					LoadRecord( record, (T)meta.Host );

					if( !KMaps.OnTransientMode ) obj = OnRefresh( (T)meta.Host );
					else obj = (T)meta.Host;
				}
			}

			DEBUG.Unindent();
			return obj;
		}
		object IKMap.Find( KRecord find )
		{
			return this.Find( find );
		}

		/// <summary>
		/// Tries to find in the internal cache the business instance identified by the columns and values specified by the
		/// dynamic lambda expressions given.
		/// <para>If it is not found in  the cache, then a query is executed against the database.</para>
		/// <para>In both cases, only the first match is returned, or null if the object is not found.</para>
		/// </summary>
		/// <param name="specs">The dynamic lambda expressions containing the columns and values to use to locate the business
		/// instance.</param>
		/// <returns>The object found, or null.</returns>
		public T Find( params Func<dynamic, object>[] specs )
		{
			if( !IsValidated ) throw new InvalidOperationException( "This Map is not validated yet." );
			if( specs == null ) throw new ArgumentNullException( "specs", "Specifications cannot be null." );
			if( specs.Length == 0 ) throw new ArgumentException( "Specifications list is empty." );

			KRecordBuilder builder = new KRecordBuilder( _Link.DbCaseSensitiveNames );
			foreach( var spec in specs ) builder.Add( spec );
			KRecord find = builder.Generate( autoDispose: true );

			T obj = Find( find ); find.Schema.Dispose(); find.Dispose();
			return obj;
		}
		object IKMap.Find( params Func<dynamic, object>[] specs )
		{
			return this.Find( specs );
		}
	}
}
// ========================================================
