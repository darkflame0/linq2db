// ---------------------------------------------------------------------------------------------------
// <auto-generated>
// This code was generated by LinqToDB scaffolding tool (https://github.com/linq2db/linq2db).
// Changes to this file may cause incorrect behavior and will be lost if the code is regenerated.
// </auto-generated>
// ---------------------------------------------------------------------------------------------------

using LinqToDB.Mapping;

#pragma warning disable 1573, 1591
#nullable enable

namespace Cli.Default.MySql
{
	[Table("patient")]
	public class Patient
	{
		[Column("PersonID" , IsPrimaryKey = true )] public int    PersonId  { get; set; } // int
		[Column("Diagnosis", CanBeNull    = false)] public string Diagnosis { get; set; } = null!; // varchar(256)

		#region Associations
		/// <summary>
		/// FK_Patient_Person
		/// </summary>
		[Association(CanBeNull = false, ThisKey = nameof(PersonId), OtherKey = nameof(MySql.Person.PersonId))]
		public Person Person { get; set; } = null!;
		#endregion
	}
}
