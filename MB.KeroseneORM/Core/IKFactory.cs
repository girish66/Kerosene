// ========================================================
namespace MB.KeroseneORM
{
	using global::System;

	// =====================================================
	/// <summary>
	/// Represents a given database vendor and version.
	/// <para>It does also provide factory methods to create specific objects adapted to the database type.</para>
	/// </summary>
	public interface IKFactory
	{
		/// <summary>
		/// Creates a new <see cref="KParameterList"/> instance adapted for this database type.
		/// </summary>
		/// <returns>A new parameter list adapted for this database type.</returns>
		KParameterList CreateParameterList();

		/// <summary>
		/// Creates a new <see cref="KParser"/> instance adapted for this database type.
		/// </summary>
		/// <returns>A new parser adapted for this database type.</returns>
		KParser CreateParser();
	}
}
// ========================================================
