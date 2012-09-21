// ========================================================
namespace MB.Tools
{
	using global::System;
	using global::System.IO;
	using global::System.Runtime.Serialization;
	using global::System.Runtime.Serialization.Formatters.Binary;
	using global::System.Runtime.Serialization.Formatters.Soap;

	// =====================================================
	/// <summary>
	/// Utilities for serialization scenarios.
	/// </summary>
	public static class SerializationHelper
	{
		public static void Serialize( this Stream sm, object obj, IFormatter formatter )
		{
			if( formatter == null ) throw new ArgumentNullException( "formatter", "IFormatter cannot be null." );
			formatter.Serialize( sm, obj );
		}
		public static object Deserialize( this Stream sm, IFormatter formatter )
		{
			if( formatter == null ) throw new ArgumentNullException( "formatter", "IFormatter cannot be null." );
			object obj = formatter.Deserialize( sm );
			return obj;
		}

		public static void Serialize( this Stream sm, object obj, bool binary )
		{
			IFormatter formatter = binary ? (IFormatter)( new BinaryFormatter() ) : (IFormatter)( new SoapFormatter() );
			Serialize( sm, obj, formatter );
		}
		public static object Deserialize( this Stream sm, bool binary )
		{
			IFormatter formatter = binary ? (IFormatter)( new BinaryFormatter() ) : (IFormatter)( new SoapFormatter() );
			return Deserialize( sm, formatter );
		}

		public static void Serialize( string path, object obj, bool binary )
		{
			if( string.IsNullOrEmpty( path ) ) throw new ArgumentException( "Path is invalid: " + path );
			using( FileStream sm = new FileStream( path, FileMode.Create ) ) {
				SerializationHelper.Serialize( sm, obj, binary );
			}
		}
		public static object Deserialize( string path, bool binary )
		{
			if( string.IsNullOrEmpty( path ) ) throw new ArgumentException( "Path is invalid: " + path );
			using( FileStream sm = new FileStream( path, FileMode.Open ) ) {
				return SerializationHelper.Deserialize( sm, binary );
			}
		}
	}
}
// ========================================================
