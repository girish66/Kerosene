// ========================================================
namespace MB.Tools
{
	using global::System;
	using global::System.Text;
	using global::System.Reflection;
	using global::System.Collections;
	using global::System.Linq.Expressions;

	// =====================================================
	/// <summary>
	/// Utilities to use with type objects.
	/// </summary>
	public static class TypeHelper
	{
		/// <summary>
		/// Provides an alternate ToString() method in which:
		/// - Null objects are translated into a normalized string,
		/// - If the class defining the object has its own ToString() method, then it is used,
		/// - Dictionaries are translated into a normalized "name = value" form per each element separated by commas,
		/// - Enumerable objects are translated into a concatenation of its elements separated by commas,
		/// - As a last resort, its public properties and fields are concatenated to produce the string representation.
		/// </summary>
		/// <param name="obj">The object to obtain its alternate string representation from.</param>
		/// <param name="brackets">A pair of characters being the opening and closing bracket to use, if needed.</param>
		/// <returns>The alternate string representation of the object given.</returns>
		public static string ToString( object obj, string brackets = null )
		{
			// Some default cases...
			if( obj == null ) return "<null>";
			if( obj is string ) return (string)obj;
			Type type = obj.GetType(); if( type.IsEnum ) return obj.ToString();

			// If the type overrides the ToString() method, use it...
			MethodInfo method = type.GetMethod( "ToString", Type.EmptyTypes );
			if( method.DeclaringType != typeof( object ) && !method.DeclaringType.Name.Contains( "Anonymous" ) )
				return obj.ToString();

			// All other cases...
			StringBuilder sb = new StringBuilder();
			bool first = true;

			if( brackets == null || brackets.Length < 2 ) brackets = "{}";

			// Dictionaries...
			if( obj is IDictionary ) {
				if( brackets == null || brackets.Length < 2 ) brackets = "{}";

				sb.AppendFormat( "{0}", brackets[0] ); first = true; foreach( DictionaryEntry kvp in ( (IDictionary)obj ) ) {
					if( !first ) sb.Append( ", " ); else { sb.Append( " " ); first = false; }
					sb.AppendFormat( "{0} = {1}", TypeHelper.ToString( kvp.Key ), TypeHelper.ToString( kvp.Value ) );
				}
				if( !first ) sb.Append( " " ); else sb.Append( "-" );
				sb.AppendFormat( "{0}", brackets[1] );
				return sb.ToString();
			}

			// IEnumerables...
			IEnumerator ator = null; if( obj is IEnumerable ) ator = ( (IEnumerable)obj ).GetEnumerator();
			else {
				method = type.GetMethod( "GetEnumerator", Type.EmptyTypes );
				if( method != null ) ator = (IEnumerator)method.Invoke( obj, null );
			}
			if( ator != null ) {
				sb.AppendFormat( "{0}", brackets[0] ); first = true; while( ator.MoveNext() ) {
					if( !first ) sb.Append( ", " ); else { sb.Append( " " ); first = false; }
					sb.Append( TypeHelper.ToString( ator.Current ) );
				}
				if( !first ) sb.Append( " " ); else sb.Append( "-" ); sb.AppendFormat( "{0}", brackets[1] );
				return sb.ToString();
			}

			// Last resort by using the public properties and fields...
			BindingFlags flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy;
			first = true;
			sb.AppendFormat( "{0}", brackets[0] );

			PropertyInfo[] props = type.GetProperties( flags ); if( props.Length != 0 ) foreach( var prop in props ) {
					if( !first ) sb.Append( ", " ); else { sb.Append( " " ); first = false; }
					// if( prop.Name == "Item" ) sb.Append( "[...]" ); else
					sb.AppendFormat( "{0} = {1}", prop.Name, TypeHelper.ToString( prop.GetValue( obj, null ) ) );
				}
			FieldInfo[] infos = type.GetFields( flags ); if( infos.Length != 0 ) foreach( var info in infos ) {
					if( !first ) sb.Append( ", " ); else { sb.Append( " " ); first = false; }
					// if( info.Name == "Item" ) sb.Append( "[...]" ); else
					sb.AppendFormat( "{0} = {1}", info.Name, TypeHelper.ToString( info.GetValue( obj ) ) );
				}

			if( !first ) sb.Append( " " ); else sb.Append( "-" );
			sb.AppendFormat( "{0}", brackets[1] );
			return sb.ToString();
		}

		// --------------------------------------------------
		const string _InvalidMultiPartNameChars = " +-*/^%[]{}()!\"\\&=?¿";
		const string _InvalidNameChars = "." + _InvalidMultiPartNameChars;

		public static readonly char[] InvalidNameChars = _InvalidNameChars.ToCharArray();
		public static readonly char[] InvalidMultiPartNameChars = _InvalidMultiPartNameChars.ToCharArray();

		// --------------------------------------------------
		public const BindingFlags PublicAndNonPublic = BindingFlags.Public | BindingFlags.NonPublic;
		public const BindingFlags AlsoInherited = BindingFlags.FlattenHierarchy;
		public const BindingFlags InstanceAndStatic = BindingFlags.Instance | BindingFlags.Static;
		public const BindingFlags DefaultBindings = PublicAndNonPublic | AlsoInherited | InstanceAndStatic;

		// --------------------------------------------------
		/// <summary>
		/// Returns if the given type is a nullable one.
		/// </summary>
		/// <param name="type">The type to verify.</param>
		/// <returns>True if the given type is a nullable one.</returns>
		public static bool IsNullableType( Type type )
		{
			if( type == null ) throw new ArgumentNullException( "type", "Type cannot be null." );

			Type generic = type.IsGenericType ? type.GetGenericTypeDefinition() : null;
			if( generic != null && generic.Equals( typeof( Nullable<> ) ) ) return true;

			if( type.IsClass ) return true;
			if( type.IsValueType ) return false;
			return false;
		}

		/// <summary>
		/// Returns if the given type is a nullable one.
		/// </summary>
		/// <typeparam name="T">The type to verify.</typeparam>
		/// <returns>True if the given type is a nullable one.</returns>
		public static bool IsNullableType<T>()
		{
			return IsNullableType( typeof( T ) );
		}

		// --------------------------------------------------
		static Delegate CreateConverterDelegate( Type sourceType, Type targetType )
		{
			// The following is an adaptation of code courtesy of Richard Deeming. Implements the delegate to invoke
			// when a generic conversion is needed. Improvement: it could be cached to improve performance.

			var input = Expression.Parameter( sourceType, "input" );

			Expression body; try {
				body = Expression.Convert( input, targetType );
			}
			catch( InvalidOperationException ) {
				var conversionType = Expression.Constant( targetType );
				body = Expression.Call( typeof( Convert ), "ChangeType", null, input, conversionType );
			}
			var result = Expression.Lambda( body, input );
			return result.Compile();
		}

		/// <summary>
		/// Converts the given object into an instance of the target type.
		/// </summary>
		/// <param name="obj">The source object to convert from.</param>
		/// <param name="targetType">The type where to convert the source objet to.</param>
		/// <returns>A converted instance.</returns>
		public static object ConvertTo( object obj, Type targetType )
		{
			if( targetType == null ) throw new ArgumentNullException( "targetType", "Target type cannot be null." );

			if( obj == null ) {
				if( TypeHelper.IsNullableType( targetType ) ) return null;
				throw new InvalidOperationException( string.Format( "Target type '{0}' does not accept null sources.", targetType.Name ) );
			}
			Delegate converter = CreateConverterDelegate( obj.GetType(), targetType );
			return converter.DynamicInvoke( obj );
		}

		/// <summary>
		/// Converts the given object into an instance of the target type.
		/// </summary>
		/// <typeparam name="T">The type where to convert the source objet to.</typeparam>
		/// <param name="obj">The source object to convert from.</param>
		/// <returns>A converted instance.</returns>
		public static T ConvertTo<T>( object obj )
		{
			return (T)ConvertTo( obj, typeof( T ) );
		}

		// --------------------------------------------------
		/// <summary>
		/// Returns whether the two given objects can be considered equivalent.
		/// </summary>
		/// <param name="source">The source object.</param>
		/// <param name="target">The target object</param>
		/// <returns>True if the two given objects can be considered equivalent, false otherwise.</returns>
		public static bool AreEquivalent( object source, object target )
		{
			if( source == null && target == null ) return true;
			if( source == null && target != null ) return false;
			if( source != null && target == null ) return false;

			Type sourceType = source.GetType();
			Type targetType = target.GetType();

			if( sourceType.IsAssignableFrom( targetType ) ) return source.Equals( target );
			if( targetType.IsAssignableFrom( sourceType ) ) return target.Equals( source );

			object value = null;
			try { value = ConvertTo( target, sourceType ); return source.Equals( value ); } catch { }
			try { value = ConvertTo( source, targetType ); return target.Equals( value ); } catch { }
			return false;
		}

		// --------------------------------------------------
		/// <summary>
		/// Tries to obtain a clone from the source object, either if it implements the <see cref="ICloneable"/> interface or
		/// if it has a parameterless Clone() method, and it is not possible, returns the original object.
		/// </summary>
		/// <param name="source">The object to clone.</param>
		/// <returns>A clone of the source object, or the original one if clonning is not possible.</returns>
		public static object TryClone( object source )
		{
			if( source == null ) return null;
			if( source is ICloneable ) return ( (ICloneable)source ).Clone();

			MethodInfo method = source.GetType().GetMethod( "Clone", Type.EmptyTypes );
			if( method == null ) return source;
			if( method.ReturnType == typeof( void ) ) return source;
			return method.Invoke( source, null );
		}

		/// <summary>
		/// Tries to obtain a clone from the source object, either if it implements the <see cref="ICloneable"/> interface or
		/// if it has a parameterless Clone() method, and it is not possible, returns the original object.
		/// </summary>
		/// <typeparam name="T">The type where to cast the cloned object.</typeparam>
		/// <param name="source">The object to clone.</param>
		/// <returns>A clone of the source object, or the original one if clonning is not possible.</returns>
		public static T TryClone<T>( T source )
		{
			return (T)TryClone( (object)source );
		}

		// --------------------------------------------------
		/// <summary>
		/// Returns the <see cref="MemberInfo"/> object corresponding to the element specified.
		/// Supported elements are properties and fields.
		/// </summary>
		/// <param name="type">The type where the element is defined.</param>
		/// <param name="name">The name of the element.</param>
		/// <param name="flags">The binding flags to use to locate the element.</param>
		/// <param name="raise">True to raise an exception if the element is not found.</param>
		/// <returns>The MemberInfo object of the element, or null if it is not found.</returns>
		public static MemberInfo GetElementInfo( Type type, string name, BindingFlags flags = DefaultBindings, bool raise = false )
		{
			if( type == null ) throw new ArgumentNullException( "type", "Declaring Type cannot be null." );
			name = name.Validated( "Member Name", invalidChars: InvalidNameChars );

			MemberInfo info = null;
			info = type.GetProperty( name, flags ); if( info != null ) return info;
			info = type.GetField( name, flags ); if( info != null ) return info;

			if( raise ) throw new ArgumentException( "Member not found: " + name );
			return null;
		}
		
		/// <summary>
		/// Returns the <see cref="MemberInfo"/> object corresponding to the element specified.
		/// Supported elements are properties and fields.
		/// </summary>
		/// <typeparam name="T">The type where the element is defined.</typeparam>
		/// <param name="name">The name of the element.</param>
		/// <param name="flags">The binding flags to use to locate the element.</param>
		/// <param name="raise">True to raise an exception if the element is not found.</param>
		/// <returns>The MemberInfo object of the element, or null if it is not found.</returns>
		public static MemberInfo GetElementInfo<T>( string name, BindingFlags flags = DefaultBindings, bool raise = false )
		{
			return GetElementInfo( typeof( T ), name, flags, raise );
		}

		/// <summary>
		/// Gets the type of the element specified.
		/// Supported elements are properties and fields.
		/// </summary>
		/// <param name="info">The <see cref="MemberInfo"/> identifying the element.</param>
		/// <param name="raise">True to rause an exception if the element is not found.</param>
		/// <returns>The type of the element, or null if it is not found.</returns>
		public static Type GetElementType( MemberInfo info, bool raise = false )
		{
			if( info == null ) throw new ArgumentNullException( "info", "MemberInfo cannot be null." );
			switch( info.MemberType ) {
				case MemberTypes.Property: return ( (PropertyInfo)info ).PropertyType;
				case MemberTypes.Field: return ( (FieldInfo)info ).FieldType;
			}
			if( raise ) throw new InvalidOperationException( "Unsupported MemberInfo type: " + info.MemberType );
			return null;
		}
		
		/// <summary>
		/// Gets the type of the element specified.
		/// Supported elements are properties and fields.
		/// </summary>
		/// <param name="type">The type where the element is defined.</param>
		/// <param name="name">The name of the element.</param>
		/// <param name="flags">The binding flags to use to locate the element.</param>
		/// <param name="raise">True to raise an exception if the element is not found.</param>
		/// <returns>The type of the element, or null if it is not found.</returns>
		public static Type GetElementType( Type type, string name, BindingFlags flags = DefaultBindings, bool raise = false )
		{
			MemberInfo info = GetElementInfo( type, name, flags, raise ); if( info == null ) return null; // raise treatment is implicit
			return GetElementType( info, raise );
		}
		
		/// <summary>
		/// Gets the type of the element specified.
		/// Supported elements are properties and fields.
		/// </summary>
		/// <typeparam name="T">The type where the element is defined.</typeparam>
		/// <param name="name">The name of the element.</param>
		/// <param name="flags">The binding flags to use to locate the element.</param>
		/// <param name="raise">True to raise an exception if the element is not found.</param>
		/// <returns>The type of the element, or null if it is not found.</returns>
		public static Type GetElementType<T>( string name, BindingFlags flags = DefaultBindings, bool raise = false )
		{
			return GetElementType( typeof( T ), name, flags, raise );
		}

		// --------------------------------------------------
		/// <summary>
		/// Gets the value of the instance element specified.
		/// Supported elements are properties and fields.
		/// </summary>
		/// <param name="info">The <see cref="MemberInfo"/> object identifying the element.</param>
		/// <param name="obj">The hosting instance of the element.</param>
		/// <param name="indexes">The indexes to use to access the element, if needed.</param>
		/// <returns>The value the element holds.</returns>
		public static object GetElementValue( MemberInfo info, object obj, object[] indexes = null )
		{
			if( info == null ) throw new ArgumentNullException( "info", "MemberInfo cannot be null." );
			switch( info.MemberType ) {
				case MemberTypes.Property: return ( (PropertyInfo)info ).GetValue( obj, indexes );
				case MemberTypes.Field:
					if( indexes != null ) throw new InvalidOperationException( "Indexes cannot be used with fields." );
					return ( (FieldInfo)info ).GetValue( obj );
			}
			throw new InvalidOperationException( "Unsupported MemberInfo type: " + info.MemberType );
		}

		/// <summary>
		/// Gets the value of the instance element specified.
		/// Supported elements are properties and fields.
		/// </summary>
		/// <param name="obj">The hosting instance of the element.</param>
		/// <param name="name">The name of the element.</param>
		/// <param name="indexes">The indexes to use to access the element, if needed.</param>
		/// <param name="flags">The binding flags to use to locate the element.</param>
		/// <returns>The value the element holds.</returns>
		public static object GetElementValue( object obj, string name, object[] indexes = null, BindingFlags flags = DefaultBindings )
		{
			if( obj == null ) throw new ArgumentNullException( "obj", "Object cannot be null." );
			flags &= ~BindingFlags.Static; // Removing the static flag
			flags |= BindingFlags.Instance; // And adding the instance flag

			MemberInfo info = GetElementInfo( obj.GetType(), name, flags, raise: true );
			return GetElementValue( info, obj, indexes );
		}

		/// <summary>
		/// Gets the value of the static element specified.
		/// Supported elements are properties and fields.
		/// </summary>
		/// <param name="type">The type where the static element is defined.</param>
		/// <param name="name">The name of the static element.</param>
		/// <param name="indexes">The indexes to use to access the element, if needed.</param>
		/// <param name="flags">The binding flags to use to locate the element.</param>
		/// <returns>The value the element holds.</returns>
		public static object GetStaticElementValue( Type type, string name, object[] indexes = null, BindingFlags flags = DefaultBindings )
		{
			if( type == null ) throw new ArgumentNullException( "type", "Declaring Type cannot be null." );
			flags |= BindingFlags.Static; // Adding the static flag
			flags &= ~BindingFlags.Instance; // And removing the instance flag

			MemberInfo info = TypeHelper.GetElementInfo( type, name, flags, raise: true );
			return GetElementValue( info, null, indexes );
		}

		/// <summary>
		/// Gets the value of the static element specified.
		/// Supported elements are properties and fields.
		/// </summary>
		/// <typeparam name="T">The type where the static element is defined.</typeparam>
		/// <param name="name">The name of the static element.</param>
		/// <param name="indexes">The indexes to use to access the element, if needed.</param>
		/// <param name="flags">The binding flags to use to locate the element.</param>
		/// <returns>The value the element holds.</returns>
		public static object GetStaticElementValue<T>( string name, object[] indexes = null, BindingFlags flags = DefaultBindings )
		{
			return GetElementValue( typeof( T ), name, indexes, flags );
		}

		/// <summary>
		/// Sets the value of the instance element specified.
		/// Supported elements are properties and fields.
		/// </summary>
		/// <param name="info">The <see cref="MemberInfo"/> object identifying the element.</param>
		/// <param name="obj">The hosting instance of the element.</param>
		/// <param name="value">The value to set the element to.</param>
		/// <param name="indexes">The indexes to use to access the element, if needed.</param>
		public static void SetElementValue( MemberInfo info, object obj, object value, object[] indexes = null )
		{
			if( info == null ) throw new ArgumentNullException( "info", "MemberInfo cannot be null." );

			switch( info.MemberType ) {
				case MemberTypes.Property: ( (PropertyInfo)info ).SetValue( obj, value, indexes ); return;
				case MemberTypes.Field:
					if( indexes != null ) throw new InvalidOperationException( "Indexes cannot be used with fields." );
					( (FieldInfo)info ).SetValue( obj, value );
					return;
			}
			throw new InvalidOperationException( "Unsupported MemberInfo type: " + info.MemberType );
		}
		
		/// <summary>
		/// Sets the value of the instance element specified.
		/// Supported elements are properties and fields.
		/// </summary>
		/// <param name="obj">The hosting instance of the element.</param>
		/// <param name="name">The name of the element.</param>
		/// <param name="value">The value to set the element to.</param>
		/// <param name="indexes">The indexes to use to access the element, if needed.</param>
		/// <param name="flags">The binding flags to use to locate the element.</param>
		public static void SetElementValue( object obj, string name, object value, object[] indexes = null, BindingFlags flags = DefaultBindings )
		{
			if( obj == null ) throw new ArgumentNullException( "obj", "Object cannot be null." );
			flags &= ~BindingFlags.Static; // Removing the static flag
			flags |= BindingFlags.Instance; // And adding the instance flag

			MemberInfo info = TypeHelper.GetElementInfo( obj.GetType(), name, flags, raise: true );
			SetElementValue( info, obj, value, indexes );
		}

		/// <summary>
		/// Sets the value of the static element specified.
		/// Supported elements are properties and fields.
		/// </summary>
		/// <param name="type">The type where the static element is defined.</param>
		/// <param name="name">The name of the static element.</param>
		/// <param name="value">The value to set the element to.</param>
		/// <param name="indexes">The indexes to use to access the element, if needed.</param>
		/// <param name="flags">The binding flags to use to locate the element.</param>
		public static void SetStaticElementValue( Type type, string name, object value, object[] indexes = null, BindingFlags flags = DefaultBindings )
		{
			if( type == null ) throw new ArgumentNullException( "type", "Declaring Type cannot be null." );
			flags |= BindingFlags.Static; // Adding the static flag
			flags &= ~BindingFlags.Instance; // And removing the instance flag

			MemberInfo info = TypeHelper.GetElementInfo( type, name, flags, raise: true );
			SetElementValue( info, null, value, indexes );
		}
		
		/// <summary>
		/// Sets the value of the static element specified.
		/// Supported elements are properties and fields.
		/// </summary>
		/// <typeparam name="T">The type where the static element is defined.</typeparam>
		/// <param name="name">The name of the static element.</param>
		/// <param name="value">The value to set the element to.</param>
		/// <param name="indexes">The indexes to use to access the element, if needed.</param>
		/// <param name="flags">The binding flags to use to locate the element.</param>
		public static void SetStaticElementValue<T>( string name, object value, object[] indexes = null, BindingFlags flags = DefaultBindings )
		{
			SetElementValue( typeof( T ), name, value, indexes, flags );
		}
	}
}
// ========================================================
