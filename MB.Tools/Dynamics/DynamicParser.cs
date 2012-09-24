// ========================================================
#undef DEBUG
namespace MB.Tools.Dynamics
{
	using global::System.Diagnostics;
	using global::System;
	using global::System.Text;
	using global::System.Dynamic;
	using global::System.Collections;
	using global::System.Collections.Generic;
	using global::System.Reflection;
	using global::System.Linq.Expressions;
	using global::System.Runtime.Serialization;
	using global::System.Runtime.CompilerServices;

	// =====================================================
	/// <summary>
	/// Represents the parsing capability of dynamic lambda expressions, creating an instance of <see cref="DynamicParser"/>
	/// to hold the results of it.
	/// </summary>
	public class DynamicParser : IDisposable
	{
		List<DynamicNode.Argument> _Arguments = new List<DynamicNode.Argument>();
		internal DynamicNode _LastNode = null;
		internal object _Returned = null;

		private DynamicParser()
		{
			DEBUG.IndentLine( "\n-- DynamicParser()" );
			DEBUG.Unindent();
		}

		protected virtual void Dispose( bool disposing )
		{
			DEBUG.IndentLine( "\n-- DynamicParser.Dispose( Disposing={0} ) This={1}", disposing, this );

			_Returned = null; // It might be in use elsewhere...
			if( _LastNode != null ) { _LastNode.Dispose(); _LastNode = null; }
			if( _Arguments != null ) { foreach( var arg in _Arguments ) arg.Dispose(); _Arguments.Clear(); _Arguments = null; }

			DEBUG.Unindent();
		}
		public void Dispose()
		{
			DEBUG.IndentLine( "\n-- DynamicParser.Dispose() This={0}", this );
			Dispose( true );
			GC.SuppressFinalize( this );
			DEBUG.Unindent();
		}
		~DynamicParser()
		{
			DEBUG.IndentLine( "\n-- ~DynamicParser() This={0}", this );
			Dispose( false );
			DEBUG.Unindent();
		}

		public override string ToString()
		{
			if( _Arguments == null ) return "<DynamicParser>";

			StringBuilder sb = new StringBuilder();
			sb.Append( "(" ); bool first = true; foreach( var arg in _Arguments ) {
				if( !first ) sb.Append( ", " ); else { sb.Append( " " ); first = false; }
				sb.Append( arg );
			}
			if( !first ) sb.Append( " " );
			sb.Append( ") => " );
			sb.Append( Result == null ? "null" : TypeHelper.ToString( Result ) );
			return sb.ToString();
		}

		/// <summary>
		/// Gets the dynamic arguments used in the dynamic lambda expression that has been parsed.
		/// </summary>
		public IEnumerable<DynamicNode.Argument> Arguments
		{
			get
			{
				if( _Arguments == null ) yield return null;
				foreach( var arg in _Arguments ) yield return arg;
			}
		}
		
		/// <summary>
		/// Gets the result of the parsing of the dynamic lambda expression.
		/// Typically it is an instance of a class derived from <see cref="DynamicNode"/>, but it can also be any arbitrary
		/// object depending upon to what kind of logic expression or actual object the expression parsed resolves to.
		/// </summary>
		public object Result
		{
			get { return _Returned ?? _LastNode; }
		}

		// --------------------------------------------------
		/// <summary>
		/// Parses the dynamic lambda expression given in the form of a delegate, and creates and returns a new instance of
		/// <see cref="DynamicParser"/> to maintain the results.
		/// </summary>
		/// <param name="func">The dynamic lambda expression or delegate to parser.</param>
		/// <returns>The instance holding the results of the parsing.</returns>
		static public DynamicParser Parse( Delegate func )
		{
			DEBUG.IndentLine( "\n-- DynamicParser.Parse( Func )" );

			if( func == null ) throw new ArgumentNullException( "func", "Delegate to parse cannot be null." );
			DynamicParser parser = new DynamicParser();

			ParameterInfo[] pars = func.Method.GetParameters(); foreach( var par in pars ) {
				bool isdynamic = par.GetCustomAttributes( typeof( DynamicAttribute ), true ).Length != 0 ? true : false;
				if( isdynamic ) {
					var dyn = new DynamicNode.Argument( par.Name ) { _Parser = parser };
					parser._Arguments.Add( dyn );
				}
				else throw new ArgumentException( string.Format( "Argument '{0}' is not dynamic.", par.Name ) );
			}
			parser._Returned = func.DynamicInvoke( parser._Arguments.ToArray() );

			DEBUG.UnindentLine( "\n-- Parsed={0}", parser );
			return parser;
		}
	}

	// =====================================================
	/// <summary>
	/// Represents a logic node in the logic tree returned as the result of parsing a dynamic lambda expression.
	/// </summary>
	[Serializable]
	public class DynamicNode : IDisposable, ISerializable, IDynamicMetaObjectProvider
	{
		internal DynamicParser _Parser = null;
		internal DynamicNode _Host = null;

		protected DynamicNode()
		{
			DEBUG.IndentLine( "\n-- DynamicNode()" );
			DEBUG.Unindent();
		}

		protected virtual void Dispose( bool disposing )
		{
			DEBUG.IndentLine( "\n-- DynamicNode.Dispose( Disposing={0} ) - This={1}", disposing, this );

			_Host = null;
			_Parser = null;

			DEBUG.Unindent();
		}
		public void Dispose()
		{
			DEBUG.IndentLine( "\n-- DynamicNode.Dispose() - This={0}", this );
			Dispose( true );
			GC.SuppressFinalize( this );
			DEBUG.Unindent();
		}
		~DynamicNode()
		{
			DEBUG.IndentLine( "\n-- DynamicNode~() - This={0}", this );
			Dispose( false );
			DEBUG.Unindent();
		}

		public DynamicMetaObject GetMetaObject( Expression parameter )
		{
			DynamicMetaObject meta = new DynamicMetaNode(
				parameter,
				BindingRestrictions.GetInstanceRestriction( parameter, this ),
				this );
			return meta;
		}

		protected DynamicNode( SerializationInfo info, StreamingContext context )
		{
			string type = info.GetString( "HostType" );
			_Host = type == "VOID" ? null : (DynamicNode)info.GetValue( "HostItem", Type.GetType( type ) );
		}
		public virtual void GetObjectData( SerializationInfo info, StreamingContext context )
		{
			info.AddValue( "HostType", _Host == null ? "VOID" : _Host.GetType().AssemblyQualifiedName );
			if( _Host != null ) info.AddValue( "HostItem", _Host );
		}

		// --------------------------------------------------
		/// <summary>
		/// Represents the host node of this one in case it is hosted, meaning that it is a property or method related to a
		/// parent object.
		/// </summary>
		public DynamicNode Host
		{
			get { return _Host; }
		}

		public override string ToString()
		{
			return "<DynamicNode>";
		}

		// --------------------------------------------------
		/// <summary>
		/// Represents a dynamic argument used in the dynamic lambda expression.
		/// </summary>
		[Serializable]
		public class Argument : DynamicNode, ISerializable
		{
			/// <summary>
			/// Creates a new instance of <see cref="DynamicNode.Argument"/> where its name (tag) is given.
			/// </summary>
			/// <param name="tag">The name (tag) of the dynamic argument.</param>
			public Argument( string tag )
			{
				DEBUG.IndentLine( "\n-- Argument( Tag={0} )", tag ?? "null" );

				Tag = tag.Validated( "Tag", invalidChars: TypeHelper.InvalidNameChars );
				DEBUG.Unindent();
			}
			protected override void Dispose( bool disposing )
			{
				DEBUG.IndentLine( "\n-- Argument.Dispose( Disposing={0} ) - This={1}", disposing, this );

				base.Dispose( disposing );
				DEBUG.Unindent();
			}

			/// <summary>
			/// Gets the name (tag) of this dynamic argument.
			/// </summary>
			public string Tag
			{
				get;
				private set;
			}

			public override string ToString()
			{
				return Tag;
			}
			protected Argument( SerializationInfo info, StreamingContext context ) : base( info, context )
			{
				Tag = info.GetString( "Tag" );
			}
			public override void GetObjectData( SerializationInfo info, StreamingContext context )
			{
				info.AddValue( "Tag", Tag );
				base.GetObjectData( info, context );
			}
		}

		// --------------------------------------------------
		/// <summary>
		/// Represents a member access operation.
		/// </summary>
		[Serializable]
		public class GetMember : DynamicNode, ISerializable
		{
			/// <summary>
			/// Creates a new instance of <see cref="DynamicNode.GetMember"/> hosted in the given parent, and with the name
			/// given.
			/// </summary>
			/// <param name="host">The hosting parent of this node.</param>
			/// <param name="name">The name of the member to access to.</param>
			public GetMember( DynamicNode host, string name )
			{
				DEBUG.IndentLine( "\n-- GetMember( Host={0}, Name={1} )", TypeHelper.ToString( host ), name ?? "null" );

				if( ( _Host = host ) == null ) throw new ArgumentNullException( "host", "Host cannot be null." );
				Name = name.Validated( "Name", invalidChars: TypeHelper.InvalidNameChars );

				DEBUG.Unindent();
			}
			protected override void Dispose( bool disposing )
			{
				DEBUG.IndentLine( "\n-- GetMember.Dispose( Disposing={0} ) - This={1}", disposing, this );
				base.Dispose( disposing );
				DEBUG.Unindent();
			}

			/// <summary>
			/// Gets the name of this member (property).
			/// </summary>
			public string Name
			{
				get;
				private set;
			}

			public override string ToString()
			{
				return string.Format( "{0}.{1}",
					Host == null ? "<null>" : Host.ToString(),
					Name );
			}
			protected GetMember( SerializationInfo info, StreamingContext context ) : base( info, context )
			{
				Name = info.GetString( "MemberName" );
			}
			public override void GetObjectData( SerializationInfo info, StreamingContext context )
			{
				info.AddValue( "MemberName", Name );
				base.GetObjectData( info, context );
			}
		}

		// --------------------------------------------------
		/// <summary>
		/// Represents the set value operation on the member specified.
		/// </summary>
		[Serializable]
		public class SetMember : DynamicNode, ISerializable
		{
			/// <summary>
			/// Creates a new <see cref="DynamicNode.SetMember"/> instance for a member hosted in the given parent, with the
			/// name given, and specifying the value to (logically) set to it.
			/// </summary>
			/// <param name="host">The hosting parent of this node.</param>
			/// <param name="name">The name of the member to set its value.</param>
			/// <param name="value">The value to (logically) set to this member.</param>
			public SetMember( DynamicNode host, string name, object value )
			{
				DEBUG.IndentLine( "\n-- SetMember( Host={0}, Name={1}, Value={2} )", TypeHelper.ToString( host ), name ?? "null", TypeHelper.ToString( value ) );

				if( ( _Host = host ) == null ) throw new ArgumentNullException( "host", "Host cannot be null." );
				Name = name.Validated( "Name", invalidChars: TypeHelper.InvalidNameChars );
				Value = value;

				DEBUG.Unindent();
			}
			protected override void Dispose( bool disposing )
			{
				DEBUG.IndentLine( "\n-- SetMember.Dispose( Disposing={0} ) - This={1}", disposing, this );

				Value = null;
				base.Dispose( disposing );

				DEBUG.Unindent();
			}

			/// <summary>
			/// Gets the name of this member (property).
			/// </summary>
			public string Name
			{
				get;
				private set;
			}

			/// <summary>
			/// Gets the value that has been (logically) set to this member.
			/// </summary>
			public object Value
			{
				get;
				private set;
			}

			public override string ToString()
			{
				return string.Format( "( {0}.{1} = {2} )",
					Host == null ? "<null>" : Host.ToString(),
					Name,
					TypeHelper.ToString( Value ) );
			}
			protected SetMember( SerializationInfo info, StreamingContext context ) : base( info, context )
			{
				Name = info.GetString( "MemberName" );

				string type = info.GetString( "MemberType" );
				Value = type == "VOID" ? null : info.GetValue( "MemberValue", Type.GetType( type ) );
			}
			public override void GetObjectData( SerializationInfo info, StreamingContext context )
			{
				info.AddValue( "MemberName", Name );
				info.AddValue( "MemberType", Value == null ? "VOID" : Value.GetType().AssemblyQualifiedName );
				if( Value != null ) info.AddValue( "MemberValue", Value );

				base.GetObjectData( info, context );
			}
		}

		// --------------------------------------------------
		/// <summary>
		/// Represents the access operation to an indexed member.
		/// </summary>
		[Serializable]
		public class GetIndex : DynamicNode, ISerializable
		{
			/// <summary>
			/// Create a new instance of <see cref="DynamicNode.GetIndex"/> hosted in the given parent and with the indexes
			/// given.
			/// </summary>
			/// <param name="host">The hosting parent of this node.</param>
			/// <param name="indexes">The indexes used to access this member.</param>
			public GetIndex( DynamicNode host, object[] indexes )
			{
				DEBUG.IndentLine( "\n-- GetIndex( Host={0}, Indexes={1} )", TypeHelper.ToString( host ), TypeHelper.ToString( indexes, brackets: "[]" ) );

				if( ( _Host = host ) == null ) throw new ArgumentNullException( "host", "Host cannot be null." );
				Indexes = indexes == null ? new object[0] : indexes;

				DEBUG.Unindent();
			}
			protected override void Dispose( bool disposing )
			{
				DEBUG.IndentLine( "\n-- GetIndex.Dispose( Disposing={0} ) - This={1}", disposing, this );

				Indexes = null;
				base.Dispose( disposing );

				DEBUG.Unindent();
			}

			/// <summary>
			/// Gets the indexes used to access this member.
			/// </summary>
			public object[] Indexes
			{
				get;
				private set;
			}

			public override string ToString()
			{
				return string.Format( "{0}{1}",
					Host == null ? "<null>" : Host.ToString(),
					TypeHelper.ToString( Indexes, "[]" ) );
			}
			protected GetIndex( SerializationInfo info, StreamingContext context ) : base( info, context )
			{
				int count = (int)info.GetValue( "IndexCount", typeof( int ) );
				Indexes = new object[count];

				for( int i = 0; i < count; i++ ) {
					string type = info.GetString( "IndexType" + i );
					Indexes[i] = type == "VOID" ? null : info.GetValue( "IndexValue" + i, Type.GetType( type ) );
				}
			}
			public override void GetObjectData( SerializationInfo info, StreamingContext context )
			{
				info.AddValue( "IndexCount", Indexes.Length );
				for( int i = 0; i < Indexes.Length; i++ ) {
					info.AddValue( "IndexType" + i, Indexes[i] == null ? "VOID" : Indexes[i].GetType().AssemblyQualifiedName );
					if( Indexes[i] != null ) info.AddValue( "IndexValue" + i, Indexes[i] );
				}
				base.GetObjectData( info, context );
			}
		}

		// --------------------------------------------------
		/// <summary>
		/// Represents the set value operation on the indexed member specified.
		/// </summary>
		[Serializable]
		public class SetIndex : DynamicNode, ISerializable
		{
			/// <summary>
			/// Create a new instance of <see cref="DynamicNode.GetIndex"/> hosted in the given parent and with the indexes
			/// given.
			/// </summary>
			/// <param name="host">The hosting parent of this node.</param>
			/// <param name="indexes">The indexes used to access this member.</param>
			/// /// <param name="value">The value to (logically) set to this member.</param>
			public SetIndex( DynamicNode host, object[] indexes, object value )
			{
				DEBUG.IndentLine( "\n-- SetIndex( Host={0}, Indexes={1}, Value={2} )", TypeHelper.ToString( host ), TypeHelper.ToString( indexes, brackets: "[]" ), TypeHelper.ToString( value ) );

				if( ( _Host = host ) == null ) throw new ArgumentNullException( "host", "Host cannot be null." );
				Indexes = indexes == null ? new object[0] : indexes;
				Value = value;

				DEBUG.Unindent();
			}
			protected override void Dispose( bool disposing )
			{
				DEBUG.IndentLine( "\n-- SetIndex.Dispose( Disposing={0} ) - This={1}", disposing, this );

				Value = null;
				Indexes = null;
				base.Dispose( disposing );

				DEBUG.Unindent();
			}

			/// <summary>
			/// Gets the indexes used to access this member.
			/// </summary>
			public object[] Indexes
			{
				get;
				private set;
			}

			/// <summary>
			/// Gets the value that has been (logically) set to this member.
			/// </summary>
			public object Value
			{
				get;
				private set;
			}

			public override string ToString()
			{
				return string.Format( "( {0}{1} = {2} )",
					Host == null ? "<null>" : Host.ToString(),
					TypeHelper.ToString( Indexes, "[]" ),
					TypeHelper.ToString( Value ) );
			}
			protected SetIndex( SerializationInfo info, StreamingContext context ) : base( info, context )
			{
				int count = (int)info.GetValue( "IndexCount", typeof( int ) );
				Indexes = new object[count];

				for( int i = 0; i < count; i++ ) {
					string type = info.GetString( "IndexType" + i );
					Indexes[i] = type == "VOID" ? null : info.GetValue( "IndexValue" + i, Type.GetType( type ) );
				}

				string type2 = info.GetString( "ValueType" );
				Value = type2 == "VOID" ? null : info.GetValue( "ValueValue", Type.GetType( type2 ) );
			}
			public override void GetObjectData( SerializationInfo info, StreamingContext context )
			{
				info.AddValue( "IndexCount", Indexes.Length );
				for( int i = 0; i < Indexes.Length; i++ ) {
					info.AddValue( "IndexType" + i, Indexes[i] == null ? "VOID" : Indexes[i].GetType().AssemblyQualifiedName );
					if( Indexes[i] != null ) info.AddValue( "IndexValue" + i, Indexes[i] );
				}

				info.AddValue( "ValueType", Value == null ? "VOID" : Value.GetType().AssemblyQualifiedName );
				if( Value != null ) info.AddValue( "ValueValue", Value );

				base.GetObjectData( info, context );
			}
		}

		// --------------------------------------------------
		/// <summary>
		/// Represents a method invocation.
		/// </summary>
		[Serializable]
		public class Method : DynamicNode, ISerializable
		{
			/// <summary>
			/// Create a new instance of <see cref="DynamicNode.Method"/> hosted in the given parent and with the arguments
			/// given.
			/// </summary>
			/// <param name="host">The hosting parent of this node.</param>
			/// <param name="name">The name of the method to (conceptually) invoke.</param>
			/// <param name="arguments">The arguments to use with the method. It can be null if no arguments are used.</param>
			public Method( DynamicNode host, string name, object[] arguments )
			{
				DEBUG.IndentLine( "\n-- Method( Host={0}, Name={1}, Arguments={2} )", TypeHelper.ToString( host ), name ?? "null", TypeHelper.ToString( arguments, brackets: "()" ) );

				if( ( _Host = host ) == null ) throw new ArgumentNullException( "host", "Host cannot be null." );
				Name = name.Validated( "Name", invalidChars: TypeHelper.InvalidNameChars );
				Arguments = arguments == null ? new object[0] : arguments;

				DEBUG.Unindent();
			}
			protected override void Dispose( bool disposing )
			{
				DEBUG.IndentLine( "\n-- Method.Dispose( Disposing={0} ) - This={1}", disposing, this );

				Arguments = null;
				base.Dispose( disposing );

				DEBUG.Unindent();
			}

			/// <summary>
			/// The name of the method.
			/// </summary>
			public string Name
			{
				get;
				private set;
			}

			/// <summary>
			/// Gets the arguments used to (conceptually) invoke this method. It can be null or an empty array if no arguments were
			/// used.
			/// </summary>
			public object[] Arguments
			{
				get;
				private set;
			}

			public override string ToString()
			{
				return string.Format( "{0}.{1}{2}",
					Host == null ? "<null>" : Host.ToString(),
					Name,
					TypeHelper.ToString( Arguments, "()" ) );
			}
			protected Method( SerializationInfo info, StreamingContext context ) : base( info, context )
			{
				Name = info.GetString( "MethodName" );

				int count = (int)info.GetValue( "ArgumentCount", typeof( int ) );
				Arguments = new object[count];

				for( int i = 0; i < count; i++ ) {
					string type = info.GetString( "ArgumentType" + i );
					Arguments[i] = type == "VOID" ? null : info.GetValue( "ArgumentValue" + i, Type.GetType( type ) );
				}
			}
			public override void GetObjectData( SerializationInfo info, StreamingContext context )
			{
				info.AddValue( "MethodName", Name );

				info.AddValue( "ArgumentCount", Arguments.Length );
				for( int i = 0; i < Arguments.Length; i++ ) {
					info.AddValue( "ArgumentType" + i, Arguments[i] == null ? "VOID" : Arguments[i].GetType().AssemblyQualifiedName );
					if( Arguments[i] != null ) info.AddValue( "ArgumentValue" + i, Arguments[i] );
				}
				base.GetObjectData( info, context );
			}
		}

		// --------------------------------------------------
		/// <summary>
		/// Represents a direct invocation of the node specified.
		/// </summary>
		[Serializable]
		public class Invoke : DynamicNode, ISerializable
		{
			/// <summary>
			/// Creates a new instance of <see cref="DynamicNode.Invoke"/> for the node specified using the arguments given.
			/// </summary>
			/// <param name="host">The node to (conceptually) invoke.</param>
			/// <param name="arguments">The arguments to use to invoke this node. It can be null if no arguments are used.</param>
			public Invoke( DynamicNode host, object[] arguments )
			{
				DEBUG.IndentLine( "\n-- Invoke( Host={0}, Arguments={1} )", TypeHelper.ToString( host ), TypeHelper.ToString( arguments, brackets: "()" ) );

				if( ( _Host = host ) == null ) throw new ArgumentNullException( "host", "Host cannot be null." );
				Arguments = arguments == null ? new object[0] : arguments;

				DEBUG.Unindent();
			}
			protected override void Dispose( bool disposing )
			{
				DEBUG.IndentLine( "\n-- Invoke.Dispose( Disposing={0} ) - This={1}", disposing, this );

				Arguments = null;
				base.Dispose( disposing );

				DEBUG.Unindent();
			}

			/// <summary>
			/// Gets the arguments used to (conceptually) invoke this node. It can be null or an empty array if no arguments were
			/// used.
			/// </summary>
			public object[] Arguments
			{
				get;
				private set;
			}

			public override string ToString()
			{
				return string.Format( "{0}{1}",
					Host == null ? "<null>" : Host.ToString(),
					TypeHelper.ToString( Arguments, "()" ) );
			}
			protected Invoke( SerializationInfo info, StreamingContext context ) : base( info, context )
			{
				int count = (int)info.GetValue( "ArgumentCount", typeof( int ) );
				Arguments = new object[count];

				for( int i = 0; i < count; i++ ) {
					string type = info.GetString( "ArgumentType" + i );
					Arguments[i] = type == "VOID" ? null : info.GetValue( "ArgumentValue" + i, Type.GetType( type ) );
				}
			}
			public override void GetObjectData( SerializationInfo info, StreamingContext context )
			{
				info.AddValue( "ArgumentCount", Arguments.Length );
				for( int i = 0; i < Arguments.Length; i++ ) {
					info.AddValue( "ArgumentType" + i, Arguments[i] == null ? "VOID" : Arguments[i].GetType().AssemblyQualifiedName );
					if( Arguments[i] != null ) info.AddValue( "ArgumentValue" + i, Arguments[i] );
				}
				base.GetObjectData( info, context );
			}
		}

		// --------------------------------------------------
		/// <summary>
		/// Represents a binary operation between its DynamicNode left operand, and its generic right one.
		/// </summary>
		[Serializable]
		public class Binary : DynamicNode, ISerializable
		{
			/// <summary>
			/// Creates a new <see cref="DynamicNode.Binary"/> instance for the operands given.
			/// </summary>
			/// <param name="left">The <see cref="DynamicNode"/> left operand who has (conceptually) initiate this operation.</param>
			/// <param name="operation">The operation to (conceptually) execute.</param>
			/// <param name="right">The right operand of the expression, that can be any valid object including null references.</param>
			public Binary( DynamicNode left, ExpressionType operation, object right )
			{
				DEBUG.IndentLine( "\n-- Binary( Left={0}, Operation={1}, Right={2} )", TypeHelper.ToString( left ), operation, TypeHelper.ToString( right ) );

				if( ( Left = left ) == null ) throw new ArgumentNullException( "left", "Left cannot be null." );
				Operation = operation;
				Right = right;

				DEBUG.Unindent();
			}
			protected override void Dispose( bool disposing )
			{
				DEBUG.IndentLine( "\n-- Binary.Dispose (Disposing={0} ) - This={1}", disposing, this );

				Left = null;
				Right = null;
				base.Dispose( disposing );

				DEBUG.Unindent();
			}

			/// <summary>
			/// Gets the left operand.
			/// </summary>
			public DynamicNode Left
			{
				get;
				private set;
			}

			/// <summary>
			/// Gets the specific binary operation this node refers to.
			/// </summary>
			public ExpressionType Operation
			{
				get;
				private set;
			}
			
			/// <summary>
			/// Gets the right operand, it can be any valid object including null references.
			/// </summary>
			public object Right
			{
				get;
				private set;
			}

			public override string ToString()
			{
				return string.Format( "({0} {1} {2})",
					Left == null ? "<null>" : Left.ToString(),
					Operation,
					TypeHelper.ToString( Right ) );
			}
			protected Binary( SerializationInfo info, StreamingContext context ) : base( info, context )
			{
				string type = info.GetString( "LeftType" );
				Left = (DynamicNode)info.GetValue( "LeftItem", Type.GetType( type ) );

				Operation = (ExpressionType)info.GetValue( "Operation", typeof( ExpressionType ) );

				type = info.GetString( "RightType" );
				Right = type == "VOID" ? null : info.GetValue( "RightItem", Type.GetType( type ) );
			}
			public override void GetObjectData( SerializationInfo info, StreamingContext context )
			{
				info.AddValue( "LeftType", Left.GetType().AssemblyQualifiedName );
				info.AddValue( "LeftItem", Left );

				info.AddValue( "Operation", Operation );

				info.AddValue( "RightType", Right == null ? "VOID" : Right.GetType().AssemblyQualifiedName );
				if( Right != null ) info.AddValue( "RightItem", Right );

				base.GetObjectData( info, context );
			}
		}

		// --------------------------------------------------
		/// <summary>
		/// Represents an unary operation on its target operand.
		/// </summary>
		[Serializable]
		public class Unary : DynamicNode, ISerializable
		{
			/// <summary>
			/// Creates a new <see cref="DynamicNode.Unary"/> instance for its target operand.
			/// </summary>
			/// <param name="operation">The unary operation to (conceptually) execute.</param>
			/// <param name="target">The target operand for this operation.</param>
			public Unary( ExpressionType operation, DynamicNode target )
			{
				DEBUG.IndentLine( "\n-- Unary( Operation={0}, Target={1} )", operation, TypeHelper.ToString( target ) );

				Operation = operation;
				if( ( Target = target ) == null ) throw new ArgumentNullException( "target", "Target cannot be null." );

				DEBUG.Unindent();
			}
			protected override void Dispose( bool disposing )
			{
				DEBUG.IndentLine( "-\n- Unary.Dispose( Disposing={0} ) - This={1}", disposing, this );

				Target = null;
				base.Dispose( disposing );

				DEBUG.Unindent();
			}

			/// <summary>
			/// Gets the unary operation to (conceptually) execute.
			/// </summary>
			public ExpressionType Operation
			{
				get;
				private set;
			}

			/// <summary>
			/// Gets the target operand for this operation.
			/// </summary>
			public DynamicNode Target
			{
				get;
				private set;
			}

			public override string ToString()
			{
				return string.Format( "({0} {1})",
					Operation,
					Target == null ? "<null>" : Target.ToString() );
			}
			protected Unary( SerializationInfo info, StreamingContext context ) : base( info, context )
			{
				Operation = (ExpressionType)info.GetValue( "Operation", typeof( ExpressionType ) );

				string type = info.GetString( "TargetType" );
				Target = (DynamicNode)info.GetValue( "TargetItem", Type.GetType( type ) );
			}
			public override void GetObjectData( SerializationInfo info, StreamingContext context )
			{
				info.AddValue( "Operation", Operation );

				info.AddValue( "TargetType", Target.GetType().AssemblyQualifiedName );
				info.AddValue( "TargetItem", Target );

				base.GetObjectData( info, context );
			}
		}

		// --------------------------------------------------
		/// <summary>
		/// Represents a conversion or cast operation.
		/// </summary>
		[Serializable]
		public class Convert : DynamicNode, ISerializable
		{
			/// <summary>
			/// Creates a new instance of <see cref="DynamicNode.Convert"/> for the target node to the target type.
			/// </summary>
			/// <param name="targetType">The target type to convert the target node to.</param>
			/// <param name="sourceNode">The target node.</param>
			public Convert( Type targetType, DynamicNode sourceNode )
			{
				DEBUG.IndentLine( "\n-- Convert( Type={0}, Source={1} )", targetType.Name, TypeHelper.ToString( sourceNode ) );

				TargetType = targetType;
				if( ( SourceNode = sourceNode ) == null ) throw new ArgumentNullException( "source", "Source cannot be null." );

				DEBUG.Unindent();
			}
			protected override void Dispose( bool disposing )
			{
				DEBUG.IndentLine( "\n-- Convert.Dispose( Disposing={0} ) - This={1}", disposing, this );

				SourceNode = null;
				base.Dispose( disposing );

				DEBUG.Unindent();
			}

			/// <summary>
			/// Gets the type the target node was (conceptually) converted to.
			/// </summary>
			public Type TargetType
			{
				get;
				private set;
			}

			/// <summary>
			/// Gets the target node to be converted.
			/// </summary>
			public DynamicNode SourceNode
			{
				get;
				private set;
			}

			public override string ToString()
			{
				return string.Format( "(({0}) {2})",
					TargetType.Name,
					SourceNode == null ? "null" : SourceNode.ToString() );
			}
			protected Convert( SerializationInfo info, StreamingContext context ) : base( info, context )
			{
				TargetType = (Type)info.GetValue( "TargetType", typeof( Type ) );

				string type = info.GetString( "SourceType" );
				SourceNode = (DynamicNode)info.GetValue( "SourceItem", Type.GetType( type ) );
			}
			public override void GetObjectData( SerializationInfo info, StreamingContext context )
			{
				info.AddValue( "TargetType", TargetType );

				info.AddValue( "SourceType", SourceNode.GetType().AssemblyQualifiedName );
				info.AddValue( "SourceItem", SourceNode );

				base.GetObjectData( info, context );
			}
		}
	}

	// =====================================================
	internal class DynamicMetaNode : DynamicMetaObject
	{
		public DynamicMetaNode( Expression parameter, BindingRestrictions rest, object value )
			: base( parameter, rest, value ) { }

		static object[] MetaList2List( DynamicMetaObject[] metaObjects )
		{
			if( metaObjects == null ) return null;

			object[] list = new object[metaObjects.Length];
			for( int i = 0; i < metaObjects.Length; i++ ) list[i] = metaObjects[i].Value;
			return list;
		}

		public override DynamicMetaObject BindGetMember( GetMemberBinder binder )
		{
			DEBUG.IndentLine( "\n-- BindGetMember: {0}", binder.Name );

			var obj = (DynamicNode)this.Value;
			var node = new DynamicNode.GetMember( obj, binder.Name ) { _Parser = obj._Parser };
			obj._Parser._LastNode = node;

			var par = Expression.Variable( typeof( DynamicNode ), "ret" );
			var exp = Expression.Block(
				new ParameterExpression[] { par },
				Expression.Assign( par, Expression.Constant( node ) )
				);

			DEBUG.Unindent();
			return new DynamicMetaNode( exp, this.Restrictions, node );
		}
		public override DynamicMetaObject BindSetMember( SetMemberBinder binder, DynamicMetaObject value )
		{
			DEBUG.IndentLine( "\n-- BindSetMember: {0} = {1}", binder.Name, value.Value == null ? "<null>" : value.Value.ToString() );

			var obj = (DynamicNode)this.Value;
			var node = new DynamicNode.SetMember( obj, binder.Name, value.Value ) { _Parser = obj._Parser };
			obj._Parser._LastNode = node;

			var par = Expression.Variable( typeof( DynamicNode ), "ret" );
			var exp = Expression.Block(
				new ParameterExpression[] { par },
				Expression.Assign( par, Expression.Constant( node ) )
				);

			DEBUG.Unindent();
			return new DynamicMetaNode( exp, this.Restrictions, node );
		}
		public override DynamicMetaObject BindGetIndex( GetIndexBinder binder, DynamicMetaObject[] indexes )
		{
			DEBUG.IndentLine( "\n-- BindGetIndex: {0}", TypeHelper.ToString( MetaList2List( indexes ), "[]" ) );

			var obj = (DynamicNode)this.Value;
			var node = new DynamicNode.GetIndex( obj, MetaList2List( indexes ) ) { _Parser = obj._Parser };
			obj._Parser._LastNode = node;

			var par = Expression.Variable( typeof( DynamicNode ), "ret" );
			var exp = Expression.Block(
				new ParameterExpression[] { par },
				Expression.Assign( par, Expression.Constant( node ) )
				);

			DEBUG.Unindent();
			return new DynamicMetaNode( exp, this.Restrictions, node );
		}
		public override DynamicMetaObject BindSetIndex( SetIndexBinder binder, DynamicMetaObject[] indexes, DynamicMetaObject value )
		{
			DEBUG.IndentLine( "\n-- BindSetIndex: {0} = {1}", TypeHelper.ToString( MetaList2List( indexes ), "[]" ), value.Value == null ? "<null>" : value.Value.ToString() );

			var obj = (DynamicNode)this.Value;
			var node = new DynamicNode.SetIndex( obj, MetaList2List( indexes ), value.Value ) { _Parser = obj._Parser };
			obj._Parser._LastNode = node;

			var par = Expression.Variable( typeof( DynamicNode ), "ret" );
			var exp = Expression.Block(
				new ParameterExpression[] { par },
				Expression.Assign( par, Expression.Constant( node ) )
				);

			DEBUG.Unindent();
			return new DynamicMetaNode( exp, this.Restrictions, node );
		}
		public override DynamicMetaObject BindInvokeMember( InvokeMemberBinder binder, DynamicMetaObject[] args )
		{
			DEBUG.IndentLine( "\n-- BindInvokeMember: {0}{1}", binder.Name, TypeHelper.ToString( MetaList2List( args ), "()" ) );

			var obj = (DynamicNode)this.Value;
			var node = new DynamicNode.Method( obj, binder.Name, MetaList2List( args ) ) { _Parser = obj._Parser };
			obj._Parser._LastNode = node;

			var par = Expression.Variable( typeof( DynamicNode ), "ret" );
			var exp = Expression.Block(
				new ParameterExpression[] { par },
				Expression.Assign( par, Expression.Constant( node ) )
				);

			DEBUG.Unindent();
			return new DynamicMetaNode( exp, this.Restrictions, node );
		}
		public override DynamicMetaObject BindInvoke( InvokeBinder binder, DynamicMetaObject[] args )
		{
			DEBUG.IndentLine( "\n-- BindInvoke: {0}", TypeHelper.ToString( MetaList2List( args ), "()" ) );

			var obj = (DynamicNode)this.Value;
			var node = new DynamicNode.Invoke( obj, MetaList2List( args ) ) { _Parser = obj._Parser };
			obj._Parser._LastNode = node;

			var par = Expression.Variable( typeof( DynamicNode ), "ret" );
			var exp = Expression.Block(
				new ParameterExpression[] { par },
				Expression.Assign( par, Expression.Constant( node ) )
				);

			DEBUG.Unindent();
			return new DynamicMetaNode( exp, this.Restrictions, node );
		}
		public override DynamicMetaObject BindBinaryOperation( BinaryOperationBinder binder, DynamicMetaObject arg )
		{
			DEBUG.IndentLine( "\n-- BindBinaryOperation: {0} {1}", binder.Operation, arg.Value == null ? "<null>" : arg.Value.ToString() );

			var obj = (DynamicNode)this.Value;
			var node = new DynamicNode.Binary( obj, binder.Operation, arg.Value ) { _Parser = obj._Parser };
			obj._Parser._LastNode = node;

			var par = Expression.Variable( typeof( DynamicNode ), "ret" );
			var exp = Expression.Block(
				new ParameterExpression[] { par },
				Expression.Assign( par, Expression.Constant( node ) )
				);

			DEBUG.Unindent();
			return new DynamicMetaNode( exp, this.Restrictions, node );
		}
		public override DynamicMetaObject BindUnaryOperation( UnaryOperationBinder binder )
		{
			DEBUG.IndentLine( "\n-- BindUnaryOperation: {0}", binder.Operation );

			var obj = (DynamicNode)this.Value;
			var node = new DynamicNode.Unary( binder.Operation, obj ) { _Parser = obj._Parser };
			obj._Parser._LastNode = node;

			// If operation is 'IsTrue' or 'IsFalse', we will return false to keep the engine working...
			object ret = node;
			if( binder.Operation == ExpressionType.IsTrue ) ret = (object)false;
			if( binder.Operation == ExpressionType.IsFalse ) ret = (object)false;

			var par = Expression.Variable( ret.GetType(), "ret" ); // the type is now obtained from "ret"
			var exp = Expression.Block(
				new ParameterExpression[] { par },
				Expression.Assign( par, Expression.Constant( ret ) ) // the expression is now obtained from "ret"
				);

			DEBUG.Unindent();
			return new DynamicMetaNode( exp, this.Restrictions, node );
		}
		public override DynamicMetaObject BindConvert( ConvertBinder binder )
		{
			DEBUG.IndentLine( "\n-- BindConvert: {0}", binder.ReturnType.Name );

			var obj = (DynamicNode)this.Value;
			var node = new DynamicNode.Convert( binder.ReturnType, obj ) { _Parser = obj._Parser };
			obj._Parser._LastNode = node;

			// Reducing the object to return if this is an assignment node...
			object ret = obj;
			bool done = false; while( !done ) {
				if( ret is DynamicNode.SetMember ) ret = ( (DynamicNode.SetMember)obj ).Value;
				else if( ret is DynamicNode.SetIndex ) ret = ( (DynamicNode.SetIndex)obj ).Value;
				else done = true;
			}

			// Creating an instance...
			if( binder.ReturnType == typeof( string ) ) ret = ret.ToString();
			else {
				try {
					if( TypeHelper.IsNullableType( binder.ReturnType ) ) ret = null; // to avoid cast exceptions
					else ret = Activator.CreateInstance( binder.ReturnType, true ); // true to allow non-public ctor as well
				}
				catch { ret = new object(); } // as the last resort scenario
			}

			var par = Expression.Variable( binder.ReturnType, "ret" );
			var exp = Expression.Block(
				new ParameterExpression[] { par },
				Expression.Assign( par, Expression.Constant( ret, binder.ReturnType ) ) // specifying binder.ReturnType
				);

			DEBUG.Unindent();
			return new DynamicMetaNode( exp, this.Restrictions, node );
		}
	}
}
// ========================================================
