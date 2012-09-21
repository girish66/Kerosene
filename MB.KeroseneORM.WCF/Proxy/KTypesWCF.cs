// ========================================================
#undef DEBUG
namespace MB.KeroseneORM.WCF
{
	using global::System.Diagnostics;
	using global::System;
	using global::System.Reflection;
	using global::System.Collections.Generic;
	using global::MB.Tools;
	using global::MB.Tools.Dynamics;

	// =====================================================
	/// <summary>
	/// Manages the types to register with the WCF serialization engine.
	/// </summary>
	public static class KTypesWCF
	{
		static List<Type> _Types = new List<Type>();
		static bool _Initialized = false;

		/// <summary>
		/// Adds the basis Kerosene types to the list of types to register with the WCF serialization engine
		/// </summary>
		public static void RegisterInitialTypes()
		{
			DEBUG.IndentLine( "\n-- KTypesWCF.RegisterInitialTypes()..." );

			AddType( typeof( DeepObject ) );
			AddType( typeof( KParameter ) ); AddType( typeof( KParameterList ) );
			AddType( typeof( KSchema ) ); AddType( typeof( KMetaColumn ) );
			AddType( typeof( KRecord ) );
			AddType( typeof( KTableAlias ) ); AddType( typeof( KTableAliasList ) );

			DEBUG.Unindent();
		}

		/// <summary>
		/// Adds the given type to be list of types to register with the WCF serialization engine
		/// </summary>
		/// <param name="type">The type to add to the list.</param>
		public static void AddType( Type type )
		{
			if( type == null ) throw new ArgumentNullException( "Type" );
			lock( _Types ) { if( !_Types.Contains( type ) ) _Types.Add( type ); }
		}

		/// <summary>
		/// Removes the given type from the list of types to be registered with the WCF serialization engine
		/// </summary>
		/// <param name="type">The type to remove from the list.</param>
		public static void RemoveType( Type type )
		{
			if( type == null ) throw new ArgumentNullException( "Type" );
			lock( _Types ) { if( _Types.Contains( type ) ) _Types.Remove( type ); }
		}
		
		/// <summary>
		/// Clears the list of types to register with the WCF serialization engine.
		/// </summary>
		public static void ClearTypes()
		{
			lock( _Types ) { _Types.Clear(); }
		}

		public static IEnumerable<Type> GetKnownTypes()
		{
			DEBUG.IndentLine( "\n-- KTypesWCF.GetKnownTypes()..." );
			if( !_Initialized ) { RegisterInitialTypes(); _Initialized = true; }
			DEBUG.Unindent(); return _Types;
		}
		public static IEnumerable<Type> GetKnownTypes( ICustomAttributeProvider provider )
		{
			DEBUG.IndentLine( "\n-- KTypesWCF.GetKnownTypes( provider )..." );
			var r = GetKnownTypes();
			DEBUG.Unindent(); return r;
		}
	}
}
// ========================================================
