// ========================================================
#undef DEBUG
namespace MB.KeroseneORM.Maps
{
	using global::System.Diagnostics;
	using global::System;
	using global::System.Collections;
	using global::System.Collections.Generic;
	using global::MB.Tools;
	using global::MB.Tools.Dynamics;

	// =====================================================
	public static partial class KMaps
	{
		/// <summary>
		/// Creates and return a mapped query command.
		/// </summary>
		/// <typeparam name="T">The type of the business class.</typeparam>
		/// <param name="link">This link object.</param>
		/// <param name="alias">The alias to use with the primary table, if needed.</param>
		/// <returns>The new created mapped query command.</returns>
		public static KMapCommandQuery<T> Query<T>( this IKLink link, Func<dynamic, object> alias = null ) where T : class
		{
			if( link == null ) throw new ArgumentNullException( "link", "Link cannot be null." );
			var map = link.GetMap<T>( raise: true, forceValidation: true );
			var cmd = map.Query( alias );
			return cmd;
		}
	}

	// =====================================================
	public partial class KMap<T>
	{
		/// <summary>
		/// Creates and return a mapped query command.
		/// </summary>
		/// <param name="alias">The alias to use with the primary table, if needed.</param>
		/// <returns>The new created mapped query command.</returns>
		public KMapCommandQuery<T> Query( Func<dynamic, object> alias = null )
		{
			return new KMapCommandQuery<T>( this, alias );
		}
		IKMapEnumerable IKMap.Query( Func<dynamic, object> alias )
		{
			return this.Query( alias );
		}
	}

	// =====================================================
	/// <summary>
	/// Represents a mapped query command that, when executed by enumerating it, will return instances of the business type.
	/// </summary>
	/// <typeparam name="T">The type of the business type.</typeparam>
	public class KMapCommandQuery<T> : IKMapEnumerable<T>, IKMapEnumerator<T> where T : class
	{
		KMap<T> _Map = null;
		string _Alias = null;
		KCommandQuery _Command = null;

		/// <summary>
		/// Creates a new instance of the mapped query command associated with the given map.
		/// <para>An internal query command is generated using the columns specified in the map's schema. Also, if its
		/// WhereFilter property is not null, it is used to set the initial contents of its Where clause.</para>
		/// </summary>
		/// <param name="map">The map this command is associated with.</param>
		/// <param name="alias">The alias to use with the primary table of the map, if needed.</param>
		public KMapCommandQuery( KMap<T> map, Func<dynamic, object> alias = null )
		{
			DEBUG.IndentLine( "\n-- KMapCommandQuery<{0}>( Map={1}, PrimaryAlias=?? )", typeof( T ).Name, map == null ? "<null>" : map.ToString() );

			if( ( _Map = map ) == null ) throw new ArgumentNullException( "map", "KMap<T> cannot be null." );
			if( !map.IsValidated ) throw new InvalidOperationException( "This map is not validated yet." );

			string nick = null; if( alias != null ) {
				var result = DynamicParser.Parse( alias ).Result;
				if( result == null ) throw new ArgumentException( "Primary Alias cannot resolve to null." );
				if( result is string ) nick = (string)result;
				nick = result.ToString();
				nick = KParser.ExtractTag( nick );
			}
			if( nick != null ) nick = nick.Validated( "Primary Alias", invalidChars: TypeHelper.InvalidNameChars );
			DEBUG.WriteLine( "\n-- Primary Alias: {0}", nick ?? "null" );
			_Alias = nick;

			_Command = new KCommandQuery( map.Link );
			_Command.From( nick == null ? map.Table : string.Format( "{0} AS {1}", map.Table, nick ) );

			foreach( var col in map.Schema ) {
				string name = nick == null ? col.ColumnName : string.Format( "{0}.{1}", nick, col.ColumnName );
				_Command.Select( name );
			}

			// Using the filter if exists...
			if( _Map.WhereFilter != null ) _Command.Where( _Map.WhereFilter );

			DEBUG.Unindent();
		}

		protected virtual void Dispose( bool disposing )
		{
			DEBUG.IndentLine( "\n-- KMapCommandQuery<{0}>.Dispose( Disposing={1} ) - This={2}", typeof( T ).Name, disposing, this );

			Reset();
			if( _Command != null ) { _Command.Dispose(); _Command = null; }
			_Map = null;

			DEBUG.Unindent();
		}
		public void Dispose()
		{
			DEBUG.IndentLine( "\n-- KMapCommandQuery<{0}>.Dispose() - This={1}", typeof( T ).Name, this );
			Dispose( true );
			GC.SuppressFinalize( this );
			DEBUG.Unindent();
		}
		~KMapCommandQuery()
		{
			DEBUG.IndentLine( "\n-- ~KMapCommandQuery<{0}>() - This={1}", typeof( T ).Name, this );
			Dispose( false );
			DEBUG.Unindent();
		}

		public override string ToString()
		{
			return string.Format( "KMapCommandQuery[ {0} ]", _Command == null ? "<null>" : _Command.ToString() );
		}

		/// <summary>
		/// Clones this command.
		/// </summary>
		/// <returns>The new cloned command.</returns>
		public KMapCommandQuery<T> Clone()
		{
			var cloned = new KMapCommandQuery<T>( _Map, null ); // We don't use the alias here as we ar cloning the command later...
			cloned._Alias = _Alias;
			cloned._Command = _Command.Clone();
			return cloned;
		}
		object ICloneable.Clone()
		{
			return this.Clone();
		}

		public KMapCommandQuery<T> GetEnumerator()
		{
			return this;
		}
		IKMapEnumerator<T> IKMapEnumerable<T>.GetEnumerator()
		{
			return this.GetEnumerator();
		}
		IKMapEnumerator IKMapEnumerable.GetEnumerator()
		{
			return this.GetEnumerator();
		}
		IEnumerator<T> IEnumerable<T>.GetEnumerator()
		{
			return this.GetEnumerator();
		}
		IEnumerator IEnumerable.GetEnumerator()
		{
			return this.GetEnumerator();
		}

		IKEnumerator _Enumerator = null;
		T _Current = null;

		/// <summary>
		/// Advances this enumerator to return the next business instance from the database. 
		/// <para>While these instances are in use by the  application they are maintained in an internal cache.</para>
		/// <para>They can be automatically removed by the entity collector when they are not in use any longer.</para>
		/// </summary>
		/// <returns>True if the next element has been obtained, false if there are no more elements available.</returns>
		public bool MoveNext()
		{
			// First iteration...
			if( _Enumerator == null ) {
				_Enumerator = _Command.GetEnumerator();
				DEBUG.Indent();
			}

			// Next iteration...
			bool r = _Enumerator.MoveNext(); if( r ) {
				KRecord record = _Enumerator.CurrentRecord;

				// Finding in the cache...
				KRecord find = new KRecord( _Map.SchemaId );
				foreach( var col in _Map.SchemaId ) record.OnGet( null, col.ColumnName, val => { find[col.ColumnName] = val; } );
				var meta = KMetaEntity.First( _Map, find ); find.Dispose(); find = null;

				if( meta != null ) {
					DEBUG.WriteLine( "\n-- MapQuery() => Updating with: {0}", record );
					meta.Record = record;
					meta.State = KMaps.OnTransientMode ? KMaps.MetaState.Dirty : KMaps.MetaState.Ready;

					T obj = (T)meta.Host; _Map.LoadRecord( record, (T)meta.Host );
					if( !KMaps.OnTransientMode ) obj = _Map.OnRefresh( obj );
					_Current = obj;
				}
				else {
					DEBUG.WriteLine( "\n-- MapQuery() => New managed entity: {0}", record );
					T obj = _Map.CreateInstance();
					meta = KMetaEntity.Get( obj, createMeta: true );
					meta.InsertOn( _Map );
					meta.State = KMaps.OnTransientMode ? KMaps.MetaState.Dirty : KMaps.MetaState.Ready;

					_Map.LoadRecord( record, obj );
					if( !KMaps.OnTransientMode ) obj = _Map.OnRefresh( obj );
					_Current = obj;
				}
			}

			// Last iteration...
			else {
				DEBUG.WriteLine( "\n-- MapQuery() => No more records" );
				Reset();
			}
			return r;
		}

		/// <summary>
		/// Resets the enumerator associated with this command.
		/// </summary>
		public void Reset()
		{
			if( _Enumerator != null ) {
				DEBUG.Unindent();
				_Enumerator.Reset();
				_Enumerator.Dispose(); _Enumerator = null;
			}
			_Current = null;
		}

		/// <summary>
		/// Returns the current business instance from the associated enumerator.
		/// </summary>
		public T Current
		{
			get { return _Current; }
		}
		object IEnumerator.Current
		{
			get { return this.Current; }
		}

		// --------------------------------------------------
		/// <summary>
		/// Appends to the From clause the contents specified in the given string, in the form "main AS alias" where the
		/// alias part is optional.
		/// </summary>
		/// <param name="spec">The string to append to the From clause.</param>
		/// <returns>This command, to allow it to permit it to be used in a fluent syntax fashion.</returns>
		public virtual KMapCommandQuery<T> From( string spec )
		{
			_Command.From( spec );
			return this;
		}

		/// <summary>
		/// Appends to the From clause the contents obtained from parsing the dynamic lamnda expression given. They typically
		/// use the syntax "x => x.Table.As( alias )", where the alias part is optional.
		/// </summary>
		/// <param name="spec">The dynamic lambda expressions to be parsed.</param>
		/// <returns>This command, to allow it to permit it to be used in a fluent syntax fashion.</returns>
		public virtual KMapCommandQuery<T> From( params Func<dynamic, object>[] specs )
		{
			_Command.From( specs );
			return this;
		}

		/// <summary>
		/// Appends to the From clause the contents obtained by parsing the command object given, and associating
		/// it to a given alias.
		/// </summary>
		/// <param name="command">The command object.</param>
		/// <param name="alias">The alias.</param>
		/// <returns>This command, to allow it to permit it to be used in a fluent syntax fashion.</returns>
		public virtual KMapCommandQuery<T> From( IKCommand command, Func<dynamic, object> alias )
		{
			_Command.From( command, alias );
			return this;
		}

		/// <summary>
		/// Appends a Join clause specifying its type, and using the contents obtained from parsing the dynamic lamda
		/// expression given.
		/// <para>They use the syntax "x => x.Table.As( alias ).On( condition )", where the As() method is optional, and the
		/// On() method is used to specify the conditions of the Join clause.</para>
		/// </summary>
		/// <param name="type">A string containing the type of the Join clause, as "JOIN", "LEFT JOIN", etc.</param>
		/// <param name="spec">The dynamic lambda expression to be parsed.</param>
		/// <returns>This command, to allow it to permit it to be used in a fluent syntax fashion.</returns>
		public virtual KMapCommandQuery<T> Join( string type, Func<dynamic, object> spec )
		{
			_Command.Join( type, spec );
			return this;
		}

		/// <summary>
		/// Appends a Join clause using the contents obtained from parsing the dynamic lambda expression given.
		/// <para>They use the syntax "x => x.Table.As( alias ).On( condition )", where the As() method is optional, and the
		/// On() method is used to specify the conditions of the Join clause.</para>
		/// </summary>
		/// <param name="spec">The dynamic lambda expression to be parsed.</param>
		/// <returns>This command, to allow it to permit it to be used in a fluent syntax fashion.</returns>
		public virtual KMapCommandQuery<T> Join( Func<dynamic, object> spec )
		{
			_Command.Join( spec );
			return this;
		}

		/// <summary>
		/// Appends to the Where clause the contents obtained from parsing the dynamic lambda expression given.
		/// <para>They are typically binary expressions specifying the filter condition of the Where clause.</para>
		/// <para>If any previous contents exists, they are appended with an AND logical operator by default.</para>
		/// <para>To append them with an OR logical operator use the "x => x.Or(...)" syntax.</para>
		/// </summary>
		/// <param name="spec">The dynamic lambda expression to be parsed.</param>
		/// <returns>This command, to allow it to permit it to be used in a fluent syntax fashion.</returns>
		public virtual KMapCommandQuery<T> Where( Func<dynamic, object> spec )
		{
			_Command.Where( spec );
			return this;
		}

		/// <summary>
		/// Appends to the Order By clause the contents obtained from parsing the dynamic lambda expression given, and with
		/// the ordering mode specified.
		/// <para>These expressions typically have the form "x => x.Table.Column", where the table part is optional and can
		/// also be an alias.</para>
		/// </summary>
		/// <param name="column">The dynamic lambda expression to be parsed.</param>
		/// <param name="ascending">Whether the ordering requested will be ascending or descending.</param>
		/// <returns>This command, to allow it to permit it to be used in a fluent syntax fashion.</returns>
		public virtual KMapCommandQuery<T> OrderBy( Func<dynamic, object> column, bool ascending = true )
		{
			_Command.OrderBy( column, ascending );
			return this;
		}

		/// <summary>
		/// Sets the contents of the Top clause. The previous contents of this clause are discarded.
		/// </summary>
		/// <param name="top">The number of TOP records to return.</param>
		/// <returns>This command, to allow it to permit it to be used in a fluent syntax fashion.</returns>
		public virtual KMapCommandQuery<T> Top( int top )
		{
			_Command.Top( top );
			return this;
		}
	}
}
// ========================================================
