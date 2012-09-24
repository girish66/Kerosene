// ========================================================
#undef DEBUG
namespace MB.KeroseneORM.Maps
{
	using global::System.Diagnostics;
	using global::System;
	using global::MB.Tools;
	using global::MB.Tools.Dynamics;

	// =====================================================
	public static partial class KMaps
	{
		/// <summary>
		/// Creates and returns a new mapped insert command for the given business instance.
		/// </summary>
		/// <typeparam name="T">The type of the business class.</typeparam>
		/// <param name="link">This link object.</param>
		/// <param name="obj">The business instance.</param>
		/// <returns>The new created command.</returns>
		public static KMapCommandInsert<T> Insert<T>( this IKLink link, T obj ) where T : class
		{
			if( link == null ) throw new ArgumentNullException( "link", "Link cannot be null." );
			var map = link.GetMap<T>( raise: true, forceValidation: true );
			var cmd = map.Insert( obj );
			return cmd;
		}
	}

	// =====================================================
	public partial class KMap<T>
	{
		/// <summary>
		/// Creates and returns a new mapped insert command for the given business instance.
		/// </summary>
		/// <param name="obj">The business instance.</param>
		/// <returns>The new created command.</returns>
		public KMapCommandInsert<T> Insert( T obj )
		{
			return new KMapCommandInsert<T>( this, obj );
		}
		IKMapExecutable IKMap.Insert( object obj )
		{
			return this.Insert( (T)obj );
		}

		// --------------------------------------------------
		/// <summary>
		/// The default method to invoke to insert the business instance when the OnInsert delegate has not been set.
		/// </summary>
		/// <param name="obj">The business instance.</param>
		/// <returns>The inserted business instance.</returns>
		public T BaseOnInsert( T obj )
		{
			if( !IsValidated ) throw new InvalidOperationException( "This map is not validated yet." );
			if( obj == null ) throw new ArgumentNullException( "obj", "Instance cannot be null." );

			// Validating the entity...
			var meta = KMetaEntity.Get( obj, createMeta: true );
			if( meta.State != KMaps.MetaState.Empty ) throw new InvalidOperationException( "Only Empty Entities can be inserted: " + meta );

			// Executing the operation...
			object witness = KMaps.EnterTransientMode( "MapInsert() => Instance: {0}", obj );
			KCommandInsert cmd = null;
			DEBUG.Indent();
			try {
				// Generating the command...
				cmd = new KCommandInsert( _Link, x => _Table );

				KRecord current = new KRecord( _Schema ); WriteRecord( obj, current );
				DynamicNode.Argument tag = new DynamicNode.Argument( "x" );
				for( int i = 0; i < current.Count; i++ ) {
					if( current.Schema[i].IsReadOnly ) continue; // Avoiding read-only columns

					DynamicNode.SetMember node = new DynamicNode.SetMember( tag, current.Schema[i].ColumnName, current[i] );
					cmd.Column( x => node );
					node.Dispose();
				}
				tag.Dispose(); tag = null;
				current.Dispose(); current = null;

				// Executing the command...
				KRecord record = (KRecord)cmd.First(); cmd.Dispose(); cmd = null;

				// If operation failed...
				if( record == null ) {
					DEBUG.WriteLine( "\n-- MapInsert() => Failed." );
					return null; // As its state is Empty, it will be removed when exiting the transient mode...
				}

				// Success...
				DEBUG.WriteLine( "\n-- MapInsert() => From record: {0}", record );
				meta.InsertOn( this ); // An Empty entity was not registered yet...

				meta.Record = record;
				meta.State = KMaps.MetaState.Inserted;
				LoadRecord( record, obj );
				return obj;
			}
			finally {
				if( cmd != null ) { cmd.Dispose(); cmd = null; }
				DEBUG.Unindent(); KMaps.ExitTransientMode( witness );
			}
		}

		/// <summary>
		/// Gets or sets the delegate to invoke to insert the the business instance given.
		/// <para>If not explictly set, it will invoke the default BaseOnInsert() method.</para>
		/// <para>When explicitly set the best practice is to invoke the default method to let it deal with the database and
		/// managed columns, and write after and/or before the code needed to manage the dependencies and unmanaged columns.</para>
		/// </summary>
		public Func<T, T> OnInsert
		{
			set
			{
				if( IsValidated ) throw new InvalidOperationException( "This map is already validated." );
				_OnInsert = value;
			}
			get
			{
				Func<T, T> r = obj => {
					if( !IsValidated ) throw new InvalidOperationException( "This map is not validated yet." );
					if( obj == null ) throw new ArgumentNullException( "obj", "Instance cannot be null." );

					if( _OnInsert == null ) obj = BaseOnInsert( obj );
					else {
						string reason = string.Format( "OnInsert() => Instance: {0}", obj );
						object witness = KMaps.EnterTransientMode( reason );
						try { obj = _OnInsert( obj ); }
						finally { KMaps.ExitTransientMode( witness ); }
					}
					return obj;
				};
				return r;
			}
		}
		Func<object, object> IKMap.OnInsert
		{
			get { Func<object, object> r = obj => { return this.OnInsert( (T)obj ); }; return r; }
		}
	}

	// =====================================================
	/// <summary>
	/// Represents a mapped insert command that, when executed, will insert the business instance it is created for.
	/// </summary>
	/// <typeparam name="T">The type of the business class.</typeparam>
	public class KMapCommandInsert<T> : IKMapExecutable<T> where T : class
	{
		T _Instance = null;
		KMap<T> _Map = null;

		/// <summary>
		/// Creates a new insert command associated with the given map and the given business instance.
		/// </summary>
		/// <param name="map">The map this command is associated with.</param>
		/// <param name="obj">The business instance this command is created for.</param>
		public KMapCommandInsert( KMap<T> map, T obj )
		{
			DEBUG.IndentLine( "\n-- KMapCommandInsert<{0}>( Map={1}, Instance={2})", typeof( T ).Name, map == null ? "<null>" : map.ToString(), obj == null ? "<null>" : obj.ToString() );

			if( ( _Map = map ) == null ) throw new ArgumentNullException( "map", "Map cannot be null." );
			if( ( _Instance = obj ) == null ) throw new ArgumentNullException( "obj", "Instance cannot be null." );

			if( !map.IsValidated ) throw new InvalidOperationException( "This map is not validated yet." );

			DEBUG.Unindent();
		}

		protected virtual void Dispose( bool disposing )
		{
			DEBUG.IndentLine( "\n-- KMapCommandInsert<{0}>.Dispose( Disposing={1} ) This={2}", typeof( T ).Name, disposing, this );

			_Instance = null;
			_Map = null;

			DEBUG.Unindent();
		}
		public void Dispose()
		{
			DEBUG.IndentLine( "\n-- KMapCommandInsert<{0}>.Dispose() This={1}", typeof( T ).Name, this );
			Dispose( true );
			GC.SuppressFinalize( this );
			DEBUG.Unindent();
		}
		~KMapCommandInsert()
		{
			DEBUG.IndentLine( "\n-- ~KMapCommandInsert<{0}>() This={1}", typeof( T ).Name, this );
			Dispose( false );
			DEBUG.Unindent();
		}

		public override string ToString()
		{
			return string.Format( "KMapCommandInsert[ {0} ]", _Instance == null ? "<null>" : _Instance.ToString() );
		}

		/// <summary>
		/// Clones this command.
		/// </summary>
		/// <returns>The new created clone.</returns>
		public KMapCommandInsert<T> Clone()
		{
			var cloned = new KMapCommandInsert<T>( _Map, _Instance ); // We don't use the alias here as we ar cloning the command later...
			return cloned;
		}
		object ICloneable.Clone()
		{
			return this.Clone();
		}

		/// <summary>
		/// Executes this command and returns the inserted business intance as it is retrieved from the database.
		/// </summary>
		/// <param name="dispose">True to dispose this command once executed.</param>
		/// <returns>The inserted business instance.</returns>
		public T Execute( bool dispose = true )
		{
			T obj = _Map.OnInsert( _Instance ); if( dispose ) Dispose();
			return obj;
		}
		object IKMapExecutable.Execute( bool dispose )
		{
			return this.Execute( dispose );
		}
	}
}
// ========================================================
