// ========================================================
namespace MB.KeroseneORM.Direct
{
	using global::System;
	using global::System.Data.Common;

	// =====================================================
	/// <summary>
	/// Represents a given database and version by inheriting the <see cref="IKFactory"/> iterface, providing its factory
	/// methods to create specific objects adapted to it, and being a "direct" version as it provides a factory method to
	/// return the ADO provider factory to use.
	/// </summary>
	public interface IKFactoryDirect : IKFactory
	{
		/// <summary>
		/// Gets the ADO provider factory this <see cref="IKFactoryDirect"/> instance refers to.
		/// </summary>
		DbProviderFactory DbProviderFactory { get; }
	}
}
// ========================================================
