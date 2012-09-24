// ========================================================
namespace MB.Tools
{
	using global::System.Diagnostics;
	using global::System;
	using global::System.Text;
	using global::System.Reflection;

	// =====================================================
	/// <summary>
	/// Utilities for debug scenarios.
	/// </summary>
	public static class DebugHelper
	{
		/// <summary>
		/// Returns the <see cref="MethodBase"/> object identiying the caller of the current method.
		/// </summary>
		/// <returns>The MethodBase object of the caller of the current method.</returns>
		public static MethodBase CallingMethod()
		{
			var stack = new StackTrace();
			var frame = stack == null ? null : stack.GetFrame( 2 );
			var method = frame == null ? null : frame.GetMethod();

			return method;
		}

		/// <summary>
		/// Returns a string in the form "Type.Method" identifying the caller of the current method.
		/// </summary>
		/// <returns>A string identifying the caller of the current method.</returns>
		public static string CalledFrom()
		{
			var stack = new StackTrace();
			var frame = stack == null ? null : stack.GetFrame( 2 );
			var method = frame == null ? null : frame.GetMethod();

			if( method == null ) return "n/a";
			return string.Format( "{0}.{1}", method.DeclaringType.Name, method.Name );
		}
	}

	// =====================================================
	/// <summary>
	/// Provides an alternate implementation of the <see cref="System.Diagnostics.Debug"/> class that facilitates its usage
	/// by providing methods that accept variable arguments, and other facilities.
	/// </summary>
	public static class DEBUG
	{
		[Conditional( "DEBUG" )]
		public static void Write( string message )
		{
			if( message == null ) return;
			if( message.Length == 0 ) return;
			char[] array = new char[message.Length]; Array.Clear( array, 0, message.Length );

			int a = 0; for( int m = 0; m < message.Length; m++ ) {
				switch( message[m] ) {
					case '\n':
						if( a != 0 ) { Debug.WriteLine( new string( array ) ); Array.Clear( array, 0, message.Length ); a = 0; }
						else Debug.WriteLine( "" );
						break;
					case '\t':
						if( a != 0 ) { Debug.WriteLine( new string( array ) ); Array.Clear( array, 0, message.Length ); a = 0; }
						Debug.Indent();
						break;
					case '\r':
						if( a != 0 ) { Debug.WriteLine( new string( array ) ); Array.Clear( array, 0, message.Length ); a = 0; }
						Debug.Unindent();
						break;
					default:
						array[a] = message[m]; a++;
						break;
				}
			}
			if( a != 0 ) Debug.Write( new string( array ) );
		}

		[Conditional( "DEBUG" )]
		public static void Write( string format, params object[] args )
		{
			string message = string.Format( format, args );
			Write( message );
		}

		[Conditional( "DEBUG" )]
		public static void WriteLine()
		{
			Debug.WriteLine( "" );
		}

		[Conditional( "DEBUG" )]
		public static void WriteLine( string message )
		{
			Write( message );
			WriteLine();
		}

		[Conditional( "DEBUG" )]
		public static void WriteLine( string format, params object[] args )
		{
			Write( format, args );
			WriteLine();
		}

		[Conditional( "DEBUG" )]
		public static void Indent()
		{
			Debug.Indent();
		}

		[Conditional( "DEBUG" )]
		public static void Indent( string message )
		{
			Write( message );
			Indent();
		}

		[Conditional( "DEBUG" )]
		public static void Indent( string format, params object[] args )
		{
			Write( format, args );
			Indent();
		}

		[Conditional( "DEBUG" )]
		public static void IndentLine()
		{
			Indent();
			WriteLine();
		}

		[Conditional( "DEBUG" )]
		public static void IndentLine( string message )
		{
			Indent();
			WriteLine( message );
		}

		[Conditional( "DEBUG" )]
		public static void IndentLine( string format, params object[] args )
		{
			Indent();
			WriteLine( format, args );
		}

		[Conditional( "DEBUG" )]
		public static void Unindent()
		{
			Debug.Unindent();
		}

		[Conditional( "DEBUG" )]
		public static void Unindent( string message )
		{
			Write( message );
			Unindent();
		}

		[Conditional( "DEBUG" )]
		public static void Unindent( string format, params object[] args )
		{
			Write( format, args );
			Unindent();
		}

		[Conditional( "DEBUG" )]
		public static void UnindentLine()
		{
			WriteLine();
			Unindent();
		}

		[Conditional( "DEBUG" )]
		public static void UnindentLine( string message )
		{
			WriteLine( message );
			Unindent();
		}

		[Conditional( "DEBUG" )]
		public static void UnindentLine( string format, params object[] args )
		{
			WriteLine( format, args );
			Unindent();
		}

		[Conditional( "DEBUG" )]
		public static void PrintException( Exception e )
		{
			while( e != null ) {
				Debug.WriteLine( "\n-----" );
				Debug.WriteLine( string.Format( "\nException: {0}", e.Message ) );
				Debug.WriteLine( string.Format( "\nStack Trace: \n{0}", e.StackTrace ) );
				e = e.InnerException;
			}
			Debug.WriteLine( "\n-----" );
		}
	}
}
// ========================================================
