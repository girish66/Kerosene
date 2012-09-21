// ========================================================
#undef DEBUG
namespace MB.KeroseneORM
{
	using global::System.Diagnostics;
	using global::System;
	using global::System.Collections;
	using global::MB.Tools;

	// =====================================================
	public static class KCommandRawHelper
	{
		/// <summary>
		/// Creates a new empty instance of <see cref="KCommandRaw"/> using a fluent syntax.
		/// </summary>
		/// <param name="link">The link the command will be associated to.</param>
		/// <returns>The new created command.</returns>
		public static KCommandRaw Raw( this IKLink link )
		{
			var cmd = new KCommandRaw( link );
			return cmd;
		}

		/// <summary>
		/// Creates a new empty instance of <see cref="KCommandRaw"/> using a fluent syntax, and sets its contents using
		/// the text and parameters given as arguments.
		/// <para>The optional list of arguments contains the values of the parameters used in the text. They are identified
		/// using the standard string formatting sequence "{n}": the parameter is then created using the value found at
		/// the n-th position, and the sequence is substituted with its actual name. Note that the sequence should always
		/// start with {0}.</para>
		/// </summary>
		/// <param name="link">The link the command will be associated to.</param>
		/// <param name="text">The text to set in this command.</param>
		/// <param name="args">The values of the parameters used in this command.</param>
		/// <returns>The new created command.</returns>
		public static KCommandRaw Raw( this IKLink link, string text, params object[] args )
		{
			var cmd = link.Raw().Set( text, args );
			return cmd;
		}
	}

	// =====================================================
	/// <summary>
	/// Represents a Raw command in which its contents can be specified by using raw strings.
	/// <para>They are also used when invoking stored procedures.</para>
	/// </summary>
	public class KCommandRaw : KCommand, IKCommand, IKCommandEnumerable, IKCommandExecutable
	{
		protected string _Text = null;

		/// <summary>
		/// Creates a new instance of <see cref="KCommandRaw"/> associated with the link given.
		/// </summary>
		/// <param name="link">The link the command will be associated to.</param>
		public KCommandRaw( IKLink link ) : base( link )
		{
			DEBUG.IndentLine( "\n-- KCommandRaw( Link )" );
			DEBUG.Unindent();
		}
		protected override void Dispose( bool disposing )
		{
			DEBUG.IndentLine( "\n-- KCommandRaw.Dispose( Disposing={0} ) - This={1}", disposing, this );
			_Text = null;
			base.Dispose( disposing );
			DEBUG.Unindent();
		}

		/// <summary>
		/// Clones this command.
		/// </summary>
		/// <returns></returns>
		public KCommandRaw Clone()
		{
			var cloned = new KCommandRaw( this.Link ); OnClone( cloned );
			cloned._Text = this._Text;
			return cloned;
		}
		object ICloneable.Clone()
		{
			return this.Clone();
		}

		/// <summary>
		/// Creates a new enumerator associated with this command.
		/// </summary>
		/// <returns>The new created enumerator.</returns>
		public IKEnumerator GetEnumerator()
		{
			return Link.CreateEnumerator( this );
		}
		IEnumerator IEnumerable.GetEnumerator()
		{
			return this.GetEnumerator();
		}

		/// <summary>
		/// Creates a new executor associated with this command.
		/// </summary>
		/// <returns>The new created executor.</returns>
		public IKExecutor GetExecutor()
		{
			return Link.CreateExecutor( this );
		}

		// --------------------------------------------------
		/// <summary>
		/// Returns the command's text as it has been specified by the Set() and Append() methods.
		/// </summary>
		/// <param name="iterable">No use.</param>
		/// <returns>Returns the command's text as it has been specified.</returns>
		public override string CommandText( bool iterable )
		{
			return _Text;
		}

		/// <summary>
		/// Sets the contents of the command with the text and arguments obtained from parsing the dynamic lambda expression
		/// given. Whatever text and arguments the command may had before are discarded.
		/// </summary>
		/// <param name="spec">The dynamic lambda expression.</param>
		/// <returns>This command, to allow it to permit it to be used in a fluent syntax fashion.</returns>
		public KCommandRaw Set( Func<dynamic, object> spec )
		{
			if( spec == null ) throw new ArgumentNullException( "spec", "Specification to set cannot be null." );
			_Text = null; Parameters.Clear();

			_Text = Parser.Parse( spec, pars: Parameters );
			return this;
		}

		/// <summary>
		/// Sets the contents of the command with the text and optional argument specified. Whatever text and argument the
		/// command may had before are discarded.
		/// <para>The optional list of arguments contains the values of the parameters used in the text. They are identified
		/// using the standard string formatting sequence "{n}": the parameter is then created using the value found at
		/// the n-th position, and the sequence is substituted with its actual name. Note that the sequence should always
		/// start with {0}.</para>
		/// </summary>
		/// <param name="text">The text to set in this command.</param>
		/// <param name="args">The values of the parameters used in this command.</param>
		/// <returns>This command, to allow it to permit it to be used in a fluent syntax fashion.</returns>
		public KCommandRaw Set( string text, params object[] args )
		{
			_Text = null; Parameters.Clear();
			return Append( text, args );
		}

		/// <summary>
		/// Appends to the command the text and arguments obtained by parsing the dynamic lambda expression given.
		/// </summary>
		/// <param name="spec">The dynamic lambda expression.</param>
		/// <returns>This command, to allow it to permit it to be used in a fluent syntax fashion.</returns>
		public KCommandRaw Append( Func<dynamic, object> spec )
		{
			if( spec == null ) throw new ArgumentNullException( "spec", "Specification to append cannot be null." );
			string text = Parser.Parse( spec, pars: Parameters );

			bool space = text.StartsWith( " " ) ? true : false;
			_Text = _Text == null ? text : string.Format( "{0}{1}{2}", _Text, !space ? " " : "", text );
			return this;
		}

		/// <summary>
		/// Appends to the command the text and optional arguments specified.
		/// <para>The optional list of arguments contains the values of the parameters used in the text. They are identified
		/// using the standard string formatting sequence "{n}": the parameter is then created using the value found at
		/// the n-th position, and the sequence is substituted with its actual name. Note that the sequence should always
		/// start with {0}.</para>
		/// </summary>
		/// <param name="text">The text to set in this command.</param>
		/// <param name="args">The values of the parameters used in this command.</param>
		/// <returns>This command, to allow it to permit it to be used in a fluent syntax fashion.</returns>
		public KCommandRaw Append( string text, params object[] args )
		{
			// The arguments are specified by embedded "{n}" starting from cero...
			if( args != null ) {

				// Need a temporary store as the arguments start from cero, and there might be collisions with the ones that already exist...
				KParameterList temp = Link.Factory.CreateParameterList();
				foreach( var arg in args ) {
					if( arg is KParameter ) throw new ArgumentException( "Please use the values to parametrize, instead of Parameter instances." );
					if( arg == null ) temp.Insert( null );
					else Parser.Parse( arg, pars: temp, nulls: true );
				}

				// Adding to the real list of parameters, and replacing the indexed placeholder with the proper name...
				for( int n = 0; n < temp.Count; n++ ) {
					KParameter par = Parameters.Insert( temp[n].Value );
					string format = string.Format( "{{{0}}}", n );
					text = text.Replace( format, par.Name );
				}
				temp.Dispose();
			}

			bool space = text.StartsWith( " " ) ? true : false;
			_Text = _Text == null ? text : string.Format( "{0}{1}{2}", _Text, !space ? " " : "", text );
			return this;
		}
	}
}
// ========================================================
