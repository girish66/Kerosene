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
	/// <summary>
	/// Represents a generic executable mapped command, that when executed will return the affected business instance
	/// specified when creating the command.
	/// </summary>
	public interface IKMapExecutable : IDisposable, ICloneable
	{
		/// <summary>
		/// Executes the command and returns the instance affected.
		/// </summary>
		/// <param name="dispose">To dispose the executor as soon as the command is executed.</param>
		/// <returns>The instance affected.</returns>
		object Execute( bool dispose = true );
	}

	// -----------------------------------------------------
	/// <summary>
	/// Represents a typped executable mapped command, that when executed will return the affected business instance
	/// specified when creating the command.
	/// </summary>
	/// <typeparam name="T">The type of the business class.</typeparam>
	public interface IKMapExecutable<T> : IKMapExecutable where T : class
	{
		new T Execute( bool dispose = true );
	}

	// =====================================================
	/// <summary>
	/// Represents a generic mapped enumerator.
	/// </summary>
	public interface IKMapEnumerator : IEnumerator, IDisposable, ICloneable
	{
	}

	/// <summary>
	/// Represents a generic mapped command, able to be enumerated to produce instances of the business class.
	/// </summary>
	public interface IKMapEnumerable : IEnumerable, IDisposable
	{
		new IKMapEnumerator GetEnumerator();
	}
	public static partial class IKMapEnumerableHelper
	{
		/// <summary>
		/// Executes the command and returns a list, potentially empty, containing the records returned from the database.
		/// </summary>
		/// <param name="command">This command.</param>
		/// <returns>A list containing the results obtained from the database.</returns>
		public static List<object> ToList( this IKMapEnumerable cmd )
		{
			if( cmd == null ) throw new ArgumentNullException( "cmd", "IKMapEnumerable cannot be null." );

			List<object> list = new List<object>();
			var reader = cmd.GetEnumerator(); while( reader.MoveNext() ) list.Add( reader.Current );

			reader.Dispose();
			return list;
		}

		/// <summary>
		/// Executes the command and returns an array, potentially empty, containing the records returned from the database.
		/// </summary>
		/// <param name="command">This command.</param>
		/// <returns>An array containing the results obtained from the database.</returns>
		public static object[] ToArray( this IKMapEnumerable cmd )
		{
			return cmd.ToList().ToArray();
		}

		/// <summary>
		/// Executes the command and returns the first record returned from the database. It can be null if no records
		/// are produced as the result of the execution of the command.
		/// </summary>
		/// <param name="command">This command.</param>
		/// <returns>The first result obtained from the database, or null.</returns>
		public static object First( this IKMapEnumerable cmd )
		{
			if( cmd == null ) throw new ArgumentNullException( "cmd", "IKMapEnumerable cannot be null." );

			object r = null;
			var reader = cmd.GetEnumerator(); if( reader.MoveNext() ) r = reader.Current;

			reader.Dispose();
			return r;
		}

		/// <summary>
		/// Executes the command and returns the last record returned from the database. It can be null if no records
		/// are produced as the result of the execution of the command.
		/// <para>Not that the current implementation of this method retrieves all possible records discarding them until
		/// the last one is found.</para>
		/// </summary>
		/// <param name="command">This command.</param>
		/// <returns>The last result obtained from the database, or null.</returns>
		public static object Last( this IKMapEnumerable cmd )
		{
			if( cmd == null ) throw new ArgumentNullException( "cmd", "IKMapEnumerable cannot be null." );

			object r = null;
			var reader = cmd.GetEnumerator(); while( reader.MoveNext() ) r = reader.Current;

			reader.Dispose();
			return r;
		}

		/// <summary>
		/// Executes the command, skips the first "skip" records, and then return as many as "take" records.
		/// <para>This enumeration can be empty if no records are produced by the command's execution, or if there are not
		/// enough records to fulfill the conditions given.</para>
		/// <para>The implementation of this method retrieves and discards all the "skip" records, and then gets the "take"
		/// ones.</para>
		/// </summary>
		/// <param name="command">This command.</param>
		/// <param name="skip">The number of records to discard.</param>
		/// <param name="take">The max number of records to take.</param>
		/// <returns>An enumeration containing the desired records.</returns>
		public static IEnumerable<object> SkipTake( this IKMapEnumerable cmd, int skip, int take )
		{
			if( cmd == null ) throw new ArgumentNullException( "cmd", "IKMapEnumerable cannot be null." );
			if( skip < 0 ) throw new ArgumentException( "Skip should be equal or bigger than cero." );
			if( take <= 0 ) throw new ArgumentException( "Take should be bigger than cero." );

			var reader = cmd.GetEnumerator();
			try {
				for( int i = 0; i < skip; i++ ) if( !reader.MoveNext() ) yield break;
				for( int i = 0; i < take; i++ ) {
					if( reader.MoveNext() ) yield return reader.Current;
					else yield break;
				}
			}
			finally { reader.Dispose(); }
		}
	}

	// -----------------------------------------------------
	/// <summary>
	/// Represents a typped mapped enumerator.
	/// </summary>
	public interface IKMapEnumerator<T> : IKMapEnumerator, IEnumerator<T> where T : class
	{
	}

	/// <summary>
	/// Represents a typped mapped command, able to be enumerated to produce instances of the business class.
	/// </summary>
	public interface IKMapEnumerable<T> : IKMapEnumerable, IEnumerable<T> where T : class
	{
		new IKMapEnumerator<T> GetEnumerator();
	}
	public static partial class IKMapEnumerableHelper
	{
		/// <summary>
		/// Executes the command and returns a list, potentially empty, containing the records returned from the database.
		/// </summary>
		/// <param name="command">This command.</param>
		/// <returns>A list containing the results obtained from the database.</returns>
		public static List<T> ToList<T>( this IKMapEnumerable<T> cmd ) where T : class
		{
			if( cmd == null ) throw new ArgumentNullException( "cmd", "IKMapEnumerable cannot be null." );

			List<T> list = new List<T>();
			var reader = cmd.GetEnumerator(); while( reader.MoveNext() ) list.Add( reader.Current );

			reader.Dispose();
			return list;
		}

		/// <summary>
		/// Executes the command and returns an array, potentially empty, containing the records returned from the database.
		/// </summary>
		/// <param name="command">This command.</param>
		/// <returns>An array containing the results obtained from the database.</returns>
		public static T[] ToArray<T>( this IKMapEnumerable<T> cmd ) where T : class
		{
			return cmd.ToList<T>().ToArray();
		}

		/// <summary>
		/// Executes the command and returns the first record returned from the database. It can be null if no records
		/// are produced as the result of the execution of the command.
		/// </summary>
		/// <param name="command">This command.</param>
		/// <returns>The first result obtained from the database, or null.</returns>
		public static T First<T>( this IKMapEnumerable<T> cmd ) where T : class
		{
			if( cmd == null ) throw new ArgumentNullException( "cmd", "IKMapEnumerable cannot be null." );

			T r = null;
			var reader = cmd.GetEnumerator(); if( reader.MoveNext() ) r = reader.Current;

			reader.Dispose();
			return r;
		}

		/// <summary>
		/// Executes the command and returns the last record returned from the database. It can be null if no records
		/// are produced as the result of the execution of the command.
		/// <para>Not that the current implementation of this method retrieves all possible records discarding them until
		/// the last one is found.</para>
		/// </summary>
		/// <param name="command">This command.</param>
		/// <returns>The last result obtained from the database, or null.</returns>
		public static T Last<T>( this IKMapEnumerable<T> cmd) where T : class
		{
			if( cmd == null ) throw new ArgumentNullException( "cmd", "IKMapEnumerable cannot be null." );

			T r = null;
			var reader = cmd.GetEnumerator(); while( reader.MoveNext() ) r = reader.Current;

			reader.Dispose();
			return r;
		}

		/// <summary>
		/// Executes the command, skips the first "skip" records, and then return as many as "take" records.
		/// <para>This enumeration can be empty if no records are produced by the command's execution, or if there are not
		/// enough records to fulfill the conditions given.</para>
		/// <para>The implementation of this method retrieves and discards all the "skip" records, and then gets the "take"
		/// ones.</para>
		/// </summary>
		/// <param name="command">This command.</param>
		/// <param name="skip">The number of records to discard.</param>
		/// <param name="take">The max number of records to take.</param>
		/// <returns>An enumeration containing the desired records.</returns>
		public static IEnumerable<T> SkipTake<T>( this IKMapEnumerable<T> cmd, int skip, int take ) where T : class
		{
			if( cmd == null ) throw new ArgumentNullException( "cmd", "IKMapEnumerable cannot be null." );
			if( skip < 0 ) throw new ArgumentException( "Skip should be equal or bigger than cero." );
			if( take <= 0 ) throw new ArgumentException( "Take should be bigger than cero." );

			var reader = cmd.GetEnumerator();
			try {
				for( int i = 0; i < skip; i++ ) if( !reader.MoveNext() ) yield break;
				for( int i = 0; i < take; i++ ) {
					if( reader.MoveNext() ) yield return reader.Current;
					else yield break;
				}
			}
			finally { reader.Dispose(); }
		}
	}
}
// ========================================================
