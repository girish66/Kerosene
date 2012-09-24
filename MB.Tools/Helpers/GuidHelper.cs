// ========================================================
namespace MB.Tools
{
	using global::System;

	// =====================================================
	/// <summary>
	/// Utilities to use with <see cref="System.Guid"/> instances.
	/// </summary>
	public static class GuidHelper
	{
		static ushort _DefaultTagLen = 6;

		/// <summary>
		/// Gets or sets the default lenght to use when generating Guid's tags.
		/// </summary>
		public static ushort DefaultTagLen
		{
			get { return _DefaultTagLen; }
			set
			{
				if( value > 0 ) _DefaultTagLen = value;
				else throw new ArgumentException( "Default tag lenght cannot be: " + value.ToString() );
			}
		}

		/// <summary>
		/// Generates a tag to identify, not uniquelly, a given Guid. A tag is a short string representation of the Guid, obtained
		/// from a number of its left-most characters of its string representation, useful for quick identification purposes. It is
		/// not unique as many Guids can have the same tag.
		/// </summary>
		/// <param name="uid">The Guid to obtain its tag from.</param>
		/// <param name="n">The number of left-most characters to use to build the tag. If cero, the default value is used.</param>
		/// <returns>The tag associated with the Guid given.</returns>
		public static string TagString( this Guid uid, ushort n = 0 )
		{
			if( uid == null ) throw new ArgumentNullException( "uid", "Guid to tag cannot be null." );
			if( n < 0 ) throw new ArgumentException( "Tag lenght cannot be: " + n.ToString() );
			if( n == 0 ) n = _DefaultTagLen;

			return uid.ToString().Right( n );
		}
	}
}
// ========================================================
