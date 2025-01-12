using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Authentifications.Models;
public class UtilisateurDto
{

	/// <summary>
	/// Représente l'identifiant unique d'un utilisateur.
	/// </summary>
	[Key]
	[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
	public Guid ID { get; set; }
	[MaxLength(20, ErrorMessage = "Username cannot exceed 20 characters")]
	public string Nom { get; set; } = string.Empty;
	[Required]
	[EmailAddress]
	public string? Email { get; set; }
	public enum Privilege { Administrateur, Utilisateur }
	[EnumDataType(typeof(Privilege))]
	[Required]
	public Privilege Role { get; set; }
	[Required]
	[Category("Security")]
	public string? Pass { get; set; }
	public bool CheckHashPassword(string password)
	{
		return BCrypt.Net.BCrypt.Verify(password, Pass);
	}
}