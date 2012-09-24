// ========================================================
#undef DEBUG
namespace MB.KeroseneORM.Maps
{
	using global::System.Diagnostics;
	using global::System;
	using global::System.Linq.Expressions;
	using global::MB.Tools;
	using global::MB.Tools.Dynamics;

	// =====================================================
	public static partial class KMaps
	{
		/// <summary>
		/// Creates and returns a new mapped update command for the given business instance.
		/// </summary>
		/// <typeparam name="T">The type of the business class.</typeparam>
		/// <param name="link">This link object.</param>
		/// <param name="obj">The business instance.</param>
		/// <returns>The new created command.</returns>
		public static KMapCommandUpdate<T> Update<T>( this IKLink link, T obj ) where T : class
		{
			if( link == null ) throw new ArgumentNullException( "link", "Link cannot be null." );
			var map = link.GetMap<T>( raise: true, forceValidation: true );
			var cmd = map.Update( obj );
			return cmd;
		}
	}

	// =====================================================
	public partial class KMap<T>
	{
		/// <summary>
		/// Creates and returns a new mapped update command for the given business instance.
		/// </summary>
		/// <param name="obj">The business instance.</param>
		/// <returns>The new created command.</returns>
		public KMapCommandUpdate<T> Update( T obj )
		{
			return new KMapCommandUpdate<T>( this, obj );
		}
		IKMapExecutable IKMap.Update( object obj )
		{
			return this.Update( (T)obj );
		}

		// --------------------------------------------------
		/// <summary>
		/// The default method to invoke to update the business instance when the OnUpdate delegate has not been set.
		/// </summary>
		/// <param name="obj">The business instance.</param>
		/// <returns>The updated business instance.</returns>
		public T BaseOnUpdate( T obj )
		{
			if( !IsValidated ) throw new InvalidOperationException( "This map is not validated yet." );
			if( obj == null ) throw new ArgumentNullException( "obj", "Instance cannot be null." );

			// Validating the entity...
			var meta = KMetaEntity.Get( obj, createMeta: false );
			if( meta == null ) throw new InvalidOperationException( "Instance is not a registered entity: " + obj );
			if( meta.Map != this ) throw new InvalidOperationException( "Instance is not registered with this map: " + obj );
			if( meta.State == KMaps.MetaState.Empty ) throw new InvalidOperationException( "Empty entities cannot be updated: " + meta );
			if( meta.State == KMaps.MetaState.Deleted ) throw new InvalidOperationException( "Empty entities cannot be updated: " + meta );

			if( meta.State == KMaps.MetaState.Updated ) return null; // The easiest case...

			// Getting the changes, if any...
			KRecord current = new KRecord( _Schema ); WriteRecord( obj, current );
			KRecord changes = meta.Record == null ? current : current.Changes( meta.Record, strict: false );
			current.Dispose();

			if( changes == null ) {
				DEBUG.WriteLine( "\n-- MapUpdate() => No changes detected for instance: {0}", obj );
				return obj;
			}

			// Executing the operation...
			object witness = KMaps.EnterTransientMode( "MapUpdate() => Instance: {0}", obj );
			KCommandUpdate cmd = null;
			DEBUG.Indent();
			try {
				// Generating the command...
				cmd = new KCommandUpdate( _Link, x => _Table );
				DynamicNode.Argument tag = new DynamicNode.Argument( "x" );

				KRecord identity = new KRecord( _SchemaId ); WriteRecord( obj, identity );
				for( int i = 0; i < identity.Count; i++ ) {
					DynamicNode.GetMember left = new DynamicNode.GetMember( tag, identity.Schema[i].ColumnName );
					DynamicNode.Binary node = new DynamicNode.Binary( left, ExpressionType.Equal, identity[i] );
					cmd.Where( x => node );
					node.Dispose();
					left.Dispose();
				}
				identity.Dispose(); identity = null;

				for( int i = 0; i < changes.Count; i++ ) {
					if( changes.Schema[i].IsReadOnly ) continue; // Avoiding read-only columns

					DynamicNode.SetMember node = new DynamicNode.SetMember( tag, changes.Schema[i].ColumnName, changes[i] );
					cmd.Column( x => node );
					node.Dispose();
				}
				changes.Dispose(); changes = null;
				tag.Dispose(); tag = null;

				// Executing the command...
				KRecord record = (KRecord)cmd.First(); cmd.Dispose(); cmd = null;

				// Operation failed...
				if( record == null ) {
					DEBUG.WriteLine( "\n-- MapUpdate() => Failed." );
					meta.State = KMaps.MetaState.Deleted; // Signalling for future removal...
					return null;
				}

				// Success...
				DEBUG.WriteLine( "\n-- MapUpdate() => Updating with: {0}", record );
				meta.Record = record;
				meta.State = KMaps.MetaState.Updated;
				LoadRecord( record, obj );
				return obj;
			}
			finally {
				if( cmd != null ) { cmd.Dispose(); cmd = null; }
				DEBUG.Unindent(); KMaps.ExitTransientMode( witness );
			}
		}

		/// <summary>
		/// Gets or sets the delegate to invoke to update the the business instance given.
		/// <para>If not explictly set, it will invoke the default BaseOnUpdate() method. When explicitly set, the best
		/// practice is to invoke the default method to let it deal with the database and managed columns, and write after
		/// and/or before the code needed to manage the dependencies and unmanaged columns.</para>
		/// </summary>
		public Func<T, T> OnUpdate
		{
			set
			{
				if( IsValidated ) throw new InvalidOperationException( "This map is already validated." );
				_OnUpdate = value;
			}
			get
			{
				Func<T, T> r = obj => {
					if( !IsValidated ) throw new InvalidOperationException( "This map is not validated yet." );
					if( obj == null ) throw new ArgumentNullException( "obj", "Instance cannot be null." );

					if( _OnUpdate == null ) obj = BaseOnUpdate( obj );
					else {
						string reason = string.Format( "OnUpdate() => Instance: {0}", obj );
						object witness = KMaps.EnterTransientMode( reason );
						try { obj = _OnUpdate( obj ); }
						finally { KMaps.ExitTransientMode( witness ); }
					}
					return obj;
				};
				return r;
			}
		}
		Func<object, object> IKMap.OnUpdate
		{
			get { Func<object, object> r = obj => { return this.OnUpdate( (T)obj ); }; return r; }
		}
	}

	// =====================================================
	/// <summary>
	/// Represents a mapped update command that, when executed, will update the business instance it is created for.
	/// </summary>
	/// <typeparam name="T">The type of the business class.</typeparam>
	public class KMapCommandUpdate<T> : IKMapExecutable<T> where T : class
	{
		T _Instance = null;
		KMap<T> _Map = null;

		/// <summary>
		/// Creates a new updated command associated with the given map and the given business instance.
		/// </summary>
		/// <param name="map">The map this command is associated with.</param>
		/// <param name="obj">The business instance this command is created for.</param>
		public KMapCommandUpdate( KMap<T> map, T obj )
		{
			DEBUG.IndentLine( "\n-- KMapCommandUpdate<{0}>( Map={1}, Instance={2})", typeof( T ).Name, map == null ? "<null>" : map.ToString(), obj == null ? "<null>" : obj.ToString() );

			if( ( _Map = map ) == null ) throw new ArgumentNullException( "map", "Map cannot be null." );
			if( ( _Instance = obj ) == null ) throw new ArgumentNullException( "obj", "Instance cannot be null." );

			if( !map.IsValidated ) throw new InvalidOperationException( "This map is not validated yet." );

			DEBUG.Unindent();
		}

		protected virtual void Dispose( bool disposing )
		{
			DEBUG.IndentLine( "\n-- KMapCommandUpdate<{0}>.Dispose( Disposing={1} ) This={2}", typeof( T ).Name, disposing, this );

			_Instance = null;
			_Map = null;

			DEBUG.Unindent();
		}
		public void Dispose()
		{
			DEBUG.IndentLine( "\n-- KMapCommandUpdate<{0}>.Dispose() This={1}", typeof( T ).Name, this );
			Dispose( true );
			GC.SuppressFinalize( this );
			DEBUG.Unindent();
		}
		~KMapCommandUpdate()
		{
			DEBUG.IndentLine( "\n-- ~KMapCommandUpdate<{0}>() This={1}", typeof( T ).Name, this );
			Dispose( false );
			DEBUG.Unindent();
		}

		public override string ToString()
		{
			return string.Format( "KMapCommandUpdate[ {0} ]", _Instance == null ? "<null>" : _Instance.ToString() );
		}

		/// <summary>
		/// Clones this command.
		/// </summary>
		/// <returns>The new created clone.</returns>
		public KMapCommandUpdate<T> Clone()
		{
			var cloned = new KMapCommandUpdate<T>( _Map, _Instance ); // We don't use the alias here as we are cloning thecommand later...
			return cloned;
		}
		object ICloneable.Clone()
		{
			return this.Clone();
		}

		/// <summary>
		/// Executes this command and returns the updated business intance as it is retrieved from the database.
		/// </summary>
		/// <param name="dispose">True to dispose this command once executed.</param>
		/// <returns>The updated business instance.</returns>
		public T Execute( bool dispose = true )
		{
			T obj = _Map.OnUpdate( _Instance ); if( dispose ) Dispose();
			return obj;
		}
		object IKMapExecutable.Execute( bool dispose )
		{
			return this.Execute( dispose );
		}
	}
}
// ========================================================
