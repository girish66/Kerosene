// ========================================================
#undef DEBUG
namespace MB.KeroseneORM
{
	using global::System.Diagnostics;
	using global::System;
	using global::MB.Tools;

	// =====================================================
	public static class KRecordHelper
	{
		/// <summary>
		/// Gets the value from the column identified by its table and column names, and if such column exists, uses its value
		/// as the parameter to execute the onGet delegate. If the column does not exist, or if the delegate is null, this
		/// method merely returns.
		/// </summary>
		/// <typeparam name="T">The type of the content of the column.</typeparam>
		/// <param name="record">This record.</param>
		/// <param name="table">The table name.</param>
		/// <param name="column">The column name.</param>
		/// <param name="onGet">The delegate to execute if the record contains the column to use the value.</param>
		public static void OnGet<T>( this KRecord record, string table, string column, Action<T> onGet )
		{
			if( record == null ) throw new ArgumentNullException( "record", "KRecord cannot be null." );
			if( onGet == null ) return;

			var ix = record.Schema.IndexOf( table, column ); if( ix < 0 ) return;
			onGet( (T)record[ix] );
		}
		public static void OnGet<T>( this KRecord record, string column, Action<T> onGet )
		{
			record.OnGet<T>( null, column, onGet );
		}

		/// <summary>
		/// Gets the value from the column identified by its table and column names, and if such column exists, uses its value
		/// as the parameter to execute the onGet delegate. If the column does not exist, or if the delegate is null, this
		/// method merely returns.
		/// </summary>
		/// <param name="record">This record.</param>
		/// <param name="table">The table name.</param>
		/// <param name="column">The column name.</param>
		/// <param name="onGet">The delegate to execute if the record contains the column to use the value.</param>
		public static void OnGet( this KRecord record, string table, string column, Action<object> onGet )
		{
			if( record == null ) throw new ArgumentNullException( "record", "KRecord cannot be null." );
			if( onGet == null ) return;

			var ix = record.Schema.IndexOf( table, column ); if( ix < 0 ) return;
			onGet( record[ix] );
		}
		public static void OnGet( this KRecord record, string column, Action<object> onGet )
		{
			record.OnGet( null, column, onGet );
		}

		/// <summary>
		/// Sets the value of the column identified by its table and column names with the value obtained from the delegate
		/// onSet. If the column does not exist, or if the delegate is null, this method merely returns.
		/// </summary>
		/// <typeparam name="T">The type of the content of the column.</typeparam>
		/// <param name="record">This record.</param>
		/// <param name="table">The table name.</param>
		/// <param name="column">The column name.</param>
		/// <param name="onSet">The delegate to obtain the value to set into this column.</param>
		public static void OnSet<T>( this KRecord record, string table, string column, Func<T> onSet )
		{
			if( record == null ) throw new ArgumentNullException( "record", "KRecord cannot be null." );
			if( onSet == null ) return;

			var ix = record.Schema.IndexOf( table, column ); if( ix < 0 ) return;
			record[ix] = onSet();
		}
		public static void OnSet<T>( this KRecord record, string column, Func<T> onSet )
		{
			record.OnSet<T>( null, column, onSet );
		}

		/// <summary>
		/// Sets the value of the column identified by its table and column names with the value obtained from the delegate
		/// onSet. If the column does not exist, or if the delegate is null, this method merely returns.
		/// </summary>
		/// <param name="record">This record.</param>
		/// <param name="table">The table name.</param>
		/// <param name="column">The column name.</param>
		/// <param name="onSet">The delegate to obtain the value to set into this column.</param>
		public static void OnSet( this KRecord record, string table, string column, Func<object> onSet )
		{
			if( record == null ) throw new ArgumentNullException( "record", "KRecord cannot be null." );
			if( onSet == null ) return;

			var ix = record.Schema.IndexOf( table, column ); if( ix < 0 ) return;
			record[ix] = onSet();
		}
		public static void OnSet( this KRecord record, string column, Func<object> onSet )
		{
			record.OnSet( null, column, onSet );
		}
	}
}
// ========================================================
