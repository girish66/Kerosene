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
		/// Refreshes the given business instance obtaining its most current contents from the database.
		/// </summary>
		/// <typeparam name="T">The type of the business class.</typeparam>
		/// <param name="link">This link objects.</param>
		/// <param name="obj">The business instance.</param>
		/// <returns>The object refreshed, or null if it is not found in the database.</returns>
		public static T Refresh<T>( this IKLink link, T obj ) where T : class
		{
			if( link == null ) throw new ArgumentNullException( "link", "Link cannot be null." );
			var map = link.GetMap<T>( raise: true, forceValidation: true );
			return map.Refresh( obj );
		}
	}

	// =====================================================
	public partial class KMap<T>
	{
		/// <summary>
		/// Refreshes the given business instance obtaining its most current contents from the database.
		/// </summary>
		/// <param name="obj">The object to be refreshed.</param>
		/// <returns>The object refreshed, or null if it is not found in the database.</returns>
		public T Refresh( T obj )
		{
			if( !IsValidated ) throw new InvalidOperationException( "This map is not validated yet." );
			if( obj == null ) throw new ArgumentNullException( "obj", "Instance cannot be null." );

			// Refresh() is imperative: we don't enter into Transient Mode...
			DEBUG.IndentLine( "\n-- MapRefresh() => Instance: {0}", obj );

			// Validating the instance received...
			var meta = KMetaEntity.Get( obj, createMeta: false );
			if( meta == null ) throw new ArgumentException( "Instance does not carry a MetaEntity." );
			if( meta.Map != this ) throw new ArgumentException( "Instance is registered into another map." );
			if( meta.State == KMaps.MetaState.Empty ) throw new ArgumentException( "Instance carries an empty MetaEntity." );

			// Deleted entities do not need to be refreshed...
			if( meta.State == KMaps.MetaState.Deleted ) {
				DEBUG.WriteLine( "\n-- MapRefresh() => Deleted entities are not refreshed." ); DEBUG.Unindent();
				return null;
			}

			// Preparing the command...
			KRecord find = new KRecord( SchemaId ); WriteRecord( obj, find );

			var cmd = _Link.Query().From( _Table ).Top( 1 );
			for( int i = 0; i < find.Count; i++ ) cmd.Where( x => x( find.Schema[i].ColumnName ) == find[i] );
			find.Dispose();

			// Executing the command...
			KRecord record = (KRecord)cmd.First(); cmd.Dispose(); cmd = null;

			if( record == null ) {
				DEBUG.WriteLine( "\n-- MapRefresh() => Not found in the database..." ); DEBUG.Unindent();
				meta.State = KMaps.MetaState.Deleted; // Signalling for future removal...
				return null;
			}

			DEBUG.WriteLine( "\n-- MapRefresh() => Updating with: {0}", record );
			meta.Record = record;
			meta.State = KMaps.MetaState.Ready; // Setting to Ready as this is a Refresh operation...
			LoadRecord( record, (T)meta.Host ); // Loading the record...
			obj = OnRefresh( (T)meta.Host ); // And refreshing it...

			DEBUG.Unindent();
			return obj;
		}
		object IKMap.Refresh( object obj )
		{
			return this.Refresh( (T)obj );
		}
	}
}
// ========================================================
