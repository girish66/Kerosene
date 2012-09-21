// ========================================================
#undef DEBUG
namespace MB.KeroseneORM
{
	using global::System.Diagnostics;
	using global::System;
	using global::System.Collections;
	using global::System.Collections.Generic;
	using global::System.Text;
	using global::MB.Tools;

	// =====================================================
	/// <summary>
	/// Represents a given command to be executed against a database.
	/// </summary>
	public interface IKCommand : IDisposable, ICloneable
	{
		/// <summary>
		/// Gets the link this command is associated to.
		/// </summary>
		IKLink Link { get; }

		/// <summary>
		/// Gets the <see cref="KParameterList"/> instance used to store the parameters of this command.
		/// </summary>
		KParameterList Parameters { get; }

		/// <summary>
		/// Generates the actual SQL code to execute, either the executable version or the iterable one based upon the value
		/// of its "iterable" argument.
		/// </summary>
		/// <param name="iterable">Whether to generate the iterable version of the command or the executable one.</param>
		/// <returns>The actual SQL text to execute against the database.</returns>
		string CommandText( bool iterable );

		/// <summary>
		/// Gets the parser created to parse the expressions used to build this command.
		/// </summary>
		KParser Parser { get; }
	}
	public static class IKCommandHelper
	{
		/// <summary>
		/// Gets a trace string for this command, built by concatenating to its SQL code a string describing the parameters
		/// to use.
		/// </summary>
		/// <param name="command">This command.</param>
		/// <param name="iterable">Whether to generate the iterable version of the command or the executable one.</param>
		/// <returns>The trace string.</returns>
		public static string TraceString( this IKCommand command, bool iterable = false )
		{
			if( command == null ) throw new ArgumentNullException( "command", "Command cannot be null." );

			StringBuilder sb = new StringBuilder();
			sb.Append( command.CommandText( iterable ) );

			var pars = command.Parameters; if( pars != null && pars.Count != 0 ) sb.AppendFormat( "-- {0}", pars );
			pars = null;
			return sb.ToString();
		}
	}

	// =====================================================
	/// <summary>
	/// Represents an object, typically a command, that is associated with a given table.
	/// </summary>
	public interface IKTableNameProvider
	{
		/// <summary>
		/// Gets the name of the table this object is associated to.
		/// </summary>
		string TableName { get; }
	}

	// =====================================================
	/// <summary>
	/// Represents an object that carries a <see cref="KTableAliasList"/> object.
	/// </summary>
	public interface IKTableAliasListProvider
	{
		/// <summary>
		/// Gets the <see cref="KTableAliasList"/> instance this object carries.
		/// </summary>
		KTableAliasList TableAliasList { get; }
	}

	// =====================================================
	/// <summary>
	/// Represents an object able to enumerate through the results of a command.
	/// </summary>
	public interface IKEnumerator : IEnumerator, IEnumerable, IDisposable
	{
		/// <summary>
		/// The command this enumerator is associated with.
		/// </summary>
		IKCommandEnumerable Command { get; }

		/// <summary>
		/// The converter to invoke to convert the results obtained from the database to whatever object the converter wants
		/// to produce.
		/// <para>This converter will receive the <see cref="KRecord"/> instance as thet are returned from the database, and
		/// can manipulate them the way it wishes to return any arbitrary object of any type (as far as it is a class instance
		/// and not a struct or a value object).</para>
		/// </summary>
		Func<KRecord, object> Converter { get; set; }

		/// <summary>
		/// Gets the current <see cref="KRecord"/> instance.
		/// </summary>
		KRecord CurrentRecord { get; }

		/// <summary>
		/// Gets the schema describing the columns and contents of the records returned.
		/// </summary>
		KSchema Schema { get; }

		/// <summary>
		/// Gets or sets whether to seal the records as soon as they are obtained from the database.
		/// </summary>
		bool SealRecords { get; set; }
	}
	public static class IKEnumeratorHelper
	{
		/// <summary>
		/// Permits the specification of a converter in a fluent syntax fashion.
		/// </summary>
		/// <param name="reader">This enumerator.</param>
		/// <param name="converter">The converter to set into this enumerator.</param>
		/// <returns>This enumerator, to be used in a fluent syntax fashion.</returns>
		public static IKEnumerator ConvertBy( this IKEnumerator reader, Func<KRecord, object> converter )
		{
			if( reader == null ) throw new ArgumentNullException( "reader", "IKEnumerator cannot be null." );
			reader.Converter = converter;
			return reader;
		}

		/// <summary>
		/// Permits the specification of the SealRecords property of the enumerator in a fluent syntax fashion.
		/// </summary>
		/// <param name="reader">This enumerator.</param>
		/// <param name="sealRecords">Whether to seal the records obtained from the database or not.</param>
		/// <returns>This enumerator, to be used in a fluent syntax fashion.</returns>
		public static IKEnumerator WithRecordsSealed( this IKEnumerator reader, bool sealRecords )
		{
			if( reader == null ) throw new ArgumentNullException( "reader", "IKEnumerator cannot be null." );
			reader.SealRecords = sealRecords;
			return reader;
		}
	}

	// -----------------------------------------------------
	/// <summary>
	/// Represents a command able to iterate through the records returned from its execution.
	/// </summary>
	public interface IKCommandEnumerable : IEnumerable, IKCommand
	{
		/// <summary>
		/// Creates a new <see cref="IKEnumerator"/> instance to execute this command.
		/// </summary>
		/// <returns></returns>
		new IKEnumerator GetEnumerator();
	}
	public static class IKCommandEnumerableHelper
	{
		/// <summary>
		/// Creates a new reader and permits the specification of a converter in a fluent syntax fashion
		/// </summary>
		/// <param name="command">This command.</param>
		/// <param name="converter">The converter to set into this enumerator.</param>
		/// <returns>The enumerator created, to be used in a fluent syntax fashion.</returns>
		public static IKEnumerator ConvertBy( this IKCommandEnumerable command, Func<KRecord, object> converter )
		{
			if( command == null ) throw new ArgumentNullException( "command", "Command cannot be null." );

			var reader = command.GetEnumerator();
			reader.Converter = converter;
			return reader;
		}

		/// <summary>
		/// Creates a new enumerator and permits the specification of its SealRecords property in a fluent syntax fashion.
		/// </summary>
		/// <param name="command">This command.</param>
		/// <param name="sealRecords">Whether to seal the records obtained from the database or not.</param>
		/// <returns>The enumerator created, to be used in a fluent syntax fashion.</returns>
		public static IKEnumerator WithRecordsSealed( this IKCommandEnumerable command, bool sealRecords )
		{
			if( command == null ) throw new ArgumentNullException( "command", "Command cannot be null." );

			var reader = command.GetEnumerator();
			reader.SealRecords = sealRecords;
			return reader;
		}

		/// <summary>
		/// Executes the command and returns a list, potentially empty, containing the records returned from the database.
		/// </summary>
		/// <param name="command">This command.</param>
		/// <returns>A list containing the results obtained from the database.</returns>
		public static List<object> ToList( this IKCommandEnumerable command )
		{
			if( command == null ) throw new ArgumentNullException( "command", "Command cannot be null." );
			List<object> list = new List<object>();

			var reader = command.GetEnumerator();
			while( reader.MoveNext() ) list.Add( reader.Converter == null ? reader.Current : reader.Converter( reader.CurrentRecord ) );

			reader.Dispose();
			return list;
		}

		/// <summary>
		/// Executes the command and returns an array, potentially empty, containing the records returned from the database.
		/// </summary>
		/// <param name="command">This command.</param>
		/// <returns>An array containing the results obtained from the database.</returns>
		public static object[] ToArray( this IKCommandEnumerable command )
		{
			List<object> list = command.ToList();
			object[] array = list.ToArray(); list.Clear(); list = null;
			return array;
		}

		/// <summary>
		/// Executes the command and returns the first record returned from the database. It can be null if no records
		/// are produced as the result of the execution of the command.
		/// </summary>
		/// <param name="command">This command.</param>
		/// <returns>The first result obtained from the database, or null.</returns>
		public static object First( this IKCommandEnumerable command )
		{
			if( command == null ) throw new ArgumentNullException( "command", "Command cannot be null." );
			object r = null;

			var reader = command.GetEnumerator();
			if( reader.MoveNext() ) r = reader.Converter == null ? reader.Current : reader.Converter( reader.CurrentRecord );

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
		public static object Last( this IKCommandEnumerable command )
		{
			if( command == null ) throw new ArgumentNullException( "command", "Command cannot be null." );
			object r = null;

			var reader = command.GetEnumerator();
			while( reader.MoveNext() ) r = reader.Converter == null ? reader.Current : reader.Converter( reader.CurrentRecord );

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
		public static IEnumerable<object> SkipTake( this IKCommandEnumerable command, int skip, int take )
		{
			if( command == null ) throw new ArgumentNullException( "command", "Command cannot be null." );
			if( skip < 0 ) throw new ArgumentException( "Skip should be equal or bigger than cero." );
			if( take <= 0 ) throw new ArgumentException( "Take should be bigger than cero." );

			var reader = command.GetEnumerator(); try {
				for( int i = 0; i < skip; i++ ) if( !reader.MoveNext() ) yield break;
				for( int i = 0; i < take; i++ ) {
					if( reader.MoveNext() ) yield return ( reader.Converter == null ? reader.Current : reader.Converter( reader.CurrentRecord ) );
					else yield break;
				}
			}
			finally { reader.Dispose(); }
		}
	}

	// =====================================================
	/// <summary>
	/// Represents an object able to execute a command and able to return just the number of records affected.
	/// </summary>
	public interface IKExecutor : IDisposable
	{
		/// <summary>
		/// The command this executor is associated with.
		/// </summary>
		IKCommandExecutable Command { get; }

		/// <summary>
		/// Executes the command and returns the number of records affected.
		/// <para>Note that in many databases the Query/Select commands will return -1.</para>
		/// </summary>
		/// <returns>The number of records affected.</returns>
		int Execute();
	}

	// -----------------------------------------------------
	/// <summary>
	/// Represents a command able to return the number of records affected from its execution.
	/// </summary>
	public interface IKCommandExecutable : IKCommand
	{
		/// <summary>
		/// Creates a new executor associated with this command.
		/// </summary>
		/// <returns>The new executor created.</returns>
		IKExecutor GetExecutor();
	}
	public static class IKCommandExecutableHelper
	{
		/// <summary>
		/// Creates a new executor associated with this command, and then executes the command to get the number of records
		/// affected.
		/// </summary>
		/// <param name="command">This command.</param>
		/// <returns>The number of records affected.</returns>
		public static int Execute( this IKCommandExecutable command )
		{
			if( command == null ) throw new ArgumentNullException( "command", "Command cannot be null." );
			var executor = command.GetExecutor();
			int r = executor.Execute();

			executor.Dispose();
			return r;
		}
	}

	// =====================================================
	/// <summary>
	/// Provides a base implementation of the <see cref="IKCommand"/> interface.
	/// </summary>
	public abstract class KCommand : IKCommand
	{
		IKLink _Link = null;
		KParameterList _Parameters = null;
		KParser _Parser = null;

		/// <summary>
		/// Creates a new <see cref="KCommand"/> associated with the given link.
		/// </summary>
		/// <param name="link">The link this command is associated with.</param>
		protected KCommand( IKLink link )
		{
			DEBUG.IndentLine( "\n-- KCommand( Link={0} )", link == null ? "null" : link.ToString() );

			if( ( _Link = link ) == null ) throw new ArgumentNullException( "link", "Link cannot be null." );
			if( ( _Parameters = _Link.Factory.CreateParameterList() ) == null ) throw new InvalidOperationException( "Cannot create a List of Parameters." );
			if( ( _Parser = _Link.Factory.CreateParser() ) == null ) throw new InvalidOperationException( "Cannot create a Parser." );

			DEBUG.Unindent();
		}

		protected virtual void Dispose( bool disposing )
		{
			DEBUG.IndentLine( "\n-- KCommand.Dispose( Disposing={0} ) - This={1}", disposing, this );

			if( _Parameters != null ) { _Parameters.Dispose(); _Parameters = null; }
			if( _Parser != null ) { _Parser.Dispose(); _Parser = null; }
			_Link = null;

			DEBUG.Unindent();
		}
		public void Dispose()
		{
			DEBUG.IndentLine( "\n-- KCommand.Dispose() - This={0}", this );
			Dispose( true );
			GC.SuppressFinalize( this );
			DEBUG.Unindent();
		}
		~KCommand()
		{
			DEBUG.IndentLine( "\n-- KCommand~() - This={0}", this );
			Dispose( false );
			DEBUG.Unindent();
		}

		protected virtual void OnClone( KCommand cloned )
		{
			foreach( var par in this.Parameters ) cloned.Parameters.Add( par.Clone() );
		}
		object ICloneable.Clone() { throw new NotImplementedException( "Abstract KCommand.Clone() called." ); }

		/// <summary>
		/// Gets the link this command is associated to.
		/// </summary>
		public IKLink Link
		{
			get { return _Link; }
		}

		/// <summary>
		/// Gets the <see cref="KParameterList"/> instance used to store the parameters of this command.
		/// </summary>
		public KParameterList Parameters
		{
			get { return _Parameters; }
		}

		/// <summary>
		/// Gets the parser created to parse the expressions used to build this command.
		/// </summary>
		public KParser Parser
		{
			get { return _Parser; }
		}

		/// <summary>
		/// Generates the actual SQL code to execute, either the executable version or the iterable one based upon the value
		/// of its "iterable" argument.
		/// </summary>
		/// <param name="iterable">Whether to generate the iterable version of the command or the executable one.</param>
		/// <returns>The actual SQL text to execute against the database.</returns>
		public abstract string CommandText( bool iterable );

		public override string ToString()
		{
			return this.TraceString( iterable: false );
		}
	}
}
// ========================================================
