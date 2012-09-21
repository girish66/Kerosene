// ========================================================
namespace MB.KeroseneORM.WCF
{
	using global::System;
	using global::System.ServiceModel;
	using global::MB.Tools.Dynamics;

	// =====================================================
	/// <summary>
	/// Represents a WCF service able to connect with an underlying database and respond to the requests against it on
	/// behalf of a client-side link.
	/// </summary>
	[ServiceContract( SessionMode = SessionMode.Required )] // Each session will use its own proxy
	[ServiceKnownType( "GetKnownTypes", typeof( KTypesWCF ) )] // To obtain the registered types to be used with WCF
	public interface IKProxyWCF : IDisposable
	{
		[OperationContract] Guid GetProxyId();
		[OperationContract] Guid ProxyConnect( DeepObject package );
		[OperationContract] void ProxyDisconnect();

		[OperationContract] bool DbCaseSensitiveNames();
		[OperationContract] void DbOpen();
		[OperationContract] void DbClose();
		[OperationContract] bool IsDbOpened();

		[OperationContract] void SetTransactionMode( KTransactionMode mode );
		[OperationContract] KTransactionMode TransactionMode();
		[OperationContract] KTransactionState TransactionState();

		[OperationContract] void TransactionStart();
		[OperationContract] void TransactionCommit();
		[OperationContract] void TransactionAbort();

		[OperationContract] Guid EnumeratorCreate( string text, KParameterList pars );
		[OperationContract] void EnumeratorDispose( Guid uid );
		[OperationContract] void EnumeratorReset( Guid uid );
		[OperationContract] bool EnumeratorMoveNext( Guid uid );
		[OperationContract] KSchema EnumeratorSchema( Guid uid );
		[OperationContract] KRecord EnumeratorCurrent( Guid uid );

		[OperationContract] Guid ExecutorCreate( string text, KParameterList pars );
		[OperationContract] void ExecutorDispose( Guid uid );
		[OperationContract] int ExecutorExecute( Guid uid );
	}
}
// ========================================================
