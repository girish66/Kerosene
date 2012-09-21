// ========================================================
#undef DEBUG
namespace MB.KeroseneORM.Maps
{
	using global::System.Diagnostics;
	using global::System;
	using global::System.Collections;
	using global::System.Collections.Generic;
	using global::MB.Tools;

	// =====================================================
	public static partial class KMaps
	{
		/// <summary>
		/// Enumerates the reason why the synchronization of the business instance is requested.
		/// </summary>
		public enum SyncReason { Insert, Update, Delete };

		/// <summary>
		/// Synchronizes the member given based upon the reason requested.
		/// <para>This object is typically a member of a host business instance, the one that is the one being synchronized
		/// for the reason given.</para>
		/// </summary>
		/// <typeparam name="T">The type of the business object to synchronize.</typeparam>
		/// <param name="map">The map the member to synchronize </param>
		/// <param name="obj">The member to synchronize.</param>
		/// <param name="why">The reason for the synchronization.</param>
		/// <returns>The object synchronized. It can be null if it has been deleted or not found. Typically this reference
		/// returned is used to set the value of the original member.</returns>
		public static T Sync<T>( this KMap<T> map, T obj, SyncReason why ) where T : class
		{
			if( map == null ) throw new ArgumentNullException( "map", "Map cannot be null." );
			if( !KMaps.OnTransientMode ) throw new InvalidOperationException( "MapSync() can only be called from transient mode." );

			object witness = KMaps.EnterTransientMode( "MapSync{0}() => Instance:{1}", why, obj == null ? "<null>" : obj.ToString() );
			DEBUG.Indent();
			try {
				// --------------------------------------------
				if( why == SyncReason.Delete ) {
					if( obj == null ) return null; // The easiest case...

					var meta = KMetaEntity.Get( obj, createMeta: false );
					if( meta == null ) {
						DEBUG.WriteLine( "\n-- MapSync{0}() => No action needed, Not an entity: {1}", why, obj );
						return null;
					}
					if( meta.State == MetaState.Empty ) {
						DEBUG.WriteLine( "\n-- MapSync{0}() => No action needed, Empty entity: {1}", why, obj );
						return null;
					}
					if( meta.State == MetaState.Deleted ) {
						DEBUG.WriteLine( "\n-- MapSync{0}() => No action needed, Instance already deleted: {1}", why, obj );
						return null;
					}
					DEBUG.WriteLine( "\n-- MapSync{0}() => Deleting the entity: {1}", why, obj );
					obj = map.Delete( obj ).Execute();
					return null;
				}
				// --------------------------------------------
				if( why == SyncReason.Insert || why == SyncReason.Update ) {
					if( obj == null ) return null; // The easiest case...

					var meta = KMetaEntity.Get( obj, createMeta: false );
					if( meta == null || meta.State == MetaState.Empty ) {
						DEBUG.WriteLine( "\n-- MapSync{0} => Inserting: {1}", why, obj );
						obj = map.Insert( obj ).Execute();
						return obj;
					}
					if( meta.State == MetaState.Deleted ) {
						DEBUG.WriteLine( "\n-- MapSync{0} => No action needed, Instance was deleted: {1}", why, meta );
						return null;
					}
					DEBUG.WriteLine( "\n-- MapSync{0} => Setting to Dirty: {1}.", why, meta );
					meta.State = MetaState.Dirty;
					return obj;
				}
				// --------------------------------------------
				throw new NotSupportedException( "Reason not supported: " + why );
			}
			finally { DEBUG.Unindent(); KMaps.ExitTransientMode( witness ); }
		}

		/// <summary>
		/// Synchronizes the member given based upon the reason requested.
		/// <para>This object is typically a member of a host business instance, the one that is the one being synchronized
		/// for the reason given.</para>
		/// </summary>
		/// <typeparam name="T">The type of the member to synchronize.</typeparam>
		/// <param name="link">This link object.</param>
		/// <param name="obj">The member to synchronize.</param>
		/// <param name="why">The reason for the synchronization.</param>
		/// <returns>The object synchronized. It can be null if it has been deleted or not found. Typically this reference
		/// returned is used to set the value of the original member.</returns>
		public static T Sync<T>( this IKLink link, T obj, SyncReason why ) where T : class
		{
			if( link == null ) throw new ArgumentNullException( "link", "IKLink cannot be null." );
			var map = link.GetMap<T>( raise: true, forceValidation: true );
			return Sync<T>( map, obj, why );
		}

		/// <summary>
		/// Synchronizes a member of the host business instance, being this member a list of a given type instances, for the
		/// reason given.
		/// </summary>
		/// <typeparam name="T">The type of the business object to synchronize.</typeparam>
		/// <param name="map">The map the member to synchronize </param>
		/// <param name="list">The list member containing the instances to be synchronized.</param>
		/// <param name="why">The reason for the synchronization.</param>
		/// <returns>The list once this elements have been synchronized. Potentially some of them might have been removed if
		/// they have been deleted in the synchronization process.</returns>
		public static List<T> Sync<T>( this KMap<T> map, List<T> list, SyncReason why ) where T : class
		{
			if( map == null ) throw new ArgumentNullException( "map", "Map cannot be null." );

			object witness = KMaps.EnterTransientMode( "MapSync{0}() => List of type <{1}>", why, typeof( T ).Name );
			DEBUG.Indent();
			try {
				if( list == null || list.Count == 0 ) return list; // The easiest case...

				int count = list.Count; for( int i = 0; i < count; i++ ) {
					var obj = Sync<T>( map, list[i], why );
					if( obj == null ) { list.RemoveAt( i ); count--; i--; }
				}
				return list;
			}
			finally { DEBUG.Unindent(); KMaps.ExitTransientMode( witness ); }
		}
		
		/// <summary>
		/// Synchronizes a member of the host business instance, being this member a list of a given type instances, for the
		/// reason given.
		/// </summary>
		/// <typeparam name="T">The type of the members to synchronize.</typeparam>
		/// <param name="link">This link object.</param>
		/// <param name="list">The list member containing the instances to be synchronized.</param>
		/// <param name="why">The reason for the synchronization.</param>
		/// <returns>The list once this elements have been synchronized. Potentially some of them might have been removed if
		/// they have been deleted in the synchronization process.</returns>
		public static List<T> Sync<T>( this IKLink link, List<T> list, SyncReason why ) where T : class
		{
			if( link == null ) throw new ArgumentNullException( "link", "IKLink cannot be null." );
			var map = link.GetMap<T>( raise: true, forceValidation: true );
			return Sync<T>( map, list, why );
		}
	}
}
// ========================================================
