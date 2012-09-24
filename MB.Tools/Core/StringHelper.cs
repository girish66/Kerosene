// ========================================================
namespace MB.Tools
{
	using global::System;
	using global::System.Text;

	// =====================================================
	public static class StringHelper
	{
		/// <summary>
		/// Returns a new string containing the n left-most characters from the source string.
		/// </summary>
		/// <param name="source">The source string.</param>
		/// <param name="n">The number of left-most characters.</param>
		/// <returns>A new string containing the n left-most characters.</returns>
		public static string Left( this string source, int n )
		{
			if( source == null ) throw new ArgumentNullException( "source", "Souce string cannot be null." );
			if( n < 0 ) throw new ArgumentException( string.Format( "Number of characters cannot be less than cero: {0}." + n ) );

			if( n == 0 ) return string.Empty;
			string s = ( source.Length != 0 && n < source.Length ) ? source.Substring( 0, n ) : source;
			return s;
		}

		/// <summary>
		/// Returns a new string containing the n right-most characters from the source string.
		/// </summary>
		/// <param name="source">The source string.</param>
		/// <param name="n">The number of right-most characters.</param>
		/// <returns>A new string containing the n right-most characters.</returns>
		public static string Right( this string source, int n )
		{
			if( source == null ) throw new ArgumentNullException( "source", "Souce string cannot be null." );
			if( n < 0 ) throw new ArgumentException( string.Format( "Number of characters cannot be less than cero: {0}." + n ) );

			if( n == 0 ) return string.Empty;
			string s = ( source.Length != 0 && n < source.Length ) ? source.Substring( source.Length - n ) : source;
			return s;
		}

		/// <summary>
		/// Returns true if the target string contains any of the characters given.
		/// </summary>
		/// <param name="target">The target string.</param>
		/// <param name="array">The array of characters to validate.</param>
		/// <returns>True if the target string contains any of the characters given.</returns>
		public static bool ContainsAny( this string target, char[] array )
		{
			if( target == null ) throw new ArgumentNullException( "target", "Target string cannot be null." );
			if( array == null || array.Length == 0 ) return false; // No characters to validate

			int ix = target.IndexOfAny( array );
			return ix >= 0 ? true : false;
		}

		/// <summary>
		/// Returns a new validated string built from the source string using the rules given, or throws an exception is any
		/// error is found.
		/// </summary>
		/// <param name="source">The source string.</param>
		/// <param name="desc">A description of the source string used to build the exception messages.</param>
		/// <param name="canbeNull">If the returned string can be null.</param>
		/// <param name="trim">True to trim the string before returning.</param>
		/// <param name="trimStart">True to trim for the start of the string.</param>
		/// <param name="trimEnd">True to trim from the end of the string.</param>
		/// <param name="minLen">The min valid lenght of the returned string.</param>
		/// <param name="maxLen">The max valid lenght of the returned string.</param>
		/// <param name="padLeft">The character to use to left pad the returned string.</param>
		/// <param name="padRight">The character to use to right pad the returned string.</param>
		/// <param name="canbeEmpty">If the returned string can be empty.</param>
		/// <param name="invalidChars">An array containing the invalid characters.</param>
		/// <param name="validChars">An array containing the only valid characters.</param>
		/// <returns>A new validated string.</returns>
		public static string Validated( this string source, string desc = null,
			bool canbeNull = false, bool trim = true, bool trimStart = false, bool trimEnd = false,
			int minLen = -1, int maxLen = -1, char padLeft = '\0', char padRight = '\0', bool canbeEmpty = false,
			char[] invalidChars = null, char[] validChars = null )
		{
			// Assuring a valid descriptor...
			if( string.IsNullOrWhiteSpace( desc ) ) desc = "Source";

			// Validating if null sources are accepted...
			if( source == null ) {
				if( !canbeNull ) throw new ArgumentNullException( "source", string.Format( "{0} cannot be null.", desc ) );
				return null;
			}

			// Trimming if needed...
			if( trim && !( trimStart || trimEnd ) ) source = source.Trim();
			else {
				if( trimStart ) source = source.TrimStart( ' ' );
				if( trimEnd ) source = source.TrimEnd( ' ' );
			}

			// Adjusting lenght...
			if( minLen > 0 ) {
				if( padLeft != '\0' ) source = source.PadLeft( minLen, padLeft );
				if( padRight != '\0' ) source = source.PadRight( minLen, padRight );
			}
			if( maxLen > 0 ) {
				if( padLeft != '\0' ) source = source.PadLeft( maxLen, padLeft );
				if( padRight != '\0' ) source = source.PadRight( maxLen, padRight );
			}

			// Validating emptyness and lenghts...
			if( source.Length == 0 ) {
				if( !canbeEmpty ) throw new ArgumentException( desc + " cannot be empty." );
				return string.Empty;
			}
			if( minLen >= 0 && source.Length < minLen ) throw new ArgumentException( string.Format( "{0} '{1}' lenghth is lower than: {2}.", desc, source, minLen ) );
			if( maxLen >= 0 && source.Length > maxLen ) throw new ArgumentException( string.Format( "{0} '{1}' lenghth is bigger than: {2}.", desc, source, maxLen ) );

			// Checking invalid chars...
			if( invalidChars != null ) {
				int n = source.IndexOfAny( invalidChars );
				if( n >= 0 ) throw new ArgumentException( string.Format( "Invalid character '{0}' found in {1}: {2}", source[n], desc, source ) );
			}

			// Checking valid chars...
			if( validChars != null ) {
				int n = validChars.ToString().IndexOfAny( source.ToCharArray() );
				if( n >= 0 ) throw new ArgumentException( string.Format( "Invalid character '{0}' found in {1}: {2}", validChars.ToString()[n], desc, source ) );
			}

			return source;
		}
	}
}
// ========================================================
